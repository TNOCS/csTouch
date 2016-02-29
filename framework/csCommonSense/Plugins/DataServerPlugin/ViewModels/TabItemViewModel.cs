using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using DataServer;
using ESRI.ArcGIS.Client;
using ESRI.ArcGIS.Client.Geometry;
using ESRI.ArcGIS.Client.Projection;
using ESRI.ArcGIS.Client.Symbols;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using csCommon.csMapCustomControls.CircularMenu;
using csShared;
using csShared.TabItems;
using csShared.Utils;

namespace csDataServerPlugin
{
    public interface ITabItem
    {}


    /// <summary>
    ///     Interaction logic for twowaysliding.xaml
    /// </summary>
    [Export(typeof (ITabItem))]
    public class TabItemViewModel : Screen, ITabItem
    {
        private readonly List<InputDevice> ignoredDeviceList = new List<InputDevice>();
        private ICollectionView defaultView;
        private Draw draw;
        public SurfaceListBox lastSelected;

        private dsLayer layer;
        private CircularMenuItem menu;
        private PoI newPoi;
        private PoI selectedPoiType;
        private PoiService service;
        private TabItemView view;
        public DataServerPlugin Plugin { get; set; }
        public StartPanelTabItem TabItem { get; set; }

        public AppStateSettings AppState {
            get { return AppStateSettings.Instance; }
        }

        public dsLayer Layer {
            get { return layer; }
            set {
                layer = value;
                NotifyOfPropertyChange(() => Layer);
            }
        }

        public Draw Draw {
            get { return draw; }
            set {
                draw = value;
                NotifyOfPropertyChange(() => Draw);
            }
        }

        public PoiService Service {
            get { return service; }
            set {
                service = value;
                //InitPoITypes();
                NotifyOfPropertyChange(() => Service);
            }
        }

        public ICollectionView ServicePoITypes {
            get { return defaultView ?? (defaultView = SetPoIGroupingStyle()); }
        }

        public PoI SelectedPoiType {
            get { return selectedPoiType; }
            set {
                if (selectedPoiType == value) return;
                selectedPoiType = value;
                NotifyOfPropertyChange(() => SelectedPoiType);
                CheckDrawingMode();
            }
        }

        private ICollectionView SetPoIGroupingStyle() {
            if (service == null || service.PoITypes == null) return null;
            var collectionView = CollectionViewSource.GetDefaultView(service.PoITypes);
            // When reopening the collectionView, groups and sorts may already have been added. In that case, 
            // do not add them again.
            if (collectionView.GroupDescriptions.Count > 0) return collectionView;
            if (service.PoITypes.Select(p => {
                var poi = p as PoI;
                return poi == null ? null : poi.Category;
            }).Distinct().Count() > 1)
                collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
            collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
            // TODO EV Can we do this in a nicer way? I filter out the Default element, since it's only a placeholder for meta info. 
            collectionView.Filter = o =>
            {
                var poiType = o as BaseContent;
                return poiType != null && poiType.IsVisibleInMenu;
            };
            return collectionView;
        }

        protected override void OnViewLoaded(object myView) {
            base.OnViewLoaded(myView);
            service.PoITypes.CollectionChanged += PoITypes_CollectionChanged;

            finishDrawingNotification = new NotificationEventArgs()
            {
                Duration = new TimeSpan(1, 0, 0, 0),
                Header = "Drawing",
                Background = AppState.AccentBrush,
                Foreground = Brushes.White,
                Text = "To finish the drawing, click finish or do a double tap",
                Options = new List<string>() {"Finish","Cancel"}
            };
            finishDrawingNotification.OptionClicked += finishDrawingNotification_OptionClicked;
            view = (TabItemView) myView;
            //var b = Service.PoITypes;
            Draw = new Draw(AppState.ViewDef.MapControl);
            Draw.DrawBegin += MyDrawObject_DrawBegin;
            Draw.DrawComplete += MyDrawObjectDrawComplete;

            if (TabItem != null)
            {
                TabItem.Selected += (e, s) =>
                {
                    AppState.DataServer.ActiveService = service;
                };
                TabItem.Deselected += (e, s) =>
                {
                    AppState.DataServer.ActiveService = null;
                    Draw.IsEnabled = false;
                    SelectedPoiType = null;
                    UpdateCircularMenu();
                };
            }
        }

