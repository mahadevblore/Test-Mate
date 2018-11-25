using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Xml;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.TestManagement.Client;

namespace Automation.Library
{
    public class TfsUtils
    {
        public static string LatestTestBuild { get; set; }

        public static string GetProjectName(string projName)
        {
            var doc = new XmlDocument();
            doc.Load(ProductConfiguration.AssemblyDirectory + "\\" + "ProjectsMap.xml");
            XmlNode node = doc.SelectSingleNode(@"//Project/ProjectMapping");
            var element = (XmlElement)node;
            return element.GetAttribute(projName);
        }

        public static string GetLatestTestBuild()
        {
            return LatestTestBuild;
        }

        public static string GetLastSuccededDropLocation(string projName, string buildDefinitionName)
        {
            projName = GetProjectName(projName);
            var cred = new NetworkCredential("SERVICE_ACCOUNT_NAME", "SERVICE_ACCOUNT_PASSWORD", "SERVICE_ACCOUNT_DOMAIN");

            var configurationServerUri = new Uri("TFS_URL_TILL_COLLECTION");
            TfsConfigurationServer configurationServer =
                    TfsConfigurationServerFactory.GetConfigurationServer(configurationServerUri);

            CatalogNode configurationServerNode = configurationServer.CatalogNode;

            // Query the children of the configuration server node for all of the team project collection nodes
            ReadOnlyCollection<CatalogNode> tpcNodes = configurationServerNode.QueryChildren(
                    new Guid[] { CatalogResourceTypes.ProjectCollection },
                    false,
                    CatalogQueryOptions.None);

            foreach (CatalogNode tpcNode in tpcNodes)
            {
                ServiceDefinition tpcServiceDefinition = tpcNode.Resource.ServiceReferences["Location"];

                ILocationService configLocationService = configurationServer.GetService<ILocationService>();
                Uri tpcUri = new Uri(configLocationService.LocationForCurrentConnection(tpcServiceDefinition));

                // Actually connect to the team project collection
                TfsTeamProjectCollection tpc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(tpcUri);
                ITestManagementService tms = tpc.GetService<ITestManagementService>();
                ITestManagementTeamProject proj = tms.GetTeamProject(projName);
                IBuildServer tfsBuildServer = tpc.GetService<IBuildServer>();
                // Reading from XML
                try
                {
                    IBuildServer buildServer = (IBuildServer)tpc.GetService(typeof(IBuildServer));
                    //Specify query
                    IBuildDetailSpec spec = buildServer.CreateBuildDetailSpec(projName.Trim(), buildDefinitionName.Trim());
                    spec.InformationTypes = null; // for speed improvement
                    spec.QueryOrder = BuildQueryOrder.FinishTimeAscending; //get the latest build only
                    spec.QueryOptions = QueryOptions.All;

                    IBuildDetail bDetail = buildServer.QueryBuilds(spec).Builds.OrderBy(x => x.CompilationStatus == BuildPhaseStatus.Succeeded).Last();
                    LatestTestBuild = bDetail.DropLocation;
                    LoggerUtil.LogMessageToFile("The ip resolve");
                    IPAddress ip = null;
                    string arr2 = LatestTestBuild.Split('\\')[2];
                    Network.GetResolvedConnecionIPAddress(LatestTestBuild.Split('\\')[2], out ip);
                    string temp = string.Join(@"\",LatestTestBuild.Split('\\').Select(s => s.Replace(arr2, ip.ToString())).ToArray());
                    LatestTestBuild = temp;
                    LoggerUtil.LogMessageToFile(LatestTestBuild);
                    break;
                }
                catch { }
            }
            return LatestTestBuild;
        }
    }
}
