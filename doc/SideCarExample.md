# Example how to use the AadApiGatekeeper in a Kubernetes Pod

## Prerequisites

* **Azure CLI** - To login to Azure Container Registry and push our images [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
* **Azure container registry** - Create a container registry in your Azure subscription. For example, use the [Azure portal](container-registry-get-started-portal.md) or the [Azure CLI](container-registry-get-started-azure-cli.md).
* **Docker CLI** - To set up your local computer as a Docker host and access the Docker CLI commands, install [Docker](https://docs.docker.com/engine/installation/).
* **kubectl** command-line tool to deploy and manage applications on Kubernetes. [kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/)

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

## Deploy the containers

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