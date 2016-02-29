using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using DataServer;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Controls.Primitives;
using csShared;
using csShared.Controls.Popups.InputPopup;
using csShared.Controls.Popups.MenuPopup;

namespace csDataServerPlugin {
    public interface IDataServiceSelection {}

    /// <summary>
    ///     Interaction logic for twowaysliding.xaml
    /// </summary>
    [Export(typeof (IDataServiceSelection))]
    public class DataServicesSelectionViewModel : Screen, IDataServiceSelection {
        private const string CreateNewLayerString = "Create new layer:";
        private readonly List<Guid> inProgress = new List<Guid>();
        private DataServicesSelectionView view;

        public DataServerPlugin Plugin { get; set; }

        public ObservableCollection<PoiService> Services {
            get { return Plugin.Dsb.DynamicServices; }
            set { NotifyOfPropertyChange(() => Services); }
        }


        protected override void OnViewLoaded(object myView) {
            base.OnViewLoaded(myView);
            view = (DataServicesSelectionView)myView;
            //Plugin.Dsb.Subscribed += Dsb_Subscribed;
            Plugin.Dsb.UnSubscribed += Dsb_Subscribed;
            //Console.WriteLine(Plugin.Client.DataServices.ToString());

            UpdateServices();
        }

        private void Dsb_Subscribed(object sender, ServiceSubscribeEventArgs e) {
            if (inProgress.Contains(e.Service.Id)) inProgress.Remove(e.Service.Id);
        }

        public void ServiceMenu(ActionExecutionContext context) {
            var sb = context.Source as SurfaceButton;
            if (sb == null) return;
            var a = sb.DataContext as PoiService;
            if (a == null) return;
            var menu = new MenuPopupViewModel {
                RelativeElement = sb,
                RelativePosition = new Point(-35, -5),
                TimeOut = new TimeSpan(0, 0, 0, 5),
                VerticalAlignment = VerticalAlignment.Bottom,
                DisplayProperty = "",
                AutoClose = true
            };
            //menu.Point = _view.CreateLayer.TranslatePoint(new Point(0,0),Application.Current.MainWindow);
            if (a.IsSubscribed) menu.Objects.Add((a.IsLocal && a.Mode == Mode.client) ? "Share online" : "Make local");
            if (a.IsLocal && a.IsSubscribed && a.Settings != null && a.Settings.CanEdit)
            {
                menu.Objects.Add("Rename");
                
            }
            menu.Objects.Add("Delete");
            //menu.Objects.Add("Duplicate");
            menu.Selected += (e, s) =>
            {
                switch (s.Object.ToString()) {
                    case "Make local":
                        a.MakeLocal();
                        break;
                    case "Share online":
                        a.MakeOnline();
                        break;
                    case "Remove":
                        //Plugin.Functions.Remove(a);
                        break;
                    case "Duplicate":
                        //Plugin.Functions.Add(a.Clone());
                        break;
                    case "Delete":
                        var nea = new NotificationEventArgs() { Text = "Are you sure?", Header = "Delete " + a.Name };
                        nea.Duration = new TimeSpan(0, 0, 45);
                        nea.Background = Brushes.Red;
                        nea.Image = new BitmapImage(new Uri(@"pack://application:,,,/csCommon;component/Resources/Icons/Delete.png"));
                        nea.Foreground = Brushes.White;
                        nea.Options = new List<string>() { "Yes", "No" };
                        nea.OptionClicked += (ts, n) =>
                        {
                            if (n.Option == "Yes")
                            {
                                if (a.IsSubscribed && a.Mode == Mode.server) a.MakeLocal();
                        Plugin.Dsb.DeleteService(a);
                               

                            }
                        };
                        AppStateSettings.Instance.TriggerNotification(nea);

                        
                   
                   
                        
                        break;
                    case "Rename":
                        var input = new InputPopupViewModel {
                            RelativeElement = sb,
                            RelativePosition = new Point(-20, -15),
                            TimeOut = new TimeSpan(0, 0, 2, 5),
                            VerticalAlignment = VerticalAlignment.Bottom,
                            Title = "Function Name",
                            Width = 250.0,
                            DefaultValue = a.Name
                        };
                        input.Saved += (st, ea) => {
                            var oldName = a.FileName;
                            var old = a.Name;
                            a.Name = ea.Result;
                            if (oldName == a.FileName) return;
                            if (File.Exists(oldName) && (!File.Exists(a.FileName))) {
                                if (a.SaveXml())
                                {
                                    File.Delete(oldName);
                                    
                                    AppStateSettings.Instance.RenameStartPanelTabItem(old, a.Name);
                                }
                                else
                                    a.Name = old;

                                
                            }
                            else
                                a.Name = old;
                        };
                        AppStateSettings.Instance.Popups.Add(input);
                        break;
                }
            };

            AppStateSettings.Instance.Popups.Add(menu);
        }

