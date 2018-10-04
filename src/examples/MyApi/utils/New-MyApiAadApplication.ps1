# This script creates an Azure Active Directory Application for the sample Application MyApi.
# The script returns the ClientId and ClientSecret of the application.
# Please save these values to configure your Kubernetes or ServiceFabric Mesh deployment.
Function GetAuthToken
{
    param(
        [Parameter(Mandatory=$true)]
        $TenantId)

    Import-Module Azure
    $clientId = "1950a258-227b-4e31-a9cf-717495945fc2" # Set well known client ID for AzurePowershell Application 
    $redirectUri = "urn:ietf:wg:oauth:2.0:oob"
    $resourceAppIdURI = "https://graph.windows.net"
    $authority = "https://login.microsoftonline.com/$TenantId"
    $authContext = New-Object "Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext" -ArgumentList $authority
    $authResult = $authContext.AcquireToken($resourceAppIdURI, $clientId, $redirectUri, [Microsoft.IdentityModel.Clients.ActiveDirectory.PromptBehavior]::Auto)
    return $authResult
}

$tenant = $(Get-AzureRmContext).Tenant.Id

#Register the WebApplication in AAD
$password = [System.Guid]::NewGuid().ToString()
$application = New-AzureRmADApplication `
    -DisplayName "MyApi" `
    -IdentifierUris "http://MyApi" `
    -AvailableToOtherTenants $false

# Create a ServicePrincipal for the application
New-AzureRmADServicePrincipal -ApplicationId $application.ApplicationId

# Create a ClientSecret
New-AzureRmADAppCredential `
    -ApplicationId $application.ApplicationId `
    -Password (ConvertTo-SecureString -String $password -AsPlainText -Force) `
    -StartDate $([System.DateTime]::Now) `
    -EndDate $([System.DateTime]::Now.AddYears(2))


# Allow SignIn Users
$applicationRequiredResourceAccess = @{requiredResourceAccess = @(
    @{
        resourceAppId = "00000002-0000-0000-c000-000000000000"
        resourceAccess = @(
        @{
            id = "311a71cc-e848-46a1-bdf8-97ff7156d8e6"
            type= "Scope"
        })
    })
}

$tenantId = $tenant
$objectId = $application.ObjectId
$url = "https://graph.windows.net/$tenantId/applications/$($objectId)?api-version=1.6"
$token = GetAuthToken -TenantId $tenantId

$headers = @{
'Content-Type' = 'application/json' 
'Authorization' = $token.CreateAuthorizationHeader()
}

$json = $applicationRequiredResourceAccess | ConvertTo-Json -Depth 4 -Compress

Invoke-RestMethod -Uri $url -Method Patch -Headers $headers -Body $json -ContentType "application/json"

return @{
    ApplicationId = $application.ApplicationId
    ClientSecret = $password    
    TenantId = $tenantId
}