using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using Microsoft.WindowsAzure.Management.Models;
using Microsoft.WindowsAzure.Management.Storage;
using Microsoft.WindowsAzure.Management.Storage.Models;
using Microsoft.WindowsAzure.Management.WebSites;
using Microsoft.WindowsAzure.Management.WebSites.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Subscriptions;
using Microsoft.WindowsAzure.Subscriptions.Models;

namespace AzureMgmtLibraryDemo
{
    class Program
    {
        private const string ResourceName = "CodeMash2015";
        private const string TenantId = "[your Azure AD tenant id]";
        private const string ClientId = "[your Azure AD client id]";
        private const string WebSitePath = @"C:\Projects\Demos\AzureAutomationDemos\AzureAutomation\MAML\SampleSite";


        static void Main(string[] args)
        {
            DoWork().Wait();

            Console.ReadLine();
        }

        private static async Task DoWork()
        {
            //var logAction = new Action<string>(t => File.AppendAllText(@"c:\temp\maml.log", t));
            //CloudContext.Configuration.Tracing.AddTracingInterceptor(new MyTraceInterceptor(logAction));


            // Step 1 - Authenticate
            string token = await GetAuthorizationHeader();
            TokenCloudCredentials tokenCloudCredentials = new TokenCloudCredentials(token);

            // Step 2 - Get a specific Azure subscription
            var subscription = await GetSubscription(tokenCloudCredentials, "MVP");
            tokenCloudCredentials = new TokenCloudCredentials(subscription.SubscriptionId, tokenCloudCredentials.Token);

            // Step 3 - Create a storage account
            var storageAccountName = await CreateStorageAccount(tokenCloudCredentials);

            // Step 4 - Create a web site
            var webSiteResponse = await CreateWebSite(tokenCloudCredentials);

            // Step 5 - Clean up
            Console.WriteLine("Time to clean up! Press any key to delete all previously created resources.");
            Console.ReadLine();

            var response = await RemoveStorageAccount(tokenCloudCredentials, storageAccountName);
            var deleteSiteResponse = await RemoveWebSite(tokenCloudCredentials, webSiteResponse.WebSite);

            Console.WriteLine("All done!");
            Console.ReadLine();
        }

        private static async Task<string> GetAuthorizationHeader()
        {
            string username = "";
            string password = "";

            var context = new AuthenticationContext(string.Format("https://login.windows.net/{0}", TenantId));

            var userCred = new UserCredential(username, password);

            AuthenticationResult result =
                await context.AcquireTokenAsync("https://management.core.windows.net/", ClientId, userCred);

            return result.CreateAuthorizationHeader().Substring("Bearer ".Length);
        }

        private static async Task<SubscriptionListOperationResponse.Subscription> GetSubscription(
            CloudCredentials credentials, string filter)
        {
            IEnumerable<SubscriptionListOperationResponse.Subscription> subscriptionList = null;

            using (var client = new SubscriptionClient(credentials))
            {
                var results = await client.Subscriptions.ListAsync();
                subscriptionList = results.Subscriptions;
            }

            var selectedSubscription = subscriptionList.First(s => s.SubscriptionName.Contains(filter));

            return selectedSubscription;
        }

        #region Storage Account

        private async static Task<OperationResponse> RemoveStorageAccount(SubscriptionCloudCredentials credentials, string storageAccountName)
        {
            OperationResponse response = null;

            using (var client = new StorageManagementClient(credentials))
            {
                response = await client.StorageAccounts.DeleteAsync(storageAccountName);
            }

            Console.WriteLine("Deleted storage account {0}", storageAccountName);


            return response;
        }

        private async static Task<string> CreateStorageAccount(SubscriptionCloudCredentials credentials)
        {
            var storageAccountName = string.Format("{0}{1}", ResourceName.ToLower(), new Random().Next(1, 200));

            Console.WriteLine("Creating new storage account '{0}' . . .", storageAccountName);

            using (var client = new StorageManagementClient(credentials))
            {
                var result = await client.StorageAccounts.CreateAsync(new StorageAccountCreateParameters
                {
                    Location = LocationNames.EastUS,
                    Label = string.Format("{0} Demo Account", ResourceName),
                    Name = storageAccountName,
                    AccountType = StorageAccountTypes.StandardGRS
                });
            }

            Console.WriteLine("Storage account '{0}' created.", storageAccountName);


            return storageAccountName;
        }

        #endregion

        #region Websites

        private async static Task<OperationResponse> RemoveWebSite(SubscriptionCloudCredentials credentials, WebSite webSite)
        {
            OperationResponse response = null;

            using (var client = new WebSiteManagementClient(credentials))
            {
                response = await client.WebSites.DeleteAsync(webSite.WebSpace, webSite.Name,
                            new WebSiteDeleteParameters
                            {
                                DeleteAllSlots = true,
                                DeleteEmptyServerFarm = true,
                                DeleteMetrics = true
                            });
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Failed to delete website.");
            }
            else
            {
                Console.WriteLine("Deleted web site '{0}'.", webSite.Name);
            }

            return response;
        }

