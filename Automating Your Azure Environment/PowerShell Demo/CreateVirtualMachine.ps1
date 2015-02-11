cls

$DebugPreference = "SilentlyContinue"
$VerbosePreference = "Continue"


$storageAccountName = "codemash2015"
$serviceName = "codemash-ps-vm-demo"
$vmName = "codemash-vm-1"

Set-AzureSubscription -SubscriptionName "Azure MVP MSDN Subscription" -CurrentStorageAccountName $storageAccountName
Select-AzureSubscription -SubscriptionName "Azure MVP MSDN Subscription"

$storageKey = Get-AzureStorageKey -StorageAccountName $storageAccountName
$storageContext = New-AzureStorageContext -StorageAccountName $storageAccountName -StorageAccountKey $storageKey.Primary


# Find the most recent Windows Server 2012 Datacenter image from the Azure image gallery
$image = Get-AzureVMImage `
    | where { ($_.PublisherName -ilike "Microsoft*" -and $_.ImageFamily -ilike "Windows Server 2012 R2 Datacenter" ) } `
    | sort -Unique -Descending -Property ImageFamily `
    | sort -Descending -Property PublishedDate `
    | select -First(1)


# Get the administrative credentials to use for the virtual machine
Write-Verbose "Prompt user for administrator credentials to use when provisioning the virtual machine(s)." 
$credential = Get-Credential -Message "Enter the username and password for the virtual machine administrator."
Write-Verbose "Administrator credentials captured.  Use these credentials to login to the virtual machine(s) when the script is complete."


# Create configuration details for the VM
$vm = New-AzureVMConfig -Name $vmName -InstanceSize Basic_A1 -ImageName $image.ImageName `
    | Add-AzureProvisioningConfig -Windows -AdminUsername $credential.UserName -Password $credential.GetNetworkCredential().Password `
    | Add-AzureDataDisk -CreateNew -DiskSizeInGB 10 -DiskLabel "disk 1" -LUN 0 `
    | Add-AzureDataDisk -CreateNew -DiskSizeInGB 10 -DiskLabel "disk 2" -LUN 1 `
    | Add-AzureEndpoint -Name "HTTP" -LocalPort 80 -PublicPort 80 -Protocol tcp `
    | Add-AzureEndpoint -Name "HTTPS" -LocalPort 443 -PublicPort 443 -Protocol tcp


# Custom script extension
$vm | Set-AzureVMCustomScriptExtension -ContainerName "vmstartup" -FileName "formatdisk.ps1" -Run "formatdisk.ps1"


# Create the VM
New-AzureVM -VMs $vm -Location "West US" -ServiceName $serviceName -WaitForBoot