        void finishDrawingNotification_OptionClicked(object sender, NotificationOptionSelectedEventArgs e)
        {
            if (e.Option == "Finish")
            {
                Draw.CompleteDraw();
            }
            else
            {
                Draw.IsEnabled = false;
                SelectedPoiType = null;
                CheckDrawingMode();
            }
        }

        void PoITypes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            defaultView = null;
            NotifyOfPropertyChange(() => ServicePoITypes);
        }

        /// <summary>
        /// Check if new items can be dragged
        /// </summary>
        public void DoMaxItemsCheck()
        {
            
        }

        public void PoiSelectionChanged(ActionExecutionContext s, SelectionChangedEventArgs dc) {
            var slb = (SurfaceListBox) s.Source;
            if (slb.SelectedItem != null && !Equals(slb, lastSelected) && lastSelected != null) {
                lastSelected.SelectedItem = null;
            }
            if (slb.SelectedItems.Count > 1 && dc.AddedItems != null && dc.AddedItems.Count == 1) {
                slb.SelectedItem = dc.AddedItems[0];
                return;
            }
            lastSelected = slb;
            SelectedPoiType = (PoI) slb.SelectedItem;
        }

        private void MyDrawObjectDrawComplete(object sender, DrawEventArgs e) {
            var wm = new WebMercator();
            newPoi = SelectedPoiType.GetInstance();
            newPoi.Points = new ObservableCollection<Point>();            
            foreach (var pl in newPoi.EffectiveMetaInfo) if (pl.DefaultValue != null && !newPoi.Labels.ContainsKey(pl.Label)) newPoi.Labels[pl.Label] = pl.DefaultValue;
            //foreach (var poiLabel in poiType.Labels) newPoi.Labels[poiLabel.Key] = poiLabel.Value;
            //foreach (var pl in poiType.EffectiveMetaInfo) if (pl.DefaultValue != null && !newPoi.Labels.ContainsKey(pl.Label)) newPoi.Labels[pl.Label] = pl.DefaultValue;
            AppState.TriggerDeleteNotification(finishDrawingNotification);
            //Logger.Stat("Drawing.Completed." + ((Custom) ? "Custom." + e.DrawMode : "Template." + ActiveMode.Name));
            switch (e.DrawMode) {
                case DrawMode.Freehand:
                case DrawMode.Polyline:
                    //var polygon = e.Geometry is Polygon
                    //                      ? e.Geometry as Polygon
                    //                      : new Polygon();
                    if (e.Geometry is Polyline) {
                        var source = e.Geometry as Polyline;
                        foreach (var path in source.Paths) {
                            foreach (var po in path) {
                                var r = wm.ToGeographic(po) as MapPoint;
                                if (r == null) continue;
                                newPoi.Points.Add(new Point(r.X, r.Y));
                                newPoi.Position = new Position(r.X, r.Y);
                            }
                        }
                    }
                    Layer.Service.PoIs.Add(newPoi);
                    break;
                case DrawMode.Circle:
                case DrawMode.Polygon:
                    if (e.Geometry is Polygon) {
                        var source = e.Geometry as Polygon;
                        foreach (var path in source.Rings) {
                            foreach (var po in path) {
                                var r = wm.ToGeographic(po) as MapPoint;
                                if (r == null) continue;
                                newPoi.Points.Add(new Point(r.X, r.Y));
                                newPoi.Position = new Position(r.X, r.Y);
                            }
                        }
                    }
                    Layer.Service.PoIs.Add(newPoi);
                    break;
            }
            SelectedPoiType = null;
            CheckDrawingMode();
            UpdateCircularMenu();
            //ActiveLayer.Graphics.Add(g);
        }

        private NotificationEventArgs finishDrawingNotification;

        private void MyDrawObject_DrawBegin(object sender, EventArgs e)
        {
            
            
            AppState.TriggerNotification(finishDrawingNotification);
        }

