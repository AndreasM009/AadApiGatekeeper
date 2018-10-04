Param(
[Parameter(Mandatory=$true)]
[string]$ApplicationId)

$app = Get-AzureRmADApplication -ApplicationId $ApplicationId
Remove-AzureRmADApplication -ObjectId $app.ObjectId -Force