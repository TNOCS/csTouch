using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Threading;
using Caliburn.Micro;
using ESRI.ArcGIS.Client.Behaviors;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using csShared;
using csShared.Documents;
using csShared.Interfaces;
using System.ComponentModel.Composition;
using System.CodeDom.Compiler;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Surface.Presentation.Controls;
using csCommon;

namespace csGeoLayers.Plugins.DemoScript
{
    [Export(typeof(IPlugin))]
    public class DemoScript : PropertyChangedBase, IPlugin
    {

        //private DispatcherTimer _dt;

        private static string dynCallSource =
           "using System;using System.Threading; \r\n  namespace UrbanFlood.Utils { \r\n public class DynamicCalls { \r\n public dynamic App; \r\n " +
           "public void DoStuff() { " +
           "try { \r\n %template% \r\n %script% \r\n  } " +
           "catch(ThreadAbortException abortException){ Console.WriteLine(\"ThreadAborted\");}  }}}";

        public bool CanStop { get { return false; } }

        public NotificationPlugin NotificationLayer { get; set; }

        private ISettingsScreen _settings;

        public ISettingsScreen Settings
        {
            get { return _settings; }
            set { _settings = value; NotifyOfPropertyChange(() => Settings); }
        }

        private IPluginScreen _screen; 

        public IPluginScreen Screen
        {
            get { return _screen; }
            set { _screen = value; NotifyOfPropertyChange(() => Screen); }
        }

        private List<FloatingElement> _ownFloatingElements = new List<FloatingElement>();
        private List<Guid> _ownNotifications = new List<Guid>();

        private bool _hideFromSettings = false;

        public bool HideFromSettings
        {
            get { return _hideFromSettings; }
            set { _hideFromSettings = value; NotifyOfPropertyChange(() => HideFromSettings); }
        }



        public bool KeepScriptActive = false;

        public event EventHandler ScriptFinished;
        public event EventHandler ScriptStarted;

        public int Priority
        {
            get { return 1; }
        }

        public string Icon
        {
            get { return @"icons\globe.png"; }
        }

        private bool _isRunning;

        public bool IsRunning
        {
            get { return _isRunning; }
            set { _isRunning = value; NotifyOfPropertyChange(() => IsRunning); }
        }

        public AppStateSettings AppState { get; set; }

        public FloatingElement Element { get; set; }

        public String ScriptPath { get; set; }


        private bool _scriptRunning;

        public bool ScriptRunning
        {
            get { return _scriptRunning; }
            set { _scriptRunning = value; NotifyOfPropertyChange(()=>ScriptRunning); }
        }
        

        public string Name
        {
            get { return "DemoScript"; }
        }

        public void StartScript()
        {
            StartScript(ScriptPath);
           
        }

        public void StartScriptNoNotify()
        {
            StartScript(ScriptPath,false);
        }

        private double Xres = 0;
        private double Yres = 0;


        public void StartScript(string file, bool notifyHandler = true)
        {
            try
            {
                Execute.OnUIThread(() =>
                {
                    Xres = Application.Current.MainWindow.ActualWidth;
                    Yres = Application.Current.MainWindow.ActualHeight;
                });

                string scriptCode = File.ReadAllText(file);
                string classCode = dynCallSource.Replace("%script%", scriptCode);
                string templateCode = File.ReadAllText("template.mcs");
                classCode = classCode.Replace("%template%", templateCode);
                
                var codeProvider = CodeDomProvider.CreateProvider("CSharp");

                var parameters = new CompilerParameters();
                parameters.ReferencedAssemblies.Add("mscorlib.dll");
                parameters.ReferencedAssemblies.Add("system.core.dll");
                parameters.ReferencedAssemblies.Add("System.dll");
                //parameters.ReferencedAssemblies.Add("System.Windows.dll");
                //parameters.ReferencedAssemblies.Add("PresentationFramework.dll");
                parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
                parameters.GenerateExecutable = false;
                parameters.GenerateInMemory = true;

                CompilerResults results = codeProvider.CompileAssemblyFromSource(parameters, classCode);
                var modelType = results.CompiledAssembly.GetType("UrbanFlood.Utils.DynamicCalls");
                dynamic viewModel = Activator.CreateInstance(modelType);

                viewModel.App = this;
                try
                {
                    if (ScriptStarted != null && notifyHandler) 
                        ScriptStarted(this, null);
                    IsRunning = true;
                    ScriptRunning = true;
                    viewModel.DoStuff();
                    
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }
            }
            catch (Exception err)
            {
                string errMsg = err.Message;
            }
            finally
            {
                ScriptRunning = false;
                if (ScriptFinished != null && notifyHandler) 
                    ScriptFinished(this, null);
            }

        }
       

