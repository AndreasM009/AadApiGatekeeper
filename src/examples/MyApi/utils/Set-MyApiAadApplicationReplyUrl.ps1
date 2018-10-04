param(
    [Parameter(Mandatory=$true)]
    [string] $ApplicationId,
    [Parameter(Mandatory=$true)]
    [string]$IpAddress
)

$url = "http://" + $IpAddress + "/signin-oidc"
Set-AzureRmADApplication -ApplicationId $ApplicationId -ReplyUrl @($url)