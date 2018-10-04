# Example how to use the AadApiGatekeeper in a Kubernetes Pod or in ServiceFabric Mesh

## Prerequisites

* **Azure CLI** - To login to Azure Container Registry and push our images [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
* **Azure container registry** - Create a container registry in your Azure subscription. For example, use the [Azure portal](container-registry-get-started-portal.md) or the [Azure CLI](container-registry-get-started-azure-cli.md).
* **Docker CLI** - To set up your local computer as a Docker host and access the Docker CLI commands, install [Docker](https://docs.docker.com/engine/installation/).
* **kubectl** command-line tool to deploy and manage applications on Kubernetes. [kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/)
* **az mesh** extension to deploy and manage applications on ServiceFabric Mesh. [mesh](https://docs.microsoft.com/en-us/azure/service-fabric-mesh/service-fabric-mesh-howto-setup-cli)

## Containerize the AadApiGatekeeper and the myapi sample application

* Clone the repository (`git clone https://github.com/AndreasM009/AadApiGatekeeper.git`) locally and navigate to the folder `src\examples\MyApi`.
* Create the container and tag it with *myapi* using the docker cli:

    ```docker
    docker build -t myapi .
    ```

* Navigate to `src\AadApiGatekeeper` and create the container with the tag *gatekeeper*:

    ```docker
    docker build -t gatekeeper .
    ```

* Ensure the images has been built:

    ```docker
    docker images
    ```

## Push the images to your registry

* Log in to your Azure Container Registry

    ```docker
    az login
    az acr login --name <your-registry>
    ```
    > **Note:** If you have multiple Azure subscriptions, you may have to set your desired subscription using
    > `az account set --subscription <yoursubscription>`

* Create an alias of the images

    ```docker
    docker tag gatekeeper <your-registry>.azurecr.io/gatekeeper
    docker tag myapi <your-registry>.azurecr.io/myapi
    ```
* Push the images

    ```docker
    docker push <your-registry>.azurecr.io/gatekeeper
    docker push <your-registry>.azurecr.io/myapi
    ```

## Create and configure a kubernetes cluster

* Create the cluster using the Azure CLI

    ```docker
    az group create -n <ressource-group-name> -l <location>
    az aks create --name <cluster-name> --resource-group <ressource-group-name> --node-count 3 --generate-ssh-keys --kubernetes-version 1.10.6
    ```
* Export kubectrl credentials

    ```docker
    az aks get-credentials --resource-group=<ressource-group-name> --name=<cluster-name>
    ```
    > **Note:** If you don't have kubectl installed yet, you can install it now using: `az aks install-cli`

* Configure RBAC by creating ClusterRoleBinding:

    ```docker
    kubectl create clusterrolebinding kubernetes-dashboard --clusterrole=cluster-admin --serviceaccount=kube-system:kubernetes-dashboard
    ```
* Ensure the cluster is running

    ```docker
    kubectl proxy
    ```
    Navigate to and check the [dashboard](http://localhost:8001/api/v1/namespaces/kube-system/services/kubernetes-dashboard/proxy/#!/pod?namespace=default).

## Deploy the containers to Kubernetes

The sample already contains a deployment template located at `src\examples\MyApi\k8s\deployment.yaml`. All you have to do is to replace the environment variables and secrets. 
> **Note:** You have to specify the secrets in *Base64*. You can use the following PowerShell script for the encoding:

```PowerShell
[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("admin"))
```

To access the images in your Azure Container Registry, create a secret and reference it in the yaml file:

```docker
kubectl create secret docker-registry <your-registry> --docker-server <your-registry>.azurecr.io --docker-username <docker-user> --docker-password <docker-password> --docker-email example@example.com
```

Finally, perform the deployment using

```docker
kubectl apply -f src\examples\MyApi\k8s\deployment.yaml
```

## Deploy the container to ServiceFabric Mesh

The sample already contains a deplyoment template located at: `src\examples\MyApi\servicefabricmesh\deployment.json`.
All you have to do is to replace the environment variables.
To create an Azure Active Directory application there is a utility Powershell script located at `src\examples\MyApi\utils\New-MyApiAadApplication.ps1`. You can use this script to create the application in your Azure Active Directory tenant. The script returns the needed ClientId, ClientSecret and TenantId which must be replaced in the deployment file. 

``` Powershell
./New-MyApiAadApplication.ps1
```

Replace the environment variables and create a new resource group.

``` Azure CLI
az group create --name "<your rg name>" --location westeurope
```

After the resource group is created you can deploy the ServiceFabrci Mesh Application

```
az mesh deployment create --template-file <full path to src\examples\MyApi\servicefabricmesh\deployment.json>
```

## Test the application

### For a Kubernetes deployment do the following:

After the deployment is done, get the service to get the external load balancer ip.

```
kubectl get service myapisvc
```

Copy the external ip address, open your browser and navigate to:

```
http://<external ip>/login
```

### For a ServiceFabric Mesh deployment do the following

After the deployment is done get the public Ip of the Mesh Application.

```
az mesh network list
```
Copy the ip address and modify the ReplyUrls of your Azure Active Directory application.
You can do this either manually or by using the utility script located at `src\examples\MyApi\utils\Set-MyApiAadApplicationReplyUrl`.

```
.\Set-MyApiAadApplicationReplyUrl -ApplicationId <the app id> -IpAddress <the copied ip address>
```
Oopen your browser and navigate to:

```
http://<ip address>/login
```

Login to your Azure Active Directory. After login your browser is redirected to the Swagger UI.
Here you can test the API of MyAPI. Executing the method /api/Echo/headers shows you all http headers that are forwarded to your API. You can see that the authorization header contains the bearer token that can be used by your API to call the AadApiGateKeeper API. 
The method /api/Echo/claims takes the bearer token to call the AadApiGateKeeper API to get user's claims. The AadApiGateKeeper is created as SideCar in your pod, therefore you can do a simple http call to localhost:<AadApiGatekeeperPort>/me . Take a look at the [EchoController](src/examples/MyApi/Controllers/EchoController.cs) to see all the details.

``` C#
var token = new StringValues();
// get bearer token of current request
if (!Request.Headers.TryGetValue("Authorization", out token))
    return null;

// the token
var bearerToken = token.First().Replace("Bearer ", "");

// create a http GET request and set authorization header
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
var proxyPort = Environment.GetEnvironmentVariable("Api__ProxyPort");
var result = await httpClient.GetStringAsync($"http://localhost:{proxyPort}/me");

return JsonConvert.DeserializeObject<Dictionary<string, string>>(result);
```