        public void CheckDrawingMode() {
            Draw.IsEnabled = false;
            if (SelectedPoiType == null) {
                AppState.MainBorder.BorderThickness = new Thickness(0);
                return;
            }

            switch (SelectedPoiType.DrawingMode) {
                case DrawingModes.Freehand:
                    AppState.MainBorder.BorderThickness = new Thickness(4);
                    AppState.MainBorder.BorderBrush = new SolidColorBrush(SelectedPoiType.StrokeColor);
                    Draw.DrawMode = DrawMode.Freehand;
                    Draw.LineSymbol = new LineSymbol {
                        Width = SelectedPoiType.StrokeWidth,
                        Color =
                            new SolidColorBrush(SelectedPoiType.StrokeColor) {
                                Opacity = SelectedPoiType.NEffectiveStyle.StrokeOpacity.Value
                            }
                    };

                    Draw.IsEnabled = true;
                    break;
                case DrawingModes.Polygon:
                    AppState.MainBorder.BorderThickness = new Thickness(4);
                    AppState.MainBorder.BorderBrush = new SolidColorBrush(SelectedPoiType.FillColor);
                    Draw.DrawMode = DrawMode.Polygon;
                    Draw.FillSymbol = new FillSymbol {
                        BorderThickness = SelectedPoiType.StrokeWidth,
                        BorderBrush =
                            new SolidColorBrush(SelectedPoiType.StrokeColor) {
                                Opacity = SelectedPoiType.NEffectiveStyle.StrokeOpacity.Value
                            },
                        Fill =
                            new SolidColorBrush(SelectedPoiType.FillColor) {
                                Opacity = SelectedPoiType.NEffectiveStyle.FillOpacity.Value
                            }
                    };
                    Draw.IsEnabled = true;
                    break;
                case DrawingModes.Polyline:
                    AppState.MainBorder.BorderThickness = new Thickness(4);
                    AppState.MainBorder.BorderBrush = new SolidColorBrush(SelectedPoiType.StrokeColor);
                    Draw.DrawMode = DrawMode.Polyline;
                    Draw.LineSymbol = new LineSymbol {
                        Color = AppState.MainBorder.BorderBrush,
                        Width = SelectedPoiType.StrokeWidth
                    };

                    Draw.IsEnabled = true;
                    break;
                case DrawingModes.Circle:
                    AppState.MainBorder.BorderThickness = new Thickness(4);
                    AppState.MainBorder.BorderBrush = new SolidColorBrush(SelectedPoiType.FillColor);
                    Draw.DrawMode = DrawMode.Circle;
                    Draw.FillSymbol = new FillSymbol {
                        BorderThickness = SelectedPoiType.StrokeWidth,
                        BorderBrush =
                            new SolidColorBrush(SelectedPoiType.StrokeColor) {
                                Opacity = SelectedPoiType.NEffectiveStyle.StrokeOpacity.Value
                            },
                        Fill =
                            new SolidColorBrush(SelectedPoiType.FillColor) {
                                Opacity = SelectedPoiType.NEffectiveStyle.FillOpacity.Value
                            }
                    };
                    Draw.IsEnabled = true;
                    break;
                default:
                    AppState.MainBorder.BorderThickness = new Thickness(0);
                    break;
            }
            UpdateCircularMenu();
            newPoi = new PoI {
                Service = Service,
                PoiTypeId = SelectedPoiType.ContentId,
                PoiType = SelectedPoiType,
                Id = Guid.NewGuid()
            };
        }


