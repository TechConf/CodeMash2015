#Requires -Version 3.0

<#
What else would you like this script to do for you?  Send us feedback here: http://go.microsoft.com/fwlink/?LinkID=517524 
#>

Param(
  [string][Parameter(Mandatory=$true)] $StorageAccountName,
  [string][Parameter(Mandatory=$true)] $ResourceGroupLocation,
  [string] $ResourceGroupName = 'AutomationDemo',
  [string] $StorageContainerName = $ResourceGroupName.ToLowerInvariant(),
  [string] $TemplateFile = '..\Templates\WebSiteDeploySQLRedis.json',
  [string] $TemplateParametersFile = '..\Templates\WebSiteDeploySQLRedis.param.dev.json',
  [string] $LocalStorageDropPath = '..\bin\Debug\StorageDrop',
  [string] $AzCopyPath = '..\Tools\AzCopy.exe'
)

Set-StrictMode -Version 3

# Convert relative paths to absolute paths if needed
$AzCopyPath = [System.IO.Path]::Combine($PSScriptRoot, $AzCopyPath)
$TemplateFile = [System.IO.Path]::Combine($PSScriptRoot, $TemplateFile)
$TemplateParametersFile = [System.IO.Path]::Combine($PSScriptRoot, $TemplateParametersFile)
$LocalStorageDropPath = [System.IO.Path]::Combine($PSScriptRoot, $LocalStorageDropPath)

# Use AzCopy to copy files from the local storage drop path to the storage account container
Switch-AzureMode AzureServiceManagement
$storageAccountKey = (Get-AzureStorageKey -StorageAccountName $StorageAccountName).Primary
$storageAccountContext = New-AzureStorageContext $StorageAccountName (Get-AzureStorageKey $StorageAccountName).Primary
$dropLocation = $storageAccountContext.BlobEndPoint + $StorageContainerName
& "$AzCopyPath" """$LocalStorageDropPath"" $dropLocation /DestKey:$storageAccountKey /S /Y"

# Create a SAS token for the storage container - this gives temporary read-only access to the container (defaults to 1 hour).
$dropLocationSasToken = New-AzureStorageContainerSASToken -Container $StorageContainerName -Context $storageAccountContext -Permission r 
$dropLocationSasToken = ConvertTo-SecureString $dropLocationSasToken -AsPlainText -Force

# Create or update the resource group using the specified template file and template parameters file
Switch-AzureMode AzureResourceManager
New-AzureResourceGroup -Name $ResourceGroupName `
                       -Location $ResourceGroupLocation `
                       -TemplateFile $TemplateFile `
                       -TemplateParameterFile $TemplateParametersFile `
                       -dropLocation $dropLocation `
                       -dropLocationSasToken $dropLocationSasToken `
                       -Force -Verbose
