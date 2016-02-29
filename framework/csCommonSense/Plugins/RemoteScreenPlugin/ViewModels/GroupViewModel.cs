using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Caliburn.Micro;
using csCommon.Utils.Collections;
using csImb;
using csRemoteScreenPlugin;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.FloatingElements;
using csShared.Geo;
using csShared.Geo.Esri;
using csShared.Interfaces;


namespace csCommon.RemoteScreenPlugin
{
    [Export(typeof (IScreen))]
    public class GroupViewModel : Screen
    {

        private csGroup group;

        public csGroup Group
        {
            get { return group; }
            set { group = value; NotifyOfPropertyChange(()=>Group); }
        }

        private csRemoteScreenPlugin.RemoteScreenPlugin plugin;

        public csRemoteScreenPlugin.RemoteScreenPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }

        public bool HasClients
        {
            get { return Group.Clients.Any(); }
        }

        public string Clients { get
        {
            return Group.Clients.Count  + ((Group.Clients.Count() == 1) ? " client" : " clients") +
                ", " + Group.Layers.Count + ((Group.Layers.Count() == 1) ? " layer" : " layers") + 
                ",  owner is " + (Group.OwnerClient != null ? Group.OwnerClient.Name : "Unknown");
        }}
        
        public AppStateSettings AppState { get { return AppStateSettings.Instance; }}

        public bool CanDeleteGroup {
            get { return AppState.Imb.CanDeleteGroup(Group); }
        }

        public void GroupMenu(FrameworkElement element) {
            if (!CanDeleteGroup) return;
            var menu = new MenuPopupViewModel
            {
                RelativeElement   = element,
                RelativePosition  = new Point(25, 0),
                TimeOut           = new TimeSpan(0, 0, 0, 5),
                VerticalAlignment = VerticalAlignment.Top,
                DisplayProperty   = string.Empty
            };
            var miDelete = MenuHelpers.CreateMenuItem("Delete Group", MenuHelpers.DeleteIcon);
            miDelete.Click += (e, f) => AppState.Imb.DeleteGroup(Group);
            menu.Items.Add(miDelete);
            //menu.Items.AddRange(GetMenuItems(layer));

            if (menu.Items.Any()) AppState.Popups.Add(menu);
        }

        protected override void OnViewLoaded(object view)
        {
            //cv = (ContactView) view;
            base.OnViewLoaded(view);
            Group.Clients.CollectionChanged += Clients_CollectionChanged;
            Group.Layers.CollectionChanged += Clients_CollectionChanged;

            TriggerUpdates();
        }

        void Clients_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            TriggerUpdates();
        }

        private void TriggerUpdates()
        {
            NotifyOfPropertyChange(() => HasClients);
            NotifyOfPropertyChange(() => Clients);
            
        }


        public void Join()
        {
            AppState.Imb.JoinGroup(Group);
        }

        public void Leave()
        {
            AppState.Imb.LeaveGroup(Group);            
        }

        public void FollowMap()
        {
            if (Plugin != null)
            {
                //if (Plugin.Following == Group.Id)
                //{
                //    Plugin.Follow2D(0);
                //}
                //else Plugin.Follow2D(Group.Id);
                
                
            }
        }
        

        

    }

    public class GroupActiveBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((bool) value) ? Brushes.Green : Brushes.LightSlateGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}