        public void CreateLayer(object sender, RoutedEventArgs e) {
            var sb = sender as SurfaceToggleButton;
            if (sb == null) return;

            var menu = new MenuPopupViewModel
            {
                RelativeElement = sb,
                RelativePosition = new Point(35, -5),
                TimeOut = new TimeSpan(0, 0, 0, 5),
                VerticalAlignment = VerticalAlignment.Bottom,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            menu.Objects.Add(CreateNewLayerString);
            foreach (var service in Plugin.Dsb.Templates)
                menu.Objects.Add(string.Format("* {0}", service.Name));
            menu.Selected += (o, args) => CreateNewService(args);
            AppStateSettings.Instance.Popups.Add(menu);
        }

        // TODO EV When the service has been created, I can add pois to it. On closing the app, it saves the pois too. But when restarting, it doesn't show them anymore, nor can I add new pois.
        // TODO EV Every time I create a new service, it shows in the popup. Actually, I only want to show actual templates, which I shouldn't be able to remove.

        /// <summary>
        /// Create a new service based on an existing one..
        /// </summary>
        /// <param name="args"></param>
        private async void CreateNewService(MenuSelectedEventArgs args) {
            var selectedServiceName = args.Object.ToString().Trim(new[] {' ', '*'});
            if (string.Equals(CreateNewLayerString, selectedServiceName)) return;
            var selectedService = Plugin.Dsb.Templates.FirstOrDefault(s => Equals(s.Name, selectedServiceName));
            if (selectedService == null) return;
            var i = 1;
            var newServiceName = selectedServiceName + i;
            while (Plugin.Dsb.Services.Any(s => Equals(s.Name, newServiceName))) newServiceName = selectedServiceName + ++i;

            var folder = Path.GetDirectoryName(selectedService.FileName);
            // Make sure you save it, otherwise you cannot subscribe to it.
            var clonedFile = await PoiService.GetCleanClone(folder, selectedServiceName + ".dsd", folder, newServiceName);
            if (string.IsNullOrEmpty(clonedFile)) return;
            var clonedDataService = AppStateSettings.Instance.DataServer.AddLocalDataService(folder, Mode.client, clonedFile,autoStart:true);
            
            //clonedDataService.IsFileBased = true;
            //clonedDataService.IsLocal = true;
            //clonedDataService.PoITypes.IsRessetable = true;
            //var settings = clonedDataService.AllContent.FirstOrDefault(c => c.Count > 0 && c[0] is ServiceSettings);
            //if (settings != null) settings.IsRessetable = true;
            //Plugin.Dsb.AddService(clonedDataService, Mode.client);
            //Plugin.Dsb.Subscribe(clonedDataService);
        }

        public void Subscribe(PoiService s) {
            // check if we are trying to subscribe all ready
            ThreadPool.QueueUserWorkItem(delegate 
                                             {
                                                // if (inProgress.Contains(s.Id)) return;
                                                 s.IsLoading = true;
                                                 inProgress.Add(s.Id);
                                                 Plugin.Dsb.Subscribe(s);
                                                 s.IsLoading = false;
                                                 //AppStateSettings.Instance.TriggerScriptCommand(this,"");
                                             });
            
        }

        public void UnSubscribe(Service s) {
            Plugin.Dsb.UnSubscribe(s);
        }

        private void UpdateServices() {
            //if (Services != null && Plugin!=null)
            //{
            //    Services.AddRange(Plugin.AvailableDataServices.Where(k=>k.CanCreate));
            //    Services.AddRange(Plugin.Client.DataServices.Where(k => k.CanCreate));
            //}
        }
    }
}