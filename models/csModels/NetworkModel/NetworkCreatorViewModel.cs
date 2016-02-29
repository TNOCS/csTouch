using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using csShared.Controls.Popups.MenuPopup;
using csShared.Geo;
using DataServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NetworkModel
{
    [DebuggerDisplay("Name = {Title}")]
    public class Network : PropertyChangedBase
    {
        private string title;
        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(() => Title); }
        }
    }

    [Export(typeof(IScreen))]
    public class NetworkCreatorViewModel : Screen, IEditableScreen
    {
        private BindableCollection<Network> networks;
        private Network selectedNetwork;
        private PoI lastPoi;
        private string networkName;
        private bool canEdit;

        public static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public IModel Model { get; set; }

        public Network SelectedNetwork
        {
            get { return selectedNetwork; }
            set { selectedNetwork = value; NotifyOfPropertyChange(() => SelectedNetwork); }
        }

        public BindableCollection<Network> Networks
        {
            get { return networks; }
            set { networks = value; NotifyOfPropertyChange(() => Networks); }
        }

        public string NetworkName
        {
            get { return networkName; }
            set { networkName = value; NotifyOfPropertyChange(() => NetworkName); }
        }

        #region Color selection

        private string selectedColor = "Red";

        public string SelectedColor
        {
            get { return selectedColor; }
            set
            {
                selectedColor = value;
                NotifyOfPropertyChange(() => SelectedColor);
                NotifyOfPropertyChange(() => SelectedColorBrush);
            }
        }

        public SolidColorBrush SelectedColorBrush { get { return new BrushConverter().ConvertFromString(SelectedColor) as SolidColorBrush; } }

        private static MenuPopupViewModel GetMenu(FrameworkElement fe)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement = fe,
                RelativePosition = new Point(10, 30),
                TimeOut = new TimeSpan(0, 0, 0, 15),
                VerticalAlignment = VerticalAlignment.Bottom,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            return menu;
        }

        public void SelectColor(FrameworkElement el)
        {
            var m = GetMenu(el);
            m.AddMenuItems(new[] { "Red", "Yellow", "Black", "Green", "Blue", "Orange", "Purple", "Brown" });
            m.Selected += (s, f) =>
            {
                SelectedColor = f.Object.ToString();
            };
            AppState.Popups.Add(m);
        }

        #endregion Color selection

        public void AddNetwork()
        {
            if (Networks == null) Networks = new BindableCollection<Network>();
            var title = string.IsNullOrEmpty(networkName) ? Model.Id : networkName;
            var network = Networks.FirstOrDefault(n => string.Equals(n.Title, title, StringComparison.InvariantCultureIgnoreCase))
                          ?? new Network {Title = title};
            // Only add the network in case its unique
            if (!Networks.Contains(network)) Networks.Add(network);
            UpdateNetworkNamesLabel();
            SelectedNetwork = network;
            lastPoi = PoI;

            if (Model == null || Model.DataServer == null) return;
            foreach (var service in Model.DataServer.Services.OfType<PoiService>().Where(s => s.IsSubscribed))
            {
                service.Tapped += ServiceOnTapped;
            }
            var notificationEventArgs = new NotificationEventArgs
            {
                Id = Guid.NewGuid(),
                Background = Brushes.White,
                Foreground = Brushes.Black,
                Header = "Add a network",
                Text = "Press OK when done.",
                Duration = TimeSpan.FromDays(1),
                Options = new List<string> { "OK", "Cancel" }
            };

            notificationEventArgs.OptionClicked += (sender, args) =>
            {
                foreach (var service in Model.DataServer.Services.OfType<PoiService>())
                {
                    service.Tapped -= ServiceOnTapped;
                }
                switch (args.Option)
                {
                    case "OK":
                        break;
                    case "Cancel":
                        RemoveNetwork(selectedNetwork);
                        break;
                }
            };
            AppState.TriggerNotification(notificationEventArgs);
        }

        private void UpdateNetworkNamesLabel()
        {
            PoI.Labels[Model.Id + ".Networks"] = string.Join(";", Networks.Select(n => n.Title).ToArray());
        }

        private void ServiceOnTapped(object sender, TappedEventArgs e)
        {
            var poi = e.Content as PoI;
            if (poi == null) return;
            var linkPoiTypeParameter = Model.Model.Parameters.FirstOrDefault(p => string.Equals(p.Name, "LinkPoiType", StringComparison.InvariantCultureIgnoreCase));
            if (linkPoiTypeParameter == null) return;
            var linkPoiType = linkPoiTypeParameter.Value;
            var poiType = Model.Service.PoITypes.OfType<PoI>().FirstOrDefault(pt => string.Equals(pt.PoiId, linkPoiType, StringComparison.InvariantCultureIgnoreCase));
            if (poiType == null) return;

            var position = poi.Position ?? CalculateCenter(poi);
            var link = new PoI
            {
                Id = Guid.NewGuid(),
                PoiTypeId = linkPoiType,
                Layer = PoI.Layer + "_link",
                Labels = new Dictionary<string, string> {
                    { "IsActive", "true" },
                    { "QoS", "100"},
                    {Model.Id + ".CreatorId", PoI.Id.ToString() },
                    {Model.Id + ".NetworkName", selectedNetwork.Title },
                    {Model.Id + ".SourceId", lastPoi.Id.ToString() },
                    {Model.Id + ".SinkId", poi.Id.ToString() },
                },
                Points = new ObservableCollection<Point> {
                    new Point {X = lastPoi.Position.Longitude, Y = lastPoi.Position.Latitude},
                    new Point {X = position.Longitude, Y = position.Latitude}
                }
            };
            var strokeColor = (Color)ColorConverter.ConvertFromString(selectedColor);
            link.Style = new PoIStyle { StrokeColor = strokeColor };
            Model.Service.PoIs.Add(link);
            lastPoi = poi;
        }

        /// <summary>
        /// Calculate the center of a polygon PoI and set its position accordingly.
        /// </summary>
        /// <param name="poi"></param>
        /// <returns></returns>
        private static Position CalculateCenter(BaseContent poi)
        {
            var points = poi.Points;
            if (points == null || points.Count == 0) return new Position(0, 0);
            var x = points.Average(p => p.X);
            var y = points.Average(p => p.Y);
            poi.Position = new Position(x, y);
            return poi.Position;
        }

        public void RemoveNetwork(Network network)
        {
            Networks.Remove(network);
            UpdateNetworkNamesLabel();
            var pois = Model.Service.PoIs;
            var keyCreatorId = Model.Id + ".CreatorId";
            var keyNetworkName = Model.Id + ".NetworkName";
            for (var i = pois.Count - 1; i >= 0; i--)
            {
                var poi = pois[i];
                if (!poi.Labels.ContainsKey(keyCreatorId) || !poi.Labels.ContainsKey(keyNetworkName)) continue;
                if (!string.Equals(poi.Labels[keyCreatorId], PoI.Id.ToString())) continue;
                if (!string.Equals(poi.Labels[keyNetworkName], network.Title, StringComparison.InvariantCultureIgnoreCase)) continue;
                pois.RemoveAt(i);
            }
        }

        public bool CanEdit
        {
            get { return canEdit; }
            set
            {
                if (canEdit == value) return;
                canEdit = value;
                NotifyOfPropertyChange(() => CanEdit);
            }
        }

        public MapCallOutViewModel CallOut { get; set; }

        public PoI PoI { get; set; }
    }
}
