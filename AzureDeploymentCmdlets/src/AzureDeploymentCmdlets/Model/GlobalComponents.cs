﻿using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using AzureDeploymentCmdlets.Properties;
using AzureDeploymentCmdlets.PublishSettingsSchema;
using AzureDeploymentCmdlets.Utilities;

namespace AzureDeploymentCmdlets.Model
{
    class GlobalComponents
    {
        public GlobalPathInfo GlobalPaths { get; private set; }
        public PublishData PublishSettings { get; private set; }
        public X509Certificate2 Certificate { get; private set; }
        public ServiceSettings GlobalSettings { get; private set; }

        public GlobalComponents(string azureSdkPath)
        {
            GlobalPaths = new GlobalPathInfo(azureSdkPath);
            Load(GlobalPaths);
        }

        public GlobalComponents(string publishSettingsPath, string azureSdkPath)
        {
            GlobalPaths = new GlobalPathInfo(azureSdkPath);
            New(publishSettingsPath, GlobalPaths);
            Save(GlobalPaths, azureSdkPath);
        }

        private void New(string publishSettingsPath, GlobalPathInfo paths)
        {
            Validate.ValidateNullArgument(paths, string.Empty);
            Validate.ValidateStringIsNullOrEmpty(paths.AzureSdkDirectory, Resources.AzureSdkDirectoryName);
            Validate.ValidateFileFull(publishSettingsPath, Resources.PublishSettings);
            Validate.ValidateFileExtention(publishSettingsPath, Resources.PublishSettingsFileExtention);

            PublishSettings = General.DeserializeXmlFile<PublishData>(publishSettingsPath, string.Format(Resources.InvalidPublishSettingsSchema, publishSettingsPath));
            Certificate = new X509Certificate2(Convert.FromBase64String(PublishSettings.Items[0].ManagementCertificate), string.Empty);
            PublishSettings.Items[0].ManagementCertificate = Certificate.Thumbprint;
            GlobalSettings = new ServiceSettings();

            General.AddCertificateToStore(Certificate);
        }

        internal void Load(GlobalPathInfo paths)
        {
            Validate.ValidateNullArgument(paths, string.Empty);
            Validate.ValidateDirectoryExists(paths.AzureSdkDirectory, Resources.GlobalComponents_Load_PublishSettingsNotFound);
            Validate.ValidateFileExists(paths.GlobalSettings, string.Format(Resources.PathDoesNotExistForElement, Resources.SettingsFileName, paths.GlobalSettings));
            Validate.ValidateFileExists(paths.PublishSettings, string.Format(Resources.PathDoesNotExistForElement, Resources.PublishSettingsFileName, paths.PublishSettings));

            PublishSettings = General.DeserializeXmlFile<PublishData>(paths.PublishSettings);
            Certificate = General.GetCertificateFromStore(PublishSettings.Items[0].ManagementCertificate);
            GlobalSettings = ServiceSettings.Load(paths.GlobalSettings);
        }

        internal void Save(GlobalPathInfo paths, string azureSdkPath)
        {
            // Create new AzureSDK directory if doesn't exist
            //
            Directory.CreateDirectory(azureSdkPath);

            // Save *.publishsettings
            //
            General.SerializeXmlFile<PublishData>(PublishSettings, paths.PublishSettings);
            
            // Save global settings
            //
            GlobalSettings.Save(paths.GlobalSettings);
        }

        internal string GetSubscriptionId(string subscriptionName)
        {
            foreach (PublishDataPublishProfileSubscription subscription in PublishSettings.Items[0].Subscription)
            {
                if (subscription.Name.Equals(subscriptionName))
                {
                    Validate.IsGuid(subscription.Id);
                    return subscription.Id;
                }
            }

            throw new ArgumentException(string.Format(Resources.SubscriptionIdNotFoundMessage, subscriptionName, GlobalPaths.PublishSettings));
        }

        internal void DeleteGlobalComponents()
        {
            General.RemoveCertificateFromStore(Certificate);
            File.Delete(GlobalPaths.PublishSettings);
            File.Delete(GlobalPaths.GlobalSettings);
            Directory.Delete(GlobalPaths.AzureSdkDirectory);
        }
    }
}