        private async static Task<WebSiteCreateResponse> CreateWebSite(SubscriptionCloudCredentials credentials)
        {
            Console.WriteLine("Creating new Azure Web Site . . .");

            WebSiteCreateResponse response = null;

            using (var client = new WebSiteManagementClient(credentials))
            {
                var webspaces = await client.WebSpaces.ListAsync();

                var myWebSpace = webspaces.First(x => x.GeoRegion == LocationNames.EastUS);

                var whp = new WebHostingPlanCreateParameters
                {
                    Name = string.Format("{0}_whp", ResourceName.ToLower()),
                    NumberOfWorkers = 1,
                    SKU = SkuOptions.Free,
                    WorkerSize = WorkerSizeOptions.Small
                };

                var whpCreateResponse = await client.WebHostingPlans.CreateAsync(myWebSpace.Name,whp);

                WebSiteCreateParameters siteCreateParameters = new WebSiteCreateParameters
                {
                    Name = string.Format("{0}{1}", ResourceName, new Random().Next(1, 200)),
                    ServerFarm = whp.Name,
                    WebSpace = new WebSiteCreateParameters.WebSpaceDetails
                    {
                        GeoRegion = LocationNames.EastUS,
                        Name = myWebSpace.Name,
                        Plan = "VirtualDedicatedPlan"
                    }
                };

                response = await client.WebSites.CreateAsync(myWebSpace.Name, siteCreateParameters);

                WebSiteGetPublishProfileResponse publishProfileResult = 
                    await client.WebSites.GetPublishProfileAsync(myWebSpace.Name, siteCreateParameters.Name);

                WebSiteGetPublishProfileResponse.PublishProfile profile = 
                    publishProfileResult.PublishProfiles.First(x => x.MSDeploySite != null);

                new WebDeployPublishingHelper(
                    profile.PublishUrl,
                    profile.MSDeploySite,
                    profile.UserName,
                    profile.UserPassword,
                    WebSitePath).PublishFolder();

            }

            return response;
        }

        #endregion

        #region Cloud Services

        private async static Task<OperationResponse> DeployCloudService(SubscriptionCloudCredentials credentials, string storageAccountName,
            string serviceName)
        {
            Console.WriteLine("Deploying to Cloud Service {0}", serviceName);

            OperationResponse response = null;
            string storageAccountKey = null;

            using (var client = new StorageManagementClient(credentials))
            {
                var keys = await client.StorageAccounts.GetKeysAsync(storageAccountName);
                storageAccountKey = keys.PrimaryKey;
            }

            string storageConnectionString =
                string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}", storageAccountName,
                    storageAccountKey);

            CloudStorageAccount account = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = account.CreateCloudBlobClient();
            CloudBlobContainer deploymentContainer = blobClient.GetContainerReference("maml-deployment");
            await deploymentContainer.CreateIfNotExistsAsync();


            CloudBlockBlob deploymentBlob = deploymentContainer.GetBlockBlobReference("AzureCloudService1.cspkg");
            await deploymentBlob.UploadFromFileAsync(@"C:\Projects\Demos\AzureAutomationDemos\AzureAutomation\AzureCloudService1\bin\Release\app.publish\AzureCloudService1.cspkg", FileMode.Open);

            using (var client = new ComputeManagementClient(credentials))
            {
                response = await client.Deployments.CreateAsync(serviceName,
                    DeploymentSlot.Production,
                    new DeploymentCreateParameters
                    {
                        Label = serviceName,
                        Name = serviceName + "Prod",
                        PackageUri = deploymentBlob.Uri,
                        Configuration = File.ReadAllText(@"C:\Projects\Demos\AzureAutomationDemos\AzureAutomation\AzureCloudService1\bin\Release\app.publish\ServiceConfiguration.Cloud.cscfg"),
                        StartDeployment = true
                    });
            }

            return response;
        }

        private async static Task<string> CreateCloudService(SubscriptionCloudCredentials credentials)
        {
            Console.WriteLine("Creating new Cloud Service . . .");

            OperationResponse response;

            string name = string.Format("{0}{1}", ResourceName, new Random().Next(1, 200));

            using (var client = new ComputeManagementClient(credentials))
            {
                response = await client.HostedServices.CreateAsync(new HostedServiceCreateParameters
                {
                    ServiceName = name,
                    Location = LocationNames.EastUS,
                    Label = string.Format("{0} Demo Service", ResourceName)
                });
            }

            return name;
        }

        private async static Task<OperationResponse> RemoveCloudService(SubscriptionCloudCredentials credentials,
            string serviceName)
        {
            OperationResponse response;

            Console.WriteLine("Removing Cloud Service '{0}'.", serviceName);

            using (var client = new ComputeManagementClient(credentials))
            {
                response = await client.HostedServices.DeleteAllAsync(serviceName);
            }

            return response;
        }

        #endregion
    }

    public class MyTraceInterceptor : ICloudTracingInterceptor
    {
        private readonly Action<string> _logAction;

        public MyTraceInterceptor(Action<string> logAction)
        {
            _logAction = logAction;
        }

        private void Write(string message, params object[] arguments)
        {
            _logAction(string.Format(message, arguments));
        }

        public void Information(string message)
        {
            Write(message);
        }

        public void Configuration(string source, string name, string value)
        {
            Write("Configuration(" + source + "): " + name + " = " + value);
        }

        public void Enter(string invocationId, object instance, string method, IDictionary<string, object> parameters)
        {
            Write("{0}: Enter {1}({4}) on 0x{3:X}:{2}",
                 invocationId,
                 method,
                 instance,
                 instance.GetHashCode(),
                 string.Join(
                     ", ",
                     parameters.Select(p => p.Key + "=" + p.Value.ToString())));
        }

        public void SendRequest(string invocationId, HttpRequestMessage request)
        {
            Write("{0}: SendRequst {1}", invocationId, request.ToString());
        }

        public void ReceiveResponse(string invocationId, HttpResponseMessage response)
        {
            Write("{0}: ReceiveResponse {1}", invocationId, response.ToString());
        }

        public void Error(string invocationId, Exception ex)
        {
            Write("{0}: Error {1}", invocationId, ex.ToString());
        }

        public void Exit(string invocationId, object result)
        {
            Write("{0}: Exit {1}", invocationId, result);
        }
    }
}
