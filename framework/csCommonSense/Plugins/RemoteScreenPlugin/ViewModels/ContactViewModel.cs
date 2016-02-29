using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Caliburn.Micro;
using csCommon.Utils.Collections;
using csImb;
using csRemoteScreenPlugin;
using csShared;
using csShared.FloatingElements;
using csShared.Geo;
using csShared.Geo.Esri;
using csShared.Interfaces;


namespace csCommon.RemoteScreenPlugin
{
    [Export(typeof (IScreen))]
    public class ContactViewModel : Screen
    {

        private ImbClientStatus client;

        public ImbClientStatus Client
        {
            get { return client; }
            set { client = value; NotifyOfPropertyChange(()=>Client); }
        }

        private csRemoteScreenPlugin.RemoteScreenPlugin plugin;

        public csRemoteScreenPlugin.RemoteScreenPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }

        public bool CanFollow2D
        {
            get { return Client.AllCapabilities.Contains(csImb.csImb.Capability2D) && AppState.Imb.ActiveGroup == null; }
        }

        public bool CanFollow3D
        {
            get { return Client.AllCapabilities.Contains(csImb.csImb.Capability3D); }
        }

        public bool CanFollowScreen
        {
            get { return Client.AllCapabilities.Contains(csImb.csImb.CapabilityRemoteScreen); }
        }
        
        public AppStateSettings AppState { get { return AppStateSettings.Instance; }}
        //private ContactView cv;
        public ContactViewModel()
        {

        }

        protected override void OnViewLoaded(object view)
        {
            //cv = (ContactView) view;
            base.OnViewLoaded(view);
            
        }

       
        public void FollowScreen()
        {
            FollowScreenViewModel vm = new FollowScreenViewModel() { Client = this.Client };

            Size s = new Size(500.0, (500.0 / this.Client.ResolutionX) * this.Client.ResolutionY);
            var fe = FloatingHelpers.CreateFloatingElement("Follow Screen", new Point(300, 300), s, vm);
            vm.Fe = fe;
            fe.CanFullScreen = true;
            fe.CanScale = true;
            fe.MinSize = new Size(300, 200);
            AppState.FloatingItems.AddFloatingElement(fe);
            vm.Start();


        }

        public void FollowMap()
        {
            if (Plugin != null)
            {
                if (Plugin.Following == Client.Id)
                {
                    Plugin.Follow2D(0);
                }
                else Plugin.Follow2D(Client.Id);
                
                
            }
        }
        

        

    }

    public class UserIconConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var l = (ImbClientStatus)value;
            return l.Image;
            // FIXME TODO: Unreachable code
            //return new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/person.png"));

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}