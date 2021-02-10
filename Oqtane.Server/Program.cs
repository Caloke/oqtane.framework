using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Oqtane.Infrastructure;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using System.IO;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Oqtane.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {


            //Check for the Debugger is attached or not if attached then run the application in IIS or IISExpress
            var isService = false;

            //when the service start we need to pass the --service parameter while running the .exe
            if (Debugger.IsAttached == false && args.Contains("--service"))
            {
                isService = true;
            }

            if (isService)
            {
                //Get the Content Root Directory
                var pathToContentRoot = Directory.GetCurrentDirectory();

                string ConfigurationFile = "appsettings.json"; //Configuration file.
                string portNo = "5000"; //Port

                var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                pathToContentRoot = Path.GetDirectoryName(pathToExe);

                //Get the json file and read the service port no if available in the json file.
                string AppJsonFilePath = Path.Combine(pathToContentRoot, ConfigurationFile);

                if (File.Exists(AppJsonFilePath))
                {
                    using (StreamReader sr = new StreamReader(AppJsonFilePath))
                    {
                        string jsonData = sr.ReadToEnd();
                        JObject jObject = JObject.Parse(jsonData);
                        if (jObject["ServicePort"] != null)
                            portNo = jObject["ServicePort"].ToString();

                    }
                }

                var host = WebHost.CreateDefaultBuilder(args)
                .UseContentRoot(pathToContentRoot)
                .UseStartup<Startup>()
                .UseUrls("http://*:" + portNo)
                .ConfigureLocalizationSettings()
                .Build();

                host.RunAsService();
            }
            else
            {
                var host = BuildWebHost(args);
                using (var serviceScope = host.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
                {
                    var databaseManager = serviceScope.ServiceProvider.GetService<IDatabaseManager>();
                    databaseManager.Install();
                }
                host.Run();
            }

        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .Build())
                .UseStartup<Startup>()
                .UseUrls("http://*:5000")
                .ConfigureLocalizationSettings()
                .Build();
    }
}
