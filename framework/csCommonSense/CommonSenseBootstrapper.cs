// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommonSenseBootstrapper.cs" company="">
//   
// </copyright>
// <summary>
//   The common sense bootstrapper.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace csCommon
{
    using csCommon.Utils;

    #region

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.ComponentModel.Composition.Primitives;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Markup;
    using System.Windows.Threading;

    using Caliburn.Micro;

    using csShared;
    using Microsoft.Win32;
    using Logging;

    #endregion

    // The bootstrapper procedure changed in caliburn.micro version 2.0 (from version 1.5)
    // See http://cureos.blogspot.nl/2014/06/upgrading-caliburn-micro-from-15-to-20.html
    // See http://caliburnmicro.com/documentation/migrating-to-2.0.0

    /// <summary>
    /// Default implementation to bootstrap Common Sense framework.
    /// Example:
    ///    // IMain is the root view!
    ///    public class AppBootstrapper : CommonSenseBootstrapper<IMain> 
    ///    {
    ///        // Load also main-app (needed if plugins are defined in main app)
    ///        protected override Assembly[] AdditionalAssembly()
    ///        {
    ///            return new Assembly[] { Assembly.GetExecutingAssembly() };
    ///        }
    /// }

    /// </summary>
    /// <typeparam name="T">
    /// </typeparam>
    public abstract class CommonSenseBootstrapper<T> : BootstrapperBase
        where T : class
    {

        static CommonSenseBootstrapper()
        {
            // Enables this to log caliburn message to debug log:
            // LogManager.GetLog = type => new CaliburnLogging(type);
            
            try
            {
                // Check if Microsoft Surface Runtime is installed
                var productName = Registry.ClassesRoot.OpenSubKey("Installer\\Products\\D93B2C96060FDA948877104C41A44842", false)?.GetValue("ProductName") as string;
                if ((productName == null) || (productName != "Microsoft Surface 2.0 Runtime"))
                {
                    LogCs.LogError("Microsoft Surface 2.0 Runtime NOT installed, needed to run CS");
                }
            } catch(Exception)
            { }
            
        }

        #region Fields

        /// <summary>
        ///     The container.
        /// </summary>
        private CompositionContainer container;

        #endregion

        

        #region Methods

        /// <summary>
        /// Add plugin assemblies that do not use the cs*.dll pattern 
        /// </summary>
        /// <returns>
        /// The <see cref="Assembly[]"/>.
        /// </returns>
        protected abstract Assembly[] AdditionalAssembly();

        /// <summary>
        /// The build up.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        protected override void BuildUp(object instance)
        {
            this.container.SatisfyImportsOnce(instance);
        }

        /// <summary>
        ///     Configure which MEF modules to load
        /// </summary>
        protected override void Configure()
        {
            // LogManager.GetLog = type => new csShared.Utils.DebugLogger(type);
            var catalog =
                new AggregateCatalog(
                    AssemblySource.Instance.Select(x => new AssemblyCatalog(x)).OfType<ComposablePartCatalog>());

            this.container = new CompositionContainer(catalog);

            var batch = new CompositionBatch();

            batch.AddExportedValue(this.CustomWindowManager());
            batch.AddExportedValue(this.CustomEventAggregator());
            batch.AddExportedValue(this.container);
            batch.AddExportedValue(catalog);

            this.container.Compose(batch);
        }

        /// <summary>
        ///     Override this method for a custom Event Aggregator (default caliburn implementation is used)
        /// </summary>
        /// <returns>
        ///     The <see cref="IEventAggregator" />.
        /// </returns>
        protected virtual IEventAggregator CustomEventAggregator()
        {
            return new EventAggregator();
        }

        /// <summary>
        ///     TOverride this method for a custom Window Manager (default caliburn implementation is used)
        /// </summary>
        /// <returns>
        ///     The <see cref="IWindowManager" />.
        /// </returns>
        protected virtual IWindowManager CustomWindowManager()
        {
            return new WindowManager();
        }

        /// <summary>
        /// The get all instances.
        /// </summary>
        /// <param name="serviceType">
        /// The service type.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable"/>.
        /// </returns>
        protected override IEnumerable<object> GetAllInstances(Type serviceType)
        {
            return this.container.GetExportedValues<object>(AttributedModelServices.GetContractName(serviceType));
        }

        /// <summary>
        /// This creates an instance !
        /// </summary>
        /// <param name="serviceType">
        /// The service type.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// </exception>
        protected override object GetInstance(Type serviceType, string key)
        {
            var contract = string.IsNullOrEmpty(key) ? AttributedModelServices.GetContractName(serviceType) : key;
            var exports = this.container.GetExportedValues<object>(contract);

            var enumerable = exports as object[] ?? exports.ToArray();
            if (enumerable.Any())
            {
                return enumerable.First();
            }

            throw new Exception(string.Format("Could not locate any instances of contract {0}.", contract));
        }

        /// <summary>
        /// The on exit.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected override void OnExit(object sender, EventArgs e)
        {
            foreach (var plugin in AppStateSettings.Instance.Plugins.Where(k => k.IsRunning))
            {
                plugin.Stop();
            }

            base.OnExit(sender, e);
        }

        /// <summary>
        /// Check whether we have installed the application using appname.exe TNO.
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
        protected override void OnStartup(object sender, StartupEventArgs e)
        {
#if RELEASE_WITH_KEY
            if (!Properties.Settings.Default.HasValidLicense)
            {
                if (e.Args.Contains("TNO") || e.Args.Contains("tno"))
                {
                    Properties.Settings.Default.HasValidLicense = true;
                    Properties.Settings.Default.LicensedOn = DateTime.Now;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    Console.WriteLine("License not valid! Please contact TNO to purchase a license.");
                    Application.Shutdown(-1);
                    return;
                }
            }
#endif
            this.DisplayRootViewFor(typeof(T));
            base.OnStartup(sender, e);
        }

        /// <summary>
        /// The on unhandled exception.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;

            // base.OnUnhandledException(sender, e);
            if (!(e.Exception is XamlParseException))
            {
                return;
            }

            Console.WriteLine(e.Exception.Message);
            var inner = e.Exception.InnerException;
            while (inner != null)
            {
                Console.WriteLine(inner.Message);
                inner = inner.InnerException;
            }
        }

        /// <summary>
        ///     The select assemblies.
        /// </summary>
        /// <returns>
        ///     The <see cref="IEnumerable" />.
        /// </returns>
        protected override IEnumerable<Assembly> SelectAssemblies()
        {
            var result = new List<Assembly>();

            // Add assemblies specified by owner
            if (this.AdditionalAssembly() != null)
            {
                foreach (var additional in this.AdditionalAssembly())
                {
                    AddAssembly(result, additional);
                }
            }

            var d = Directory.GetCurrentDirectory();
            Debug.WriteLine(string.Format("Loading plugins (cs*.dll) in directory {0}", d));
            foreach (var pluginDllFile in Directory.GetFiles(d, "cs*.dll"))
            {
                try
                {
                    var a = Assembly.LoadFrom(pluginDllFile);
                    var assemblyTypes = a.GetTypes();
                    var typeAndSupportedInterfaces = new Dictionary<Type, IEnumerable<string>>();
                    foreach (var assembly in assemblyTypes)
                    {
                        var supportedInterfaces = new[]
                                                      {
                                                          "IModule", "IPlugin", "IDocument", "IDataService", "IClient", 
                                                          "IModel"
                                                      };
                        var dllInterfaces = assembly.GetInterfaces().Select(x => x.Name);
                        var implementedInterfaces = dllInterfaces.Where(p => supportedInterfaces.Contains(p));
                        if (implementedInterfaces.Any())
                        {
                            typeAndSupportedInterfaces.Add(assembly, implementedInterfaces);

                            // Could to here; the plugin must be loaded
                        }
                    }

                    if (typeAndSupportedInterfaces.Any())
                    {
                        Debug.WriteLine(string.Format("Load plugin dll {0}", pluginDllFile));
                        foreach (KeyValuePair<Type, IEnumerable<string>> entry in typeAndSupportedInterfaces)
                        {
                            Debug.WriteLine(string.Format(
                                " * Type {0} implemented interface(s) {1} ", 
                                entry.Key.Name, 
                                string.Join(",", entry.Value)));
                        }

                        AddAssembly(result, a);
                    }
                }
                catch (SystemException e)
                {
                    Logging.LogCs.LogException(string.Format("Failed to load plugin {0} ({1}).", pluginDllFile, e.Message), e);
                    MessageBox.Show(string.Format("Failed to load plugin {0}.", pluginDllFile), "Load failure");
                    if (e is System.Reflection.ReflectionTypeLoadException)
                    {
                        var typeLoadException = e as ReflectionTypeLoadException;
                        var loaderExceptions = typeLoadException.LoaderExceptions;
                        foreach(var missing in loaderExceptions)
                        {
                            Logging.LogCs.LogMessage("*) Loader exception: " + missing.Message);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// The add assembly.
        /// </summary>
        /// <param name="result">
        /// The result.
        /// </param>
        /// <param name="a">
        /// The a.
        /// </param>
        private static void AddAssembly(ICollection<Assembly> result, Assembly a)
        {
            if (!result.Contains(a))
            {
                result.Add(a);
            }
        }

        #endregion
    }
}