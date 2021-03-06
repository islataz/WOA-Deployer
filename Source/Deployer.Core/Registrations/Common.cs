using System;
using System.Net.Http;
using System.Threading.Tasks;
using Deployer.Core.DevOpsBuildClient;
using Deployer.Core.Scripting;
using Grace.DependencyInjection;
using Octokit;
using Zafiro.Core;
using Zafiro.Core.FileSystem;

namespace Deployer.Core.Registrations
{
    public class Common : IConfigurationModule
    {
        public void Configure(IExportRegistrationBlock block)
        {
            block.Export<ZipExtractor>().As<IZipExtractor>();
            block.Export<FileSystemOperations>().As<IFileSystemOperations>().Lifestyle.Singleton();
            block.Export<Downloader>().As<IDownloader>().Lifestyle.Singleton();
            block.ExportFactory(() => new HttpClient {Timeout = TimeSpan.FromMinutes(30)}).Lifestyle.Singleton();
            block.ExportFactory(() => new GitHubClient(new ProductHeaderValue("WOADeployer"))).As<IGitHubClient>()
                .Lifestyle.Singleton();
            block.ExportFactory(() => AzureDevOpsBuildClient.Create(new Uri("https://dev.azure.com")))
                .As<IAzureDevOpsBuildClient>().Lifestyle.Singleton();
            block.ExportFactory((IDownloader downloader) => XmlDeviceRepository(downloader)).As<IDevRepo>().Lifestyle
                .Singleton();
        }

        private static XmlDeviceRepository XmlDeviceRepository(IDownloader downloader)
        {
            var definition = "https://raw.githubusercontent.com/WOA-Project/Deployment-Feed/master/Deployments.xml";
            return new XmlDeviceRepository(new Uri(definition), downloader);
        }
    }
}