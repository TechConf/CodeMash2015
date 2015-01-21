using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Web.Deployment;

namespace AzureMgmtLibraryDemo
{
    // Source: https://github.com/bradygaster/build2014-MAML-IntegrationTesting/blob/master/WebSiteIntegrationTests/WebDeployPublishingHelper.cs

    internal class WebDeployPublishingHelper
    {
        private string _serviceUrl;
        private string _remoteSite;
        private string _localSourceFolder;
        private string _user;
        private string _password;

        public WebDeployPublishingHelper(
            string serviceUrl,
            string remoteSite,
            string userName,
            string passWord,
            string localSourceFolder)
        {
            _serviceUrl = serviceUrl;
            _remoteSite = remoteSite;
            _user = userName;
            _password = passWord;
            _localSourceFolder = localSourceFolder;
        }

        public void PublishFolder()
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;

            // Create temp files for the manifests
            System.Net.ServicePointManager.ServerCertificateValidationCallback = AllowCertificateCallback;
            DeploymentBaseOptions RemoteBaseOption = new DeploymentBaseOptions();
            DeploymentSyncOptions SyncOption = new DeploymentSyncOptions();

            UpdateBaseOptions(RemoteBaseOption, _serviceUrl, _remoteSite, _user, _password);

            SyncOption.DoNotDelete = true;
            SyncOption.WhatIf = false;


            DeploymentBaseOptions localBaseOptions = new DeploymentBaseOptions();
            using (DeploymentObject deploymentObject = DeploymentManager.CreateObject(DeploymentWellKnownProvider.ContentPath, _localSourceFolder, localBaseOptions))
            {
                deploymentObject.SyncTo(DeploymentWellKnownProvider.ContentPath, _remoteSite, RemoteBaseOption, SyncOption);
            }

            Console.ForegroundColor = oldColor;
        }

        private void UpdateBaseOptions(DeploymentBaseOptions baseOption, string serviceUrl, string remoteSite, string user, string password)
        {
            baseOption.ComputerName = ConstructServiceUrlForDeployThruWMSVC(serviceUrl, remoteSite);
            baseOption.TempAgent = false;
            baseOption.AuthenticationType = "Basic";
            baseOption.Trace += new EventHandler<DeploymentTraceEventArgs>(LogTrace);
            baseOption.UserName = user;
            baseOption.Password = password;
        }

        private void LogTrace(object sender, DeploymentTraceEventArgs args)
        {
            Console.WriteLine(args.Message);
        }

        //ported from IISOOB\projects\ui\wm\Deployment\Data\Profiles\PublishProfile.cs
        private string ConstructServiceUrlForDeployThruWMSVC(string serviceUrl, string siteName)
        {
            const string https = "https://";
            const string http = "http://";
            const string msddepaxd = "msdeploy.axd";

            System.UriBuilder serviceUriBuilder = null;

            // We want to try adding https:// if there is no schema. However abc:123 is parsed as a schema=abc and path=123
            // so the goal is to isolate this case and add the https:// but allow for http if the user chooses to
            // since we do not allow for any schema other than http or https, it's safe to assume we can add it if none exist
            if (!(serviceUrl.StartsWith(http, StringComparison.OrdinalIgnoreCase) || serviceUrl.StartsWith(https, StringComparison.OrdinalIgnoreCase)))
            {
                serviceUrl = string.Concat(https, serviceUrl.TrimStart());
            }

            serviceUriBuilder = new UriBuilder(serviceUrl);

            // if the user did not explicitly defined a port
            if (serviceUrl.IndexOf(":" + serviceUriBuilder.Port.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase) == -1)
            {
                serviceUriBuilder.Port = 8172;
            }

            // user did not explicitly set a path
            if (string.IsNullOrEmpty(serviceUriBuilder.Path) || serviceUriBuilder.Path.Equals("/", StringComparison.OrdinalIgnoreCase))
            {
                serviceUriBuilder.Path = msddepaxd;
            }

            // user did not explicityly set the scheme
            if (serviceUrl.IndexOf(serviceUriBuilder.Scheme, StringComparison.OrdinalIgnoreCase) == -1)
            {
                serviceUriBuilder.Scheme = https;
            }
            if (string.IsNullOrEmpty(serviceUriBuilder.Query))
            {
                string[] fragments = siteName.Trim().Split(new char[] { '/', '\\' });
                serviceUriBuilder.Query = "site=" + fragments[0];
            }

            return serviceUriBuilder.Uri.AbsoluteUri;
        }

        private bool AllowCertificateCallback(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }
    }
}
