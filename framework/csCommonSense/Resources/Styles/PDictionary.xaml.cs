using csCommon.csMapCustomControls.CircularMenu;
using csShared;
using DataServer;
using ESRI.ArcGIS.Client;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace csDataServerPlugin.Resources
{
    public partial class PDictionary
    {
        private CircularMenuItem menu;

        public PDictionary()
        {
            InitializeComponent();
        }

        private static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; }
        }

        private void FillSymbolDown(object sender, InputDevice device, Point pos)
        {
            var db = ((FrameworkElement)sender).DataContext as DataBinding;
            if (db == null) return;
            var pl = db.Attributes["Layer"] as dsLayer;
            if (pl == null) return;
            var p = db.Attributes["PoI"] as PoI;
            UpdateCircularMenu(p, db, pl, pos);
        }

        private void FillSymbolTouchDown(object sender, TouchEventArgs e)
        {
            FillSymbolDown(sender, e.Device, e.GetTouchPoint(Application.Current.MainWindow).Position);
        }

        private void FillSymbolMouseDown(object sender, MouseButtonEventArgs e)
        {
            FillSymbolDown(sender, e.Device, e.GetPosition(Application.Current.MainWindow));
        }

        private void UpdateCircularMenu(PoI p, DataBinding db, dsLayer pl, Point pos)
        {
            if (!p.NEffectiveStyle.CanEdit.ValueOrFalse()) return;
            pos = new Point(pos.X + 50, pos.Y - 50);
            if (!p.IsSelected)
            {
                if (menu != null) AppState.RemoveCircularMenu("PoiMenu" + p.Id);
                return;
            }
            var lineWidth = new CircularMenuItem
            {
                Title = "Line\nWidth",
                Position = 5,
                Items = new List<CircularMenuItem> {
                    new CircularMenuItem {
                        Title = "1",
                        Position = 1
                    },
                    new CircularMenuItem {
                        Title = "2",
                        Position = 2
                    },
                    new CircularMenuItem {
                        Title = "3",
                        Position = 3
                    },
                    new CircularMenuItem {
                        Title = "5",
                        Position = 4
                    },
                    new CircularMenuItem {
                        Title = "10",
                        Position = 5
                    }
                }
            };

            lineWidth.ItemSelected += (e, f) =>
            {
                p.StrokeWidth = Convert.ToDouble(f.SelectedItem.Title);
                lineWidth.Menu.Back();
                //pl.RedrawPoi(p, db.Attributes["Graphic"] as Graphic);
            };

            //Color c = Colors.Blue;
            var fill = ColorCircularMenuItem.CreateColorMenu("Fill", 6);
            fill.ColorChanged += (e, f) =>
            {
                //SolidColorBrush scb = fill.Fill as SolidColorBrush;
                p.FillColor = fill.Color;
                //pl.RedrawPoi(p, db.Attributes["Graphic"] as Graphic);
            };

            var line = ColorCircularMenuItem.CreateColorMenu("Line\nColor", 7);
            line.ColorChanged += (e, f) =>
            {
                p.StrokeColor = line.Color;
                //pl.RedrawPoi(p, db.Attributes["Graphic"] as Graphic);
            };

            var delete = new CircularMenuItem
            {
                Position = 2,
                Title = "Delete",
                Element = "M33.977998,27.684L33.977998,58.102997 41.373998,58.102997 41.373998,27.684z M14.841999,27.684L14.841999,58.102997 22.237998,58.102997 22.237998,27.684z M4.0319996,22.433001L52.183,22.433001 52.183,63.999001 4.0319996,63.999001z M15.974,0L40.195001,0 40.195001,7.7260003 56.167001,7.7260003 56.167001,16.000999 0,16.000999 0,7.7260003 15.974,7.7260003z"
            };
            delete.Selected += (e, f) => AppState.RemoveCircularMenu("PoiMenu" + p.Id);

            var stylemenu = new CircularMenuItem
            {
                Id = "style",
                Title = "Style",
                Position = 1,
                Icon = "pack://application:,,,/csCommon;component/Resources/Icons/paint.png",
                Items = new List<CircularMenuItem> {
                    lineWidth,
                    fill,
                    line
                }
            };

            menu = new CircularMenuItem
            {
                Id = "PoiMenu" + p.Id,
                Title = "test",
                StartPosition = pos,
                Icon = "pack://application:,,,/csCommon;component/Resources/Icons/paint.png",
                Items = new List<CircularMenuItem>
                {
                    stylemenu,
                    delete
                },
                AutoCloseTimeout = 10
            };
            AppState.AddCircularMenu(menu);

            //AppState.SetCircularMenu(menu);
        }
    }
}