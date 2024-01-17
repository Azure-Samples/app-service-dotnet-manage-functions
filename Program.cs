// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using Azure.ResourceManager.AppService.Models;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Samples.Common;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ManageFunctionAppBasic
{
    public class Program
    {
        /**
         * Azure App Service basic sample for managing function apps.
         *  - Create 3 function apps under the same new app service plan:
         *    - 1, 2 are in the same resource group, 3 in a different one
         *    - 1, 3 are under the same consumption plan, 2 under a basic app service plan
         *  - List function apps
         *  - Delete a function app
         */

        public static async Task RunSample(ArmClient client)
        {
            // New resources
            AzureLocation region = AzureLocation.EastUS;
            string websiteName = Utilities.CreateRandomName("website-");
            string website2Name = Utilities.CreateRandomName("website-");
            string planName = Utilities.CreateRandomName("appserviceplan-");
            string app1Name = Utilities.CreateRandomName("webapp1-");
            string app2Name = Utilities.CreateRandomName("webapp2-");
            string app3Name = Utilities.CreateRandomName("webapp3-");
            string rg1Name = Utilities.CreateRandomName("rg1NEMV_");
            string rg2Name = Utilities.CreateRandomName("rg2NEMV_");
            var lro1 = await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rg1Name, new ResourceGroupData(AzureLocation.EastUS));
            var resourceGroup1 = lro1.Value;
            var lro2 = await client.GetDefaultSubscription().GetResourceGroups().CreateOrUpdateAsync(Azure.WaitUntil.Completed, rg2Name, new ResourceGroupData(AzureLocation.EastUS));
            var resourceGroup2 = lro2.Value;

            try
            {


                //============================================================
                // Create a function app with a new app service plan

                Utilities.Log("Creating function app " + app1Name + " in resource group " + rg1Name + "...");

                var webSiteCollection = resourceGroup1.GetWebSites();
                var webSiteData = new WebSiteData(region)
                {
                    SiteConfig = new Azure.ResourceManager.AppService.Models.SiteConfigProperties()
                    {
                        WindowsFxVersion = "PricingTier.StandardS1",
                        NetFrameworkVersion = "NetFrameworkVersion.V4_6",
                    }
                };
                var webSite_lro = await webSiteCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, websiteName, webSiteData);
                var webSite = webSite_lro.Value;

                var planCollection = resourceGroup1.GetWebSites();
                var planData = new AppServicePlanData(region)
                {
                };
                var planResource_lro =await planCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, planName, webSiteData);
                var planResource = planResource_lro.Value;
                SiteFunctionCollection functionAppCollection = webSite.GetSiteFunctions();
                var functionData = new FunctionEnvelopeData()
                {
                };
                var funtion_lro =await functionAppCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, app1Name, functionData);
                var function = funtion_lro.Value;

                Utilities.Log("Created function app " + function.Data.Name);
                Utilities.Print(function);

                //============================================================
                // Create a second function app with the same app service plan

                Utilities.Log("Creating another function app " + app2Name + " in resource group " + rg1Name + "...");
                var function2Data = new FunctionEnvelopeData()
                {
                };
                var funtion2_lro = functionAppCollection.CreateOrUpdate(Azure.WaitUntil.Completed, app2Name, functionData);
                var function2 = funtion_lro.Value;

                Utilities.Log("Created function app " + function2.Data.Name);
                Utilities.Print(function2);

                //============================================================
                // Create a third function app with the same app service plan, but
                // in a different resource group

                Utilities.Log("Creating another function app " + app3Name + " in resource group " + rg2Name + "...");
                var webSite2Collection = resourceGroup2.GetWebSites();
                var webSite2Data = new WebSiteData(region)
                {
                    SiteConfig = new Azure.ResourceManager.AppService.Models.SiteConfigProperties()
                    {
                        WindowsFxVersion = "PricingTier.StandardS1",
                        NetFrameworkVersion = "NetFrameworkVersion.V4_6",
                    }
                };
                var webSite2_lro = await webSiteCollection.CreateOrUpdateAsync(Azure.WaitUntil.Completed, website2Name, webSiteData);
                var webSite2 = webSite_lro.Value;
                SiteFunctionCollection function2AppCollection = webSite2.GetSiteFunctions();
                var function3Data = new FunctionEnvelopeData()
                {
                };
                var funtion3_lro = functionAppCollection.CreateOrUpdate(Azure.WaitUntil.Completed, app1Name, functionData);
                var function3 = funtion_lro.Value;
                await function3.UpdateAsync(WaitUntil.Completed, function3Data);

                Utilities.Log("Created function app " + function3.Data.Name);
                Utilities.Print(function3);

                //============================================================
                // stop and start app1, restart app 2
                Utilities.Log("Stopping app " + webSite.Data.Name);
                await webSite.StopAsync();
                Utilities.Log("Stopped app " + webSite.Data.Name);
                Utilities.Print(webSite);
                Utilities.Log("Starting app " + webSite.Data.Name);
                await webSite.StartAsync();
                Utilities.Log("Started app " + webSite.Data.Name);
                Utilities.Print(webSite);
                Utilities.Log("Restarting app " + webSite2.Data.Name);
                await webSite2.RestartAsync();
                Utilities.Log("Restarted app " + webSite2.Data.Name);
                Utilities.Print(webSite2);

                //=============================================================
                // List function apps

                Utilities.Log("Printing list of function apps in resource group " + rg1Name + "...");

                await foreach (SiteFunctionResource functionApp in functionAppCollection.GetAllAsync())
                {
                    Utilities.Print(functionApp);
                }

                Utilities.Log("Printing list of function apps in resource group " + rg2Name + "...");

                await foreach (SiteFunctionResource functionApp in function2AppCollection.GetAllAsync())
                {
                    Utilities.Print(functionApp);
                }

                //=============================================================
                // Delete a function app

                Utilities.Log("Deleting function app " + app1Name + "...");
                await function.DeleteAsync(WaitUntil.Completed);
                Utilities.Log("Deleted function app " + app1Name + "...");

                Utilities.Log("Printing list of function apps in resource group " + rg1Name + " again...");
                await foreach (SiteFunctionResource functionApp in functionAppCollection.GetAllAsync())
                {
                    Utilities.Print(functionApp);
                }
            }
            finally
            {
                try
                {
                    Utilities.Log("Deleting Resource Group: " + rg2Name);
                    await resourceGroup2.DeleteAsync(WaitUntil.Completed);
                    Utilities.Log("Deleted Resource Group: " + rg2Name);
                    Utilities.Log("Deleting Resource Group: " + rg1Name);
                    await resourceGroup2.DeleteAsync(WaitUntil.Completed);
                    Utilities.Log("Deleted Resource Group: " + rg1Name);
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception g)
                {
                    Utilities.Log(g);
                }
            }
        }

        public static async Task Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

                // Print selected subscription
                Utilities.Log("Selected subscription: " + client.GetSubscriptions().Id);

                await RunSample(client);
            }
            catch (Exception e)
            {
                Utilities.Log(e);
            }
        }
    }
}