        public void Init()
        {
        }
        

        Random rnd = new Random();
        
        public void Start()
        {
            IsRunning = true;
        }

        public void Pause()
        {
            IsRunning = false;
        }

        public void Stop()
        {
            IsRunning = false;
            ScriptRunning = false;
        }

        public bool IsActive()
        {
            return IsRunning || ScriptRunning;
        }

        private bool stillwaiting = true;
        private string interactionname = "";
        public bool WaitForInteraction(string name, int timeout)
        {
            if (!IsActive()) return false;

            var now = DateTime.Now;
            interactionname = name;
            AppState.InteractionOccurred += AppState_InteractionOccurred;
            while (stillwaiting)
            {
                if (ScriptRunning == false) 
                    return false;
                Thread.Sleep(100);
                var v = DateTime.Now - now;
                if (v.TotalMilliseconds > timeout)
                {
                    stillwaiting = true;
                    return false;
                }
            }
            stillwaiting = true;
            return true;
        }


        public string WaitForInteraction(int timeout)
        {
            if (!IsActive()) return "";

            var now = DateTime.Now;
            AppState.InteractionOccurred += AppState_SomeInteractionOccurred;
            while (stillwaiting)
            {
                if (ScriptRunning == false)
                    return "";
                Thread.Sleep(100);
                var v = DateTime.Now - now;
                if (v.TotalMilliseconds > timeout)
                {
                    stillwaiting = true;
                    return "";
                }
            }
            stillwaiting = true;
            return interactionname;
        }

        public void TriggerScriptCommand(string command)
        {
            if (!IsActive()) return;

            AppState.TriggerScriptCommand(this,command);
        }

        void AppState_InteractionOccurred(object sender, string name, string command)
        {
            if (name == interactionname)
            {
                stillwaiting = false;
            }
        }

        void AppState_SomeInteractionOccurred(object sender, string name, string command)
        {
           stillwaiting = false;
            interactionname = name;
        }

        /// <summary>
        /// Pause a while
        /// </summary>
        /// <param name="msecs"></param>
        public void Pause(int msecs)
        {
            var passed = msecs;
            while (passed > 0)
            {
                if (ScriptRunning == false) 
                    return;
                Thread.Sleep(50);
                passed -= 50;
            }
        }

        /// <summary>
        ///  Change map type
        /// </summary>
        /// <param name="maptype"></param>
        public void ChangeMapType(string maptype)
        {
            if (!IsActive()) return;
            AppState.ViewDef.ChangeMapType(maptype);
        }

        public double GetXResolution()
        {
            return Xres;
        }

        public double GetYResolution()
        {
            return Yres;
        }

