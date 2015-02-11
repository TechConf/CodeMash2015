<#
.SYNOPSIS 
    Sets up the connection to an Azure subscription

.DESCRIPTION
    This runbook sets up a connection to an Azure subscription by placing the Azure
    management certificate into the local machine store and setting the connection to the subscription.

.PARAMETER AzureConnection
    Name of the Azure connection asset that was created in the Automation service.
    The name of the connection should match the Azure subscription name.
    This connection asset contains the subscription id and the name of the certificate asset that 
    holds the management certificate for this subscription.

.EXAMPLE
    Connect-Azure -AzureConnection "Visual Studio Ultimate with MSDN"

.NOTES
    AUTHOR: System Center Automation Team
    LASTEDIT: Jan 31, 2014 
#>

workflow Connect-Azure
{
    Param
    (   
        [Parameter(Mandatory=$true)]
        [String]
        $AzureConnectionName       
    )
    
    # Get the Azure connection asset that is stored in the Auotmation service based on the name that was passed into the runbook 
    $AzureConn = Get-AutomationConnection -Name $AzureConnectionName
    if ($AzureConn -eq $null)
    {
        throw "Could not retrieve '$AzureConnectionName' connection asset. Check that you created this first in the Automation service."
    }

    # Get the Azure management certificate that is used to connect to this subscription
    $Certificate = Get-AutomationCertificate -Name $AzureConn.AutomationCertificateName
    if ($Certificate -eq $null)
    {
        throw "Could not retrieve '$AzureConn.AutomationCertificateName' certificate asset. Check that you created this first in the Automation service."
    }
    
    InlineScript {         
        # Import the Azure management certificate into the LocalMachine
        $MgmtCertThumbprint = ($Using:Certificate.Thumbprint).ToString()    
		         
        if ((Test-Path Cert:\LocalMachine\Root\$MgmtCertThumbprint) -eq $false)
        {
            Write-Progress "Management certificate is not in the local machine certificate store - adding it"     

            $store = new-object System.Security.Cryptography.X509Certificates.X509Store("Root", "LocalMachine") 
            $store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
            $store.Add($Using:Certificate) 
            $store.Close() 
        }

        # Set the Azure subscription configuration
        Set-AzureSubscription -SubscriptionName $Using:AzureConnectionName -SubscriptionId $Using:AzureConn.SubscriptionID -Certificate $using:Certificate
    }


}