using System;
using System.ComponentModel.Composition;
using System.Windows;
using Caliburn.Micro;
using csShared;
using csShared.Geo;

namespace csCommon.MapPlugins.MapTools
{
    public interface IMapToolSelection
    {
    }

    [Export(typeof (IMapToolSelection))]
    public class MapToolsViewModel : Screen, IMapToolSelection
    {
        private MapToolsView mtv;
        
        public MapToolsViewModel()
        {
            // Default caption
            Caption = "Map Tools";
        }

        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        public FloatingElement Element { get; set; }

        public MapViewDef ViewDef
        {
            get { return AppState.ViewDef; }
            
        }

        public string Caption { get; set; }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            mtv = (MapToolsView) view;

            UpdateTools();
            AppState.IsOnlineChanged += (sender, args) => UpdateTools();
        }

        private void UpdateTools()
        {
            mtv.Maps.Items.Clear();

            foreach (var a in AppState.MapToolPlugins)
            {
                try
                {
                    if (!AppState.IsOnline && a.IsOnline) continue;
                    var fe = Activator.CreateInstance(a.Control) as FrameworkElement;
                    if (fe == null) continue;
                    fe.Width = 30;
                    fe.Height = 30;
                    fe.Tag = "First";
                    fe.Margin = new Thickness(30, 30, 0, 0);
                    mtv.Maps.Items.Add(fe);
                }
                catch (Exception err)
                {
                    var bla = err.Message;
                }
            }
        }
    }
}