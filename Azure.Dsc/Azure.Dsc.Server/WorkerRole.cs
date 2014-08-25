using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Azure.Dsc.Common.DesiredStateConfiguration;
using Azure.Dsc.Common.Security;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Sogeti.IaC.Common.Security;
using Sogeti.IaC.Model.DesiredStateConfiguration;

namespace Azure.Dsc.Server
{
    public class WorkerRole : RoleEntryPoint
    {
        public SubscriptionClient Client { get; private set; }
        public CloudBlobContainer Container { get; private set; }

        public override void Run()
        {
            // This is a sample worker implementation. Replace with your logic.
            Trace.TraceInformation("Sogeti.IaC.DscPullServer entry point called");

            while (true)
            {
                var message = Client.Receive(TimeSpan.FromMinutes(5));

                if (message == null)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                try
                {
                    var machineId = message.GetBody<string>();
                    var mof = string.Format("{0}.mof", machineId);
                    var checksum = string.Format("{0}.mof.checksum", machineId);

                    var containerUri = Container.Uri.AbsoluteUri;

                    var mofRef = Container.GetBlockBlobReference(string.Format("{0}/{1}", containerUri, mof));
                    var checksumRef = Container.GetBlockBlobReference(string.Format("{0}/{1}", containerUri, checksum));

                    DownloadBlob(mofRef);
                    DownloadBlob(checksumRef);

                    message.Complete();
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.Message);
                    message.Abandon();
                }


                Trace.TraceInformation("Working");
            }
        }

        public override bool OnStart()
        {
            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            InitializeServiceBus();

            InitializeStorage();

            return base.OnStart();
        }

        private void InitializeStorage()
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("ConfigurationStorageConnectionString"));
            CloudBlobClient client = account.CreateCloudBlobClient();
            CloudBlobContainer dsc = client.GetContainerReference("dsc");
            dsc.CreateIfNotExists();

            CloudBlockBlob resources = dsc.GetBlockBlobReference("resources.zip");
            using (var stream = new MemoryStream())
            {
                resources.DownloadToStream(stream);

                var tempModulePath = RoleEnvironment.GetLocalResource("Temp").RootPath;

                var path = DscHelper.PrepareDscResourcePackages(tempModulePath, stream);

                DscHelper.MoveDscModules(path);
            }

            Container = client.GetContainerReference("mof");
            Container.CreateIfNotExists();

            var blobs = Container.ListBlobs();

            try
            {
                using (Impersonator.Impersonate())
                {
                    foreach (var blob in blobs)
                    {
                        var blobRef = Container.GetBlockBlobReference(blob.Uri.AbsoluteUri);
                        DownloadBlob(blobRef);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
        }

        private void DownloadBlob(CloudBlockBlob blob)
        {

            var targetFile = string.Format(@"{0}\WindowsPowerShell\DscService\Configuration\{1}",
                Environment.GetEnvironmentVariable("ProgramFiles"), blob.Uri.Segments[2]);

            blob.DownloadToFile(targetFile, FileMode.Create);
        }

        private void InitializeServiceBus()
        {
            string connectionString = CloudConfigurationManager.GetSetting("ConfigurationServiceBusConnectionString");
            string topicName = CloudConfigurationManager.GetSetting("ConfigurationTopic");
            var instanceNameParts = RoleEnvironment.CurrentRoleInstance.Id.Split('_');
            var subscriptionName =
                RoleEnvironment.CurrentRoleInstance.Role.Name.ToUpper() +
                "_" +
                instanceNameParts[instanceNameParts.Length - 2] +
                "_" +
                instanceNameParts[instanceNameParts.Length - 1];

            try
            {
                var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

                if (!namespaceManager.TopicExists(topicName.ToLower()))
                {
                    namespaceManager.CreateTopic(topicName.ToLower());

                    Trace.TraceInformation("Successfully created topic '{0}'.", topicName);
                }

                if (!namespaceManager.SubscriptionExists(topicName, subscriptionName.ToLower()))
                {
                    namespaceManager.CreateSubscription(topicName, subscriptionName);

                    Trace.TraceInformation("Successfully created subscription '{0}' on topic '{1}'.", subscriptionName, topicName);
                }

                Client = SubscriptionClient.CreateFromConnectionString(connectionString,
                        topicName, subscriptionName, ReceiveMode.PeekLock);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Failed to create subscription '{0}' on topic '{1}' due to the following error: {2}", subscriptionName, topicName, ex.Message);
            }
        }
    }
}
