workflow Stop-DevelopmentMachines
{
    Param
    (
        [parameter(Mandatory=$true)]
        [String]
        $ServiceName    
    )
     # Specify Azure Subscription Name
   # $subName = 'Azure MVP MSDN Connection'
    $subName = 'Azure MVP MSDN Subscription'
    
    
    # Connect to Azure Subscription using a mgmt cert
    #Connect-Azure -AzureConnectionName $subName
    
    $cred = Get-AutomationPSCredential -Name "maml@collierdemo.onmicrosoft.com"
    
    Add-AzureAccount -Credential $cred
        
    Select-AzureSubscription -SubscriptionName $subName 
    
    Get-AzureVM | Select InstanceName
    
    Get-AzureService -ServiceName $ServiceName `
	   | Foreach-Object { Stop-AzureVM -ServiceName $_.ServiceName -Name "*" –Force }
}