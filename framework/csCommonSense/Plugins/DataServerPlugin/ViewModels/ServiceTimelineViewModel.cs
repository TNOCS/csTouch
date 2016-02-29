using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Caliburn.Micro;
using csShared.Controls.Popups.MenuPopup;
using DataServer;
using Microsoft.Surface.Presentation.Controls;
using csImb;
using csShared;
using Point = System.Windows.Point;

namespace csDataServerPlugin
{

    public class ServiceTimelineViewModel : Screen
    {

        private ObservableCollection<csImb.ImbClientStatus> clients = new ObservableCollection<ImbClientStatus>();

        public ObservableCollection<csImb.ImbClientStatus> Clients
        {
            get { return clients; }
            set { clients = value; NotifyOfPropertyChange(()=>Clients); }
        }

        private BindableCollection<ImbClientStatus> selectedClients = new BindableCollection<ImbClientStatus>();

        public BindableCollection<ImbClientStatus> SelectedClients
        {
            get { return selectedClients; }
            set { selectedClients = value; NotifyOfPropertyChange(()=>SelectedClients); }
        }
        

        private PoiService service;

        public PoiService Service
        {
            get { return service; }
            set { service = value; NotifyOfPropertyChange(()=>Service); }
        }


        public void Zoom(BaseContent p)
        {
            if (p.Position == null) return;
            AppState.ViewDef.ZoomAndPoint(new Point(p.Position.Longitude,p.Position.Latitude));
            
        }

        private MenuPopupViewModel GetMenu(FrameworkElement fe)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement = fe,
                RelativePosition = new Point(35, -5),
                TimeOut = new TimeSpan(0, 0, 0, 15),
                VerticalAlignment = VerticalAlignment.Top,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            return menu;
        }

        public void SetPriority(BaseContent c, FrameworkElement b)
        {
            var m = GetMenu(b);
            for (int i = 1; i < 4; i++)
            {
                
                var mi = m.AddMenuItem(i.ToString());
                mi.FontSize = 34;
                var v = i;
                mi.Click += (s, e) =>
                {
                    c.PoiPriority = v;
                    
                };
            }
           
            AppState.Popups.Add(m);
        }

        public SortedObservableCollection<BaseContent> TimeLine
        { 
            get {
                if (Service != null) return Service.TimeLine;
            return null;
        } } 

        public DataServerBase Dsb { get; set; }

        public AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        private string newMessage;

        public string NewMessage
        {
            get { return newMessage; }
            set { newMessage = value; NotifyOfPropertyChange(()=>NewMessage); }
        }
        
        
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            
        }

        

        public void SendTo(FrameworkElement sender)
        {
            Clients.Clear();
            foreach (var a in AppState.Imb.Clients.Values.Where(k=>k.Client)) Clients.Add(a);            
        }

        public void SelectionChanged(SurfaceListBox s)
        {
            SelectedClients.Clear();
            foreach (ImbClientStatus c in s.SelectedItems) SelectedClients.Add(c);
        }

        public void SendMessage()
        {
            if (string.IsNullOrEmpty(NewMessage)) return;
            Event e = Service.CreateEvent("Message");
            e.Labels["Text"] = NewMessage;
            if (SelectedClients.Count>0)
            {
                e.Labels["To"] = string.Join<string>(",", SelectedClients.Select(k=>k.Name));
            }
            e.UserId = AppState.Imb.Status.Name;
            e.Name = NewMessage;
            Service.Events.Add(e);
            NewMessage = "";
        }

      
    }

    public class PriorityColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((int) value)
            {
                case 1:
                    return Brushes.Red;
                case 2:
                    return Brushes.Orange;
                case 3:
                    return Brushes.Green;
                case 4:
                    return Brushes.CornflowerBlue;
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}