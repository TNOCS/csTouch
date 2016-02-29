
using System;
using System.Web.Http;
using ExcelService.Controllers;
using ExcelService.Hubs;
using ExcelService.Logging;
using ExcelService.Properties;
using ExcelService.Sessions;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Ninject;
using Owin;

namespace ExcelService
{
    public class Startup
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Startup));
        private static IDisposable app;

        public static void Configure(string workbookFolder, string usernameHeader)
        {
            Settings.Default.WorkbookFolder = workbookFolder;
            Settings.Default.UsernameHeader = usernameHeader;
        }

        public static void StartWebApp(string serverAddress)
        {
            Log.Info("Starting web application");

            app = WebApp.Start<Startup>(serverAddress);

            Log.InfoFormat("Server running at {0}", serverAddress);
        }

        public static void StopWebApp()
        {
            if (app == null) return;
            Log.InfoFormat("Stopping web application: {0}", app);
            app.Dispose();
            app = null;
            Log.Info("Web application stopped");
        }

        /// <summary>
        /// When using WebApp.Start, this is called by convention.
        /// </summary>
        /// <param name="myApp"></param>
        public void Configuration(IAppBuilder myApp)
        {
            Log.Debug("Startup configuration");

            try
            {
                // Enable cross-site access
                myApp.UseCors(CorsOptions.AllowAll);

                Log.Debug("Create Ninject kernel");
                var kernel = new StandardKernel();

                kernel.Bind<IExcelSessionManager>()
                      .To<ExcelSessionManager>()
                      .InSingletonScope();
                kernel.Bind<ILog>()
                      .To<Log4NetLogger>()
                      .WithConstructorArgument(typeof (Type), c => c.Request.ParentRequest.Service);
                

                Log.Debug("Configure WebAPI");
                var config = new HttpConfiguration {DependencyResolver = new NinjectResolver(kernel)};
                config.MapHttpAttributeRoutes();
                config.Routes.MapHttpRoute("ExcelServiceApi", "excel/{controller}/{id}",
                                           new {id = RouteParameter.Optional});
                myApp.UseWebApi(config);

                Log.Debug("Configure SignalR");
                var resolver = new NinjectSignalRDependencyResolver(kernel);
                GlobalHost.DependencyResolver = resolver;

                kernel.Bind(typeof(IHubContext)).ToMethod(context =>resolver.Resolve<IConnectionManager>().GetHubContext<ExcelHub>()).WhenInjectedInto(new[]{typeof(ApiController), typeof(ExcelSessionManager)});

                
                myApp.MapSignalR();
            }
            catch (Exception ex)
            {
                Log.ErrorFormat("Error on startup configuration: {0}", ex.Message);

                var currEx = ex.InnerException;
                while (currEx != null)
                {
                    Log.Error(currEx.Message);
                    currEx = currEx.InnerException;
                }
            }

            Log.Debug("Finished startup configuration");
        }

    }
}