        /// <summary>
        /// Closes all Floating elements and plugins
        /// </summary>
        public void CloseAll()
        {
            if (!IsActive()) return;

            Execute.OnUIThread(() =>
            {
                try
                {
                    foreach (IPlugin pi in AppStateSettings.GetInstance().Plugins)
                    {
                        try
                        {
                            if (pi.CanStop) pi.Stop();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                    }
                    AppStateSettings.GetInstance().FloatingItems.Clear();                    
                    NotificationLayer.Clear();
                    _ownFloatingElements.Clear();
                    _ownNotifications.Clear();

                    AppState.TriggerDeleteAllTextNotifications();
                }
                catch
                {
                }
            });
        }


        /// <summary>
        /// Closes all Floating elements and plugins
        /// </summary>
        public void CloseAllText()
        {
            if (!IsActive()) return;

            Execute.OnUIThread(() =>
            {
                try
                {
                    NotificationLayer.Clear();
                }
                catch
                {
                }
            });
        }

        /// <summary>
        /// Closes only the floating elements that were created by this app
        /// </summary>
        public void CloseOwn()
        {
            if (!IsActive()) return;

            Execute.OnUIThread(() =>
                                   {
                                       foreach (var fe in _ownFloatingElements)
                                       {
                                           AppStateSettings.GetInstance().FloatingItems.Remove(fe);
                                       }
                                       _ownFloatingElements.Clear();

                                       foreach (var id in _ownNotifications)
                                       {
                                           AppState.TriggerDeleteNotification(new NotificationEventArgs() {Id = id});
                                       }
                                       _ownNotifications.Clear();
                                   });
        }

        //public bool IsTouchEnabled
        //{
        //    get { return Application.Current.MainWindow.IsHitTestVisible};
        //    set { 
        //Application.Current.Dispatcher.Invoke(new Action(delegate
        //    {
        //     Application.Current.MainWindow.IsHitTestVisible = value;
        //    Application.Current.MainWindow.IsManipulationEnabled = value;}
        // }));
        //}

        /// <summary>
        /// Disable touch / mouse responsiveness to application
        /// </summary>
        public void DisableTouch()
        {
            if (!IsActive()) return;

            Execute.OnUIThread(() =>
                                   {
                                       AppState.IsTouchEnabled = false;
                                   });
        }


        /// <summary>
        /// Enable touch / mouse responsiveness to application
        /// </summary>
        public void EnableTouch()
        {
            if (!IsActive()) return;

            Execute.OnUIThread(() =>
                                   {
                                       AppState.IsTouchEnabled = true;
                                   });
        }

        public void SetMapMax(double maxresolution)
        {
            if (!IsActive()) return;

            AppState.ViewDef.MapControl.MaximumResolution = maxresolution;
        }

        public void SetMapMin(double minresolution)
        {
            if (!IsActive()) return;

            AppState.ViewDef.MapControl.MinimumResolution = minresolution;
        }

        public void SetMapConstraint(double topleftX = -180, double topleftY = -90,double bottomrightX=180, double bottomrightY=90, bool setExtent=false)
        {
            if (!IsActive()) return;

            var wm = new WebMercator();
            var tl = new MapPoint(topleftX,topleftY,new SpatialReference(4326));
            var br = new MapPoint(bottomrightX, bottomrightY, new SpatialReference(4326));
            var convertedtl = (MapPoint) wm.FromGeographic(tl);
            var convertedbr = (MapPoint)wm.FromGeographic(br);
            var e = new Envelope(convertedtl, convertedbr);

            Execute.OnUIThread(() =>
           {
               var b = System.Windows.Interactivity.Interaction.GetBehaviors(AppState.ViewDef.MapControl);
               var rem = b.OfType<ConstrainExtentBehavior>().FirstOrDefault();
               if (rem != null)
                   rem.ConstrainedExtent = e;
               else
                   b.Add(new ConstrainExtentBehavior() { ConstrainedExtent = e });
               if (setExtent)
                   AppState.ViewDef.MapControl.Extent = e;
           });
            
        }


        public void LockMap()
        {
            if (!IsActive()) return;

            Execute.OnUIThread(() =>
            {
                var e = AppState.ViewDef.MapControl.Extent;
                var b = System.Windows.Interactivity.Interaction.GetBehaviors(AppState.ViewDef.MapControl);
                var rem = b.OfType<ConstrainExtentBehavior>().FirstOrDefault();
                if (rem != null)
                    rem.ConstrainedExtent = e;
                else
                    b.Add(new ConstrainExtentBehavior() { ConstrainedExtent = e });
            });

        }

        public void RemoveMapConstraint()
        {
            var wm = new WebMercator();
            var tl = new MapPoint(-280, -79, new SpatialReference(4326));
            var br = new MapPoint(280, 79, new SpatialReference(4326));
            var convertedtl = (MapPoint)wm.FromGeographic(tl);
            var convertedbr = (MapPoint)wm.FromGeographic(br);
            var e = new Envelope(convertedtl, convertedbr);

            Execute.OnUIThread(() =>
            {
                var b = System.Windows.Interactivity.Interaction.GetBehaviors(AppState.ViewDef.MapControl);
                var rems = b.OfType<ConstrainExtentBehavior>().ToList();

                foreach (var rem in rems)
                    b.Remove(rem);
                b.Add(new ConstrainExtentBehavior() { ConstrainedExtent = e });
                //SetMapMin(0.5);
               // SetMapMax(100);
            });
        }

        public bool ZoomBack;

        public void DisableZoomBack()
        {
            if (!IsActive()) return;

            ZoomBack = false;
        }

        public void EnableZoomBack()
        {
            if (!IsActive()) return;

            ZoomBack = true;
        }

        public bool DisableClose;

        public void DisableCloseAll()
        {
            if (!IsActive()) return;

            DisableClose = false;
        }

        public void EnableCloseAll()
        {
            if (!IsActive()) return;

            DisableClose = true;
        }


        /// <summary>
        /// Zoom to extent
        /// </summary>
        /// <param name="xmin"></param>
        /// <param name="ymin"></param>
        /// <param name="xmax"></param>
        /// <param name="ymax"></param>
        public void ZoomTo(double xmin, double ymin, double xmax, double ymax, int animTime = 1000, bool effect = false, bool webMercator = false)
        {
            if (!IsActive()) return;

            var e = new Envelope(new MapPoint(xmin, ymin), new MapPoint(xmax, ymax));
            if (webMercator)
            {
                var wm = new WebMercator();
                var tl = new MapPoint(xmin, ymin, new SpatialReference(4326));
                var br = new MapPoint(xmax, ymax, new SpatialReference(4326));
                var convertedtl = (MapPoint) wm.FromGeographic(tl);
                var convertedbr = (MapPoint) wm.FromGeographic(br);
                e = new Envelope(convertedtl, convertedbr);
                //                                       DemoScript.ZoomTo(-19913273.2361002, -16885380.7637054, 20161743.4494782,
                //                                                         15174632.5847574, effect: true);
            }


            // TODO animate zoom to next place
            Execute.OnUIThread(() =>
            {
                try
                {
                    var b = System.Windows.Interactivity.Interaction.GetBehaviors(AppState.ViewDef.MapControl);
                    var rems = b.OfType<ConstrainExtentBehavior>().ToList();

                    foreach (var rem in rems)
                        b.Remove(rem);

                    if (effect) 
                        {AppState.ViewDef.StartTransition();}
                    else
                    {
                        AppState.ViewDef.MapControl.ZoomDuration = new TimeSpan(0, 0, 0, 0,250);
                        //AppState.ViewDef.MapControl.PanDuration = new TimeSpan(0, 0, 0, 0, animTime);    
                    }
                    AppState.ViewDef.MapControl.MinimumResolution = 0.1;
                    AppState.ViewDef.MapControl.MaximumResolution = 1000000;

                    AppState.TriggerScriptCommand(null,"disableextentchanged");
                    AppState.ViewDef.MapControl.ZoomTo(e);
                    AppState.TriggerScriptCommand(null, "enableextentchanged");
                }
                catch
                {
                }
            });
        }

        public void ZoomToAnimate(double xmin, double ymin, double xmax, double ymax, int animTime = 200, double nrSteps = 20d)
        {
            if (!IsActive()) return;

            try
            {
                Envelope env = null;
                Execute.OnUIThread(() =>
                {
                    AppStateSettings.GetInstance().ViewDef.
                            MapControl.ZoomDuration =
                            new TimeSpan(0, 0, 0, 0, animTime);
                    AppStateSettings.GetInstance().ViewDef.
                        MapControl.PanDuration =
                        new TimeSpan(0, 0, 0, 0, animTime);
                    env = AppStateSettings.GetInstance().ViewDef.MapControl.Extent;
                });
                while (env == null)
                {
                    Thread.Sleep(50);
                    if (ScriptRunning == false) 
                        return;
                }

                bool zoomingIn = (env.XMax - env.XMin) > (xmax - xmin);
                
                double stepSizeXmin = (xmin - env.XMin) / nrSteps;
                double stepSizeYmin = (ymin - env.YMin) / nrSteps;
                double stepSizeXmax = (xmax - env.XMax) / nrSteps;
                double stepSizeYmax = (ymax - env.YMax) / nrSteps;


                double refF = 0;
                for (double step = 0; step <= nrSteps; step++)
                {
                    double relFunc = step/nrSteps; // Getal tussen 0 en 1
                    /*if (zoomingIn)
                        relFunc = Math.Pow(relFunc, 0.5);
                    else
                        relFunc = Math.Pow(relFunc, 2);*/
                    //if (zoomingIn)
                        //relFunc = Math.Sin(((relFunc * Math.PI) - (Math.PI / 2d)));
                    //else
                        relFunc = (1d + Math.Sin(( (relFunc * Math.PI) - (Math.PI / 2d)) )) / 2d ;
                    refF = relFunc;
                    
                    //var nMp = new MapPoint(pt.X + (stepSizeX * step), pt.Y + (stepSizeY * step));
                    var nExt = new Envelope(env.XMin + (stepSizeXmin*step*relFunc),
                                            env.YMin + (stepSizeYmin*step*relFunc),
                                            env.XMax + (stepSizeXmax*step*relFunc),
                                            env.YMax + (stepSizeYmax*step*relFunc));
                    Execute.OnUIThread(() =>
                    {
                        AppStateSettings.GetInstance().ViewDef.MapControl.ZoomTo(nExt);
                    });
                    if (ScriptRunning == false) 
                        return;
                    Thread.Sleep(animTime + 50);
                }
                //string bla = "";
            }
            catch (Exception err)
            {
                // FIXME TODO Deal with exception.
                //string bla = err.Message;
            }
        }

        /// <summary>
        /// Zoom to extent
        /// </summary>
        /// <param name="xmin"></param>
        /// <param name="ymin"></param>
        /// <param name="xmax"></param>
        /// <param name="ymax"></param>
        public void PanTo(double xmin, double ymin, double xmax, double ymax, int animTime = 3000)
        {
            if (!IsActive()) return;

            Execute.OnUIThread(() =>
            {
                try
                {
                    AppStateSettings.GetInstance().ViewDef.MapControl.ZoomDuration = new TimeSpan(0, 0, 0, 0, animTime);
                    AppStateSettings.GetInstance().ViewDef.MapControl.PanDuration = new TimeSpan(0, 0, 0, 0, animTime);
                    AppStateSettings.GetInstance().ViewDef.MapControl.PanTo(new Envelope(xmin,ymin,xmax,ymax));
                    
                }
                catch
                {
                }
            });
        }

        /// <summary>
        /// Zoom to extent
        /// </summary>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="animTime"></param>
        public void PanToAnimate(double posX, double posY, int animTime = 3000)
        {
            if (!IsActive()) return;

            try
            {
                //AppStateSettings.GetInstance().ViewDef.MapControl.PanTo(new MapPoint(xmin,ymin));
                //AppStateSettings.GetInstance().ViewDef.MapControl.BeginAnimation(ESRI.ArcGIS.Client.Map.ex);
                MapPoint pt = null;
                Execute.OnUIThread(() =>
                                       {
                                           AppStateSettings.GetInstance().ViewDef.
                                                   MapControl.ZoomDuration =
                                                   new TimeSpan(0, 0, 0, 0, animTime);
                                           AppStateSettings.GetInstance().ViewDef.
                                               MapControl.PanDuration =
                                               new TimeSpan(0, 0, 0, 0, animTime);
                                           pt = AppStateSettings.GetInstance().ViewDef.MapControl.Extent.GetCenter();
                                       });
                while (pt == null)
                {
                    //if (ScriptRunning == false) return;
                    Thread.Sleep(50);
                }
                double nrSteps = 20;
                double stepSizeX = (posX - pt.X) / nrSteps;
                double stepSizeY = (posY - pt.Y)/ nrSteps;

                for (double step = 0; step < nrSteps; step++)
                {
                    var nMp = new MapPoint(pt.X + (stepSizeX*step), pt.Y + (stepSizeY*step));
                    Execute.OnUIThread(() =>
                                           {   
                                               AppStateSettings.GetInstance().ViewDef.MapControl.PanTo(nMp);
                                           });
                    if (ScriptRunning == false)
                        return;
                    Thread.Sleep(100);
                }
            }
            catch (Exception err)
            {
                string bla = err.Message;
            }
        }

        /// <summary>
        /// Shows an already present plugin by changing width / height / position
        /// </summary>
        /// <param name="pluginName"></param>
        /// <param name="sizeX"></param>
        /// <param name="sizeY"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        public void ShowPlugin(string pluginName, double sizeX = 600, double sizeY = 500, double posX = 500, double posY = 500)
        {
            if (!IsActive()) return;

            Execute.OnUIThread(() =>
            {
                try
                {
                    dynamic plugin =
                        AppStateSettings.GetInstance().Plugins.FirstOrDefault(
                            p => p.Name == pluginName);
                    if (plugin != null)
                    {
                        ((FloatingElement)plugin.Element).ScatterViewItem.Width = sizeX;
                        ((FloatingElement)plugin.Element).ScatterViewItem.Height = sizeY;
                        ((FloatingElement)plugin.Element).ScatterViewItem.Center = new Point(posX, posY);

                    }
                }
                catch
                {
                }
            });
        }

        /// <summary>
        /// Hide plugin so it sits on the border again
        /// </summary>
        /// <param name="pluginName"></param>
        public void HidePlugin(string pluginName)
        {
            if (!IsActive()) return;

            Execute.OnUIThread(() =>
            {
                try
                {
                    dynamic plugin =
                        AppStateSettings.GetInstance().Plugins.FirstOrDefault(
                            p => p.Name == pluginName);
                    if (plugin != null)
                    {
                        ((FloatingElement)plugin.Element).Close();
                    }
                }
                catch
                {
                }
            });
        }


        public void ShowText(string text, double margin = 0, double marginLeft = double.NaN, double marginTop = double.NaN, double marginRight = double.NaN,
            double marginBottom = double.NaN, string background = "#FF641946", string foreground = "White", string horAlign = "Left", string verAlign = "Top", double sizex = double.NaN,
            double sizey = double.NaN, double fontSize = 40, string fontFamily = "Segoe UI", string textAlignment="Left", double duration = 5000, bool canbedeleted = true, string script = "")
        {
            if (!IsActive()) return;

            var horizontalAlignment = (HorizontalAlignment) Enum.Parse(typeof(HorizontalAlignment), horAlign); //  .TryParse() HorizontalAlignment.Left;
            var verticalAlignment = (VerticalAlignment) Enum.Parse(typeof(VerticalAlignment), verAlign);
            var marginTotal = new Thickness(margin);

            var textalignment = (TextAlignment) Enum.Parse(typeof (TextAlignment), textAlignment);
            var fontfamily = new FontFamily(fontFamily); 

            if(!double.IsNaN(marginLeft) || !double.IsNaN(marginRight) || !double.IsNaN(marginTop) || !double.IsNaN(marginBottom) )
            {
                marginTotal = new Thickness(marginLeft, marginTop, marginRight, marginBottom);
            }

            Execute.OnUIThread(()=>
                                   {
                                       var n = new NotificationEventArgs()
                                       {
                                           Text = text,
                                           Background = (Brush)new BrushConverter().ConvertFromString(background),
                                           Foreground = (Brush)new BrushConverter().ConvertFromString(foreground),
                                           HorizontalAlignment = horizontalAlignment,
                                           VerticalAlignment = verticalAlignment,
                                           TextAlignment = textalignment,
                                           FontFamily = fontfamily,
                                           Size = new Size(sizex, sizey),
                                           Margin = marginTotal,
                                           FontSize = fontSize,
                                           Duration = TimeSpan.FromMilliseconds(duration),
                                           Style = NotificationStyle.FreeText                                           
                                       };
                                       n.Click += (e, s) =>
                                                      {
                                                          AppState.TriggerScriptCommand(this, "scrpt:"+script);
                                                          //Console.WriteLine("Test");
                                                      };
                                       AppState.TriggerNotification(n);
                                   });
           
            //if (canbedeleted) _ownNotifications.Add(n.Id);
            
        }
        

        /// <summary>
        /// Tries to open a Floating element by looking in all referenced assemblies for the full namespace of the viewmodel
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="args"></param>
        /// <param name="title"></param>
        /// <param name="sizeX"></param>
        /// <param name="sizeY"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        public object OpenWindow(string modelName, object[] args = null, string title = "Window", double sizeX = 600, double sizeY = 400, double posX = 500, double posY = 500, string id = "", bool border = true)
        {
            if (!IsActive()) return null;

            object viewModel = null;
            Type modelType = null;
            try
            {
                //var args2 = new object[] {1, 2, 4};
                try
                {
                    
                    var ea = Assembly.GetEntryAssembly();
                    modelType = ea.GetType(modelName);
                    if (modelType == null)
                    {
                        
                        foreach (var refassNm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            modelType = refassNm.GetType(modelName);
                            if (modelType != null)
                            {
                                break;
                            }
                        }
                    }
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                Execute.OnUIThread(() =>
                {
                    try
                    {
                        if(modelType != null)
                            viewModel = Activator.CreateInstance(modelType, args);
                        if (viewModel != null)
                        {
                            var fe = new FloatingElement()
                            {
                                OpacityDragging = 0.5,
                                OpacityNormal = 1.0,
                                CanMove = true,
                                CanRotate = true,
                                CanScale = true,
                                StartOrientation = 0,
                                Width = sizeX,
                                Height = sizeY,
                                Background = Brushes.DarkOrange,
                                StartPosition = new Point(posX, posY),
                                MinSize = new Size(sizeX, sizeY),
                                ShowsActivationEffects = false,
                                RemoveOnEdge = true,
                                Contained = true,
                                Title = title,
                                Foreground = Brushes.White,
                                DockingStyle = DockingStyles.None,
                                ModelInstance = viewModel,
                            };
                            if (id != "") fe.Id = id;
                            if (!border) fe.Style = Application.Current.FindResource("NoBorder") as Style;
                            AppStateSettings.Instance.FloatingItems.Add(fe);
                            _ownFloatingElements.Add(fe);
                        }
                        //return viewModel;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                string exc = ex.Message;
            }
            return viewModel;
        }


        /// <summary>
        /// Tries to open a text and accompanying images in a preset way to create uniformity
        /// </summary>
        /// <param name="text"></param>
        /// <param name="image1"></param>
        /// <param name="image2"></param>
        /// <param name="image3"></param>
        public void OpenTemplate(string text, string image1, string image2, string image3)
        {
            if (!IsActive()) return;

            if (!String.IsNullOrEmpty(text))
                ShowText(text, marginLeft: 150, marginTop: 150, marginRight:0,marginBottom:0, background: "LightGreen", foreground: "White", sizex: 300, sizey: 500, duration: 10000);
            if (!String.IsNullOrEmpty(image1))
                OpenImage(image1, posX: 1000, posY: 500, sizeX: 200, sizeY: 200);
        }


        /// <summary>
        /// Tries to open a Floating element by looking in all referenced assemblies for the full namespace of the viewmodel
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="args"></param>
        /// <param name="title"></param>
        /// <param name="sizeX"></param>
        /// <param name="sizeY"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        public object OpenImage(string source,  object[] args = null, string title = "Window", string filetype = "image",double sizeX = 600, double sizeY = 400, double posX = 500, double posY = 500, string id="", bool border=false, double opacity = 1)
        {
            if (!IsActive()) return null;

            var ftype = (FileTypes) Enum.Parse(typeof(FileTypes),filetype);

            object viewModel = null;
            
            try
            {               
                Execute.OnUIThread(() =>
                {
                    try
                    {
                        Document doc = new Document() { Location = source, FileType = ftype };
                            var fe = new FloatingElement()
                            {
                                OpacityDragging = 0.5,
                                OpacityNormal = 1.0,
                                CanMove = false,
                                CanRotate = false,
                                CanScale = false,
                                StartOrientation = 0,
                                Width = sizeX,
                                Height = sizeY,
                                Background = Brushes.DarkOrange,
                                StartPosition = new Point(posX, posY),
                                MinSize = new Size(sizeX, sizeY),
                                ShowsActivationEffects = false,
                                RemoveOnEdge = true,
                                Contained = true,
                                Title = title,
                                Document = doc,
                                Foreground = Brushes.White,
                                DockingStyle = DockingStyles.None       ,
                                
                                
                            };
                            if (!border) fe.Style = Application.Current.FindResource("NoBorder") as Style;
                            if (id != "") fe.Id = id;
                            fe.OpacityNormal = opacity;
                            AppStateSettings.Instance.FloatingItems.Add(fe);
                            _ownFloatingElements.Add(fe);
                                                //return viewModel;
                    }
                    catch (Exception)
                    {

                    }
                });
            }
            catch (Exception ex)
            {
                string exc = ex.Message;
            }
            return viewModel;
        }



        /// <summary>
        /// Tries to open a Floating element by looking in all referenced assemblies for the full namespace of the viewmodel
        /// </summary>
        /// <param name="modelName"></param>
        /// <param name="args"></param>
        /// <param name="title"></param>
        /// <param name="sizeX"></param>
        /// <param name="sizeY"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        public object OpenWebpage(string source, object[] args = null, string title = "Window", double sizeX = 600, double sizeY = 400, double posX = 500, double posY = 500, string id = "", bool border = false)
        {
            if (!IsActive()) return null;

            object viewModel = null;

            try
            {
                Execute.OnUIThread(() =>
                {
                    try
                    {
                        
                        Document doc = new Document() { Location = source };
                        viewModel = new WebViewModel(){Doc = doc,DisplayName = title};
                        var fe = new FloatingElement()
                        {
                            OpacityDragging = 0.5,
                            OpacityNormal = 1.0,
                            CanMove = false,
                            CanRotate = false,
                            CanScale = false,
                            StartOrientation = 0,
                            Width = sizeX,
                            Height = sizeY,
                            Background = Brushes.DarkOrange,
                            StartPosition = new Point(posX, posY),
                            MinSize = new Size(sizeX, sizeY),
                            ShowsActivationEffects = false,
                            RemoveOnEdge = true,
                            Contained = true,
                            Title = title,
                            ModelInstance = viewModel,
                            Foreground = Brushes.White,
                            DockingStyle = DockingStyles.None,

                        };
                        if (!border) fe.Style = Application.Current.FindResource("NoBorder") as Style;
                        if (id != "") fe.Id = id;
                        AppStateSettings.Instance.FloatingItems.Add(fe);
                        _ownFloatingElements.Add(fe);
                        //return viewModel;
                    }
                    catch (Exception)
                    {

                    }
                });
            }
            catch (Exception ex)
            {
                string exc = ex.Message;
            }
            return viewModel;
        }
        

        public void CloseWindowId(string id)
        {
            if (!IsActive()) return;

            try
            {
                Execute.OnUIThread(() =>
                {
                    var fe =
                        AppStateSettings.Instance.FloatingItems.FirstOrDefault(
                            fi => fi.Id == id);
                    if (fe != null)
                        fe.Close();
                });
            }
            catch
            {
            } 
        }

        public void CloseWindowRef(object vm)
        {
            if (!IsActive()) return;

            try
            {
                Execute.OnUIThread(() =>
                {
                    var fe =
                        AppStateSettings.Instance.FloatingItems.FirstOrDefault(
                            fi => fi.ModelInstance == vm);
                    if (fe != null)
                        fe.Close();
                });
            }
            catch
            {
            }
        }

        /// <summary>
        /// Call a method on a viewmodel using reflection
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="method"></param>
        public void CallMethod(object vm, string method)
        {
            CallMethod(vm,method,new object[] {});
        }

        /// <summary>
        /// Call a method on a viewmodel using reflection
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="method"></param>
        /// <param name="args">Arguments of the method</param>
        public void CallMethod(object vm, string method, object[] args)
        {
            if (!IsActive()) return;

            try
            {
                Execute.OnUIThread(() =>
                {
                    // Try to call the method
                    var vmType = vm.GetType();
                    var vmMethod = vmType.GetMethod(method);
                    vmMethod.Invoke(vm, args);
                });
            }
            catch
            {
            }
        }

        /// <summary>
        /// Closes the window
        /// </summary>
        /// <param name="title"></param>
        public void CloseWindow(string title)
        {
            if (!IsActive()) return;

            try
            {
                Execute.OnUIThread(() =>
                {
                    var fe =
                        AppStateSettings.Instance.FloatingItems.FirstOrDefault(
                            fi => fi.Title == title);
                    if (fe != null)
                        fe.Close();
                });
            }
            catch
            {
            }
        }

        public void MoveWindowRef(object vm, double posX, double posY)
        {
            if (!IsActive()) return;

            try
            {
                Execute.OnUIThread(() =>
                {
                    var fe =
                        AppStateSettings.Instance.FloatingItems.FirstOrDefault(
                            fi => fi.ModelInstance == vm);
                    if (fe != null)
                    {
                        fe.ScatterViewItem.Center = new Point(posX, posY);
                    }
                });
            }
            catch
            {
            }
        }


        public void AnimateWindowId(string id, double posX, double posY, int duration = 2000)
        {
            if (!IsActive()) return;

            try
            {
                Execute.OnUIThread(() =>
                {
                    var fe =
                        AppStateSettings.Instance.FloatingItems.FirstOrDefault(
                            fi => fi.Id == id);
                    if (fe != null)
                    {
                        AnimateWindow(fe, posX, posY, duration);
                    }
                });
            }
            catch
            {
            }
        }

        public void AnimateWindowRef(object vm, double posX, double posY, int duration=2000)
        {
            if (!IsActive()) return;

            try
            {
                Execute.OnUIThread(() =>
                {
                    var fe =
                        AppStateSettings.Instance.FloatingItems.FirstOrDefault(
                            fi => fi.ModelInstance == vm);
                    if (fe != null)
                    {
                        AnimateWindow(fe,posX, posY, duration);
                    }
                });
            }
            catch
            {
            }
        }

        private static void AnimateWindow(FloatingElement fe,double posX, double posY, long duration=2000)
        {
// Animate to

            var pa = new PointAnimation();
            pa.From = fe.ScatterViewItem.Center;
            pa.To = new Point(posX, posY);
            pa.Duration = new Duration(TimeSpan.FromMilliseconds(duration));
            pa.EasingFunction = new ExponentialEase() {EasingMode = EasingMode.EaseIn, Exponent = -3};
            fe.ScatterViewItem.BeginAnimation(ScatterContentControlBase.CenterProperty, pa);
            
            //fe.ScatterViewItem.BeginAnimation(ScatterContentControlBase.CenterProperty, null);
            //fe.ScatterViewItem.
            //fe.ScatterViewItem.Center = new Point(posX, posY);
        }


        public void AnimateOpacityWindowId(string id, double posX, double posY, int duration = 2000)
        {
            if (!IsActive()) return;

            try
            {
                Execute.OnUIThread(() =>
                {
                    var fe =
                        AppStateSettings.Instance.FloatingItems.FirstOrDefault(
                            fi => fi.Id == id);
                    if (fe != null)
                    {
                        AnimateOpacityWindow(fe, posX, posY, duration);
                    }
                });
            }
            catch
            {
            }
        }


        private static void AnimateOpacityWindow(FloatingElement fe, double posX, double posY, long duration = 2000)
        {

            // Animate opacity 
            if (fe != null && fe.ScatterViewItem != null)
            {
                var o = new System.Windows.Media.Animation.DoubleAnimation(posX, posY,
                                                                           TimeSpan.FromMilliseconds(duration));
                fe.ScatterViewItem.BeginAnimation(ScatterContentControlBase.OpacityProperty, o);
            }
            //pa.EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseIn, Exponent = -3 };
        }

    }
}
