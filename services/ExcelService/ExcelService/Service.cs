using System.Configuration.Install;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using ExcelService.Properties;
using log4net;

namespace ExcelService
{
    public class Service : ServiceBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (Service));

        private static bool IsServiceInstalled()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == Settings.Default.ServiceName);
        }

        public static void InstallService()
        {
            if (IsServiceInstalled())
            {
                UninstallService();
            }

            ManagedInstallerClass.InstallHelper(new[] { Assembly.GetExecutingAssembly().Location });
        }

        public static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new[] { "/u", Assembly.GetExecutingAssembly().Location });
        } 

        public Service()
        {
            ServiceName = Settings.Default.ServiceName;
        }

        protected override void OnStart(string[] args)
        {
            Log.Debug("Starting service");
            Startup.StartWebApp(Settings.Default.ServiceServerUrl);
            Log.Debug("Service started");
        }

        protected override void OnStop()
        {
            Log.Debug("Stopping service");
            Startup.StopWebApp();
            Log.Debug("Service stopped");
        }
    }
}
