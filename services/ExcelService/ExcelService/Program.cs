using System;
using System.IO;
using System.ServiceProcess;
using ExcelService.Properties;

namespace ExcelService
{
    public class Program
    {
        static void Main(string[] args)
        {
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "/i":
                        Service.InstallService();
                        break;
                    case "/u":
                        Service.UninstallService();
                        break;
                    case "/console":
                        log4net.Config.XmlConfigurator.Configure(new FileInfo("console.log4net"));
                        Startup.StartWebApp(Settings.Default.ConsoleServerUrl);
                        Console.ReadLine();
                        Startup.StopWebApp();
                        break;
                }
            }

            if (args.Length != 0) return;
            log4net.Config.XmlConfigurator.Configure();
            ServiceBase.Run(new Service());
        }
    }
}