        private void UpdateCircularMenu() {
          
            if (!Draw.IsEnabled) {
                if (menu != null) AppState.RemoveCircularMenu(menu.Id);
                return;
            }
            var lineWidth = new CircularMenuItem {
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

            lineWidth.ItemSelected += (e, f) => {
                newPoi.StrokeWidth = Convert.ToDouble(f.SelectedItem.Title);
                CheckDrawingMode();
                lineWidth.Menu.Back();
            };

            var drawStyle = new CircularMenuItem {
                Title = "Style",
                Position = 2,
                Items = new List<CircularMenuItem> {
                    new CircularMenuItem {
                        Title = "Freehand",
                        Icon =
                            "pack://application:,,,/csCommon;component/Resources/Icons/freehand.png",
                        Position = 1
                    },
                    new CircularMenuItem {
                        Title = "Circle",
                        Icon =
                            "pack://application:,,,/csCommon;component/Resources/Icons/circle.png",
                        Position = 2
                    },
                    new CircularMenuItem {
                        Title = "Polygon",
                        Icon =
                            "pack://application:,,,/csCommon;component/Resources/Icons/polygon.png",
                        Position = 3
                    },
                    new CircularMenuItem {
                        Title = "Polyline",
                        Icon =
                            "pack://application:,,,/csCommon;component/Resources/Icons/polygon.png",
                        Position = 4
                    }
                }
            };
            drawStyle.ItemSelected += (e, f) => {
                drawStyle.Menu.Back();
                if (SelectedPoiType != null)
                    switch (f.SelectedItem.Title) {
                        case "Freehand":
                            newPoi.DrawingMode = DrawingModes.Freehand;
                            break;
                        case "Polygon":
                            newPoi.DrawingMode = DrawingModes.Polygon;
                            break;
                        case "Polyline":
                            newPoi.DrawingMode = DrawingModes.Polyline;

                            break;
                        case "Circle":
                            newPoi.DrawingMode = DrawingModes.Circle;
                            break;
                    }
                CheckDrawingMode();
            };

            //Color c = Colors.Blue;
            var fill = ColorCircularMenuItem.CreateColorMenu("Fill", 6);
            fill.ColorChanged += (e, f) => {
                //SolidColorBrush scb = fill.Fill as SolidColorBrush;
                newPoi.FillColor = fill.Color;
                CheckDrawingMode();
            };

            var line = ColorCircularMenuItem.CreateColorMenu("Line\nColor", 7);
            line.ColorChanged += (e, f) => {
                newPoi.StrokeColor = line.Color;
                CheckDrawingMode();
            };

            menu = new CircularMenuItem {
                Id = "PoiDrawing" + Layer.ID,
                Title = "test",
                Icon = "pack://application:,,,/csCommon;component/Resources/Icons/paint.png",
                Items = new List<CircularMenuItem> {
                    lineWidth,
                    fill,
                    line,
                    drawStyle
                }
            };

            AppState.AddCircularMenu(menu);
        }

        public void StartDrag(object sender, MouseEventArgs e) {
            if (e == null) return;
            StartDragDrop(sender, e.Device, e.OriginalSource);
            e.Handled = true;
        }

        public void StartTouchDrag(object sender, TouchEventArgs e) {
            if (e == null) return;
            StartDragDrop(sender, e.Device, e.OriginalSource);
            e.Handled = true;
        }

        public void StartDragDrop(object sender, InputDevice device, object src) {
            //if (!layer.Service.Settings.CanEdit) return;
            var p = sender as PoI;

            if (p == null) return;
            if (!p.IsVisible) return;
            if (p.DrawingMode != DrawingModes.Point && p.DrawingMode != DrawingModes.Image) {
                SelectedPoiType = p;
                return;
            }
            var mf = sender;

            p.Data["layer"] = layer;

            // Check whether the input device is in the ignore list.
            if (ignoredDeviceList.Contains(device)) return;

            InputDeviceHelper.InitializeDeviceState(device);

            // try to start drag-and-drop,
            // verify that the cursor the contact was placed at is a ListBoxItem
            //                DependencyObject downSource = sender as DependencyObject;
            //                FrameworkElement parrent = mf.Parent as FrameworkElement;
            //                var source = GetVisualAncestor<FrameworkElement>(downSource);

            var findSource = src as FrameworkElement;

            //var parrent = GetVisualAncestor<FrameworkElement>(downSource);

            //                var cursorVisual = new ucDocument(){Document = new Document(){Location = mf.Location}};
            //                var cursorVisual = new ListFeatureView() { CanBeDragged = false };
            var cursorVisual = new ucPoiPreview {
                PoI = mf as PoI,
                DataContext = mf,
                Width = 40,
                Height = 40
            };

            var devices = new List<InputDevice>(new[] {device});

            SurfaceDragDrop.BeginDragDrop(view, findSource, cursorVisual, mf, devices, DragDropEffects.Copy);

            // Reset the input device's state.
            InputDeviceHelper.ClearDeviceState(device);
            ignoredDeviceList.Remove(device);
        }
    }
}