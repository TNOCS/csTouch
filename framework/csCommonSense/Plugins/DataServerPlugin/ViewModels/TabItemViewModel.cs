// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TabItemViewModel.cs" company="">
//   
// </copyright>
// <summary>
//   The TabItem interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace csDataServerPlugin
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;

    using Caliburn.Micro;

    using csCommon.csMapCustomControls.CircularMenu;

    using csShared;
    using csShared.TabItems;
    using csShared.Utils;

    using DataServer;

    using ESRI.ArcGIS.Client;
    using ESRI.ArcGIS.Client.Symbols;

    using Microsoft.Surface.Presentation;
    using Microsoft.Surface.Presentation.Controls;

    #endregion

    /// <summary>
    ///     The TabItem interface.
    /// </summary>
    public interface ITabItem
    {
    }

    /// <summary>
    ///     Displays all avaliable POI types of a services  in a tab. By selecting the type of dragging (for point / image) a
    ///     POI can be created
    ///     on the GIS map. This class supports geographical shapes like points, lines, polygons, circles, etc.
    /// </summary>
    [Export(typeof(ITabItem))]
    public class TabItemViewModel : Screen, ITabItem
    {
        #region Constants

        /// <summary>
        ///     The create poi by clicking on map is enabled.
        /// </summary>
        private const string CreatePoiByClickingOnMapIsEnabled =
            "CommonSenseFramework.CreatePoiByClickingOnMapIsEnabled";

        /// <summary>
        ///     The create poi by dragging icon is enabled.
        /// </summary>
        private const string CreatePoiByDraggingIconIsEnabled = "CommonSenseFramework.CreatePoiByDraggingIconIsEnabled";

        /// <summary>
        ///     The notification cancel.
        /// </summary>
        private const string NotificationCancel = "Cancel";

        /// <summary>
        ///     The notification finish.
        /// </summary>
        private const string NotificationFinish = "Finish";

        #endregion

        #region Fields

        /// <summary>
        ///     The last selected.
        /// </summary>
        public SurfaceListBox lastSelected;

        /// <summary>
        ///     The ignored device list.
        /// </summary>
        private readonly List<InputDevice> ignoredDeviceList = new List<InputDevice>();

        /// <summary>
        ///     The layer.
        /// </summary>
        private dsLayer layer;

        /// <summary>
        ///     The draw.
        /// </summary>
        private Draw mEsriShapeDrawingTool;

        /// <summary>
        ///     The m esri shape poi.
        /// </summary>
        private PoI mEsriShapePoi;

        /// <summary>
        ///     The m shape mode notification.
        /// </summary>
        private NotificationEventArgs mNotification;

        /// <summary>
        ///     The m poi service.
        /// </summary>
        private PoiService mPoiService;

        /// <summary>
        ///     The m poi types collection view.
        /// </summary>
        private ICollectionView mPoiTypesCollectionView;

        /// <summary>
        ///     The selected poi type.
        /// </summary>
        private PoI mSelectedPoiType;

        /// <summary>
        ///     The menu.
        /// </summary>
        private CircularMenuItem menu;

        /// <summary>
        ///     The view.
        /// </summary>
        private TabItemView view;

        #endregion

        #region Enums

        /// <summary>
        ///     The edit mode.
        /// </summary>
        private enum EditMode
        {
            /// <summary>
            ///     The none.
            /// </summary>
            None, 

            /// <summary>
            ///     The icon.
            /// </summary>
            Icon, 

            /// <summary>
            ///     The shape.
            /// </summary>
            Shape
        };

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the app state.
        /// </summary>
        public AppStateSettings AppState
        {
            get
            {
                return AppStateSettings.Instance;
            }
        }

        /// <summary>
        ///     Gets the avaliable po i types in service.
        /// </summary>
        public ReadOnlyObservableCollection<BaseContent> AvaliablePoITypesInService { get; private set; }

        /// <summary>
        ///     Gets the draw.
        /// </summary>
        public Draw Draw
        {
            get
            {
                return this.mEsriShapeDrawingTool;
            }

            private set
            {
                this.mEsriShapeDrawingTool = value;
                this.NotifyOfPropertyChange(() => this.Draw);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether esri shape creator is active.
        /// </summary>
        public bool EsriShapeCreatorIsActive
        {
            get
            {
                return this.Draw.IsEnabled;
            }
        }

        /// <summary>
        ///     Gets or sets the layer.
        /// </summary>
        public dsLayer Layer
        {
            get
            {
                return this.layer;
            }

            set
            {
                this.layer = value;
                this.NotifyOfPropertyChange(() => this.Layer);
            }
        }

        /// <summary>
        ///     Gets or sets the plugin.
        /// </summary>
        public DataServerPlugin Plugin { get; set; }

        /// <summary>
        ///     Gets or sets the selected poi type.
        /// </summary>
        public PoI SelectedPoiType
        {
            get
            {
                return this.mSelectedPoiType;
            }

            set
            {
                if (ReferenceEquals(this.mSelectedPoiType, value))
                {
                    return;
                }

                this.EndEditMode();
                this.mSelectedPoiType = value;
                if (value != null)
                {
                    this.StartEditMode();
                }

                this.NotifyOfPropertyChange(() => this.SelectedPoiType);
            }
        }

        /// <summary>
        ///     Gets or sets the service.
        /// </summary>
        public PoiService Service
        {
            get
            {
                return this.mPoiService;
            }

            set
            {
                if (ReferenceEquals(this.mPoiService, value))
                {
                    return; // Not changed
                }

                this.ChangePoiService(this.mPoiService, value);
                this.NotifyOfPropertyChange(() => this.Service);
            }
        }

        /// <summary>
        ///     Gets the service po i types.
        /// </summary>
        public ICollectionView ServicePoITypes
        {
            get
            {
                return this.mPoiTypesCollectionView;
            }

            private set
            {
                this.mPoiTypesCollectionView = value;
                this.NotifyOfPropertyChange(() => this.ServicePoITypes);
            }
        }

        /// <summary>
        ///     Gets or sets the tab item.
        /// </summary>
        public StartPanelTabItem TabItem { get; set; }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the active edit mode.
        /// </summary>
        private EditMode ActiveEditMode
        {
            get
            {
                if (this.SelectedPoiType == null)
                {
                    return EditMode.None;
                }

                return ((this.SelectedPoiType.DrawingMode == DrawingModes.Image)
                        || (this.SelectedPoiType.DrawingMode == DrawingModes.Point))
                           ? EditMode.Icon
                           : EditMode.Shape;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The poi selection changed.
        /// </summary>
        /// <param name="s">
        /// The s.
        /// </param>
        /// <param name="dc">
        /// The dc.
        /// </param>
        public void PoiSelectionChanged(ActionExecutionContext s, SelectionChangedEventArgs dc)
        {
            var slb = (SurfaceListBox)s.Source;
            if (slb.SelectedItem != null && !Equals(slb, this.lastSelected) && this.lastSelected != null)
            {
                this.lastSelected.SelectedItem = null;
            }

            if (slb.SelectedItems.Count > 1 && dc.AddedItems != null && dc.AddedItems.Count == 1)
            {
                slb.SelectedItem = dc.AddedItems[0];
                return;
            }

            this.lastSelected = slb;
            this.SelectedPoiType = (PoI)slb.SelectedItem;
        }

        /// <summary>
        /// This is the MouseDown event!
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        public void StartDrag(object sender, MouseEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            this.StartDragDrop(sender, e.Device, e.OriginalSource);
            e.Handled = true;
        }

        /// <summary>
        /// The start drag drop.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="device">
        /// The device.
        /// </param>
        /// <param name="src">
        /// The src.
        /// </param>
        public void StartDragDrop(object sender, InputDevice device, object src)
        {
            // if (!layer.Service.Settings.CanEdit) return;
            var p = sender as PoI;

            if (p == null)
            {
                return;
            }

            if (!p.IsVisible)
            {
                return;
            }

            this.SelectedPoiType = p;
            p.Data["layer"] = this.layer; // Needed for drag drop handler
            bool iconDragEnabled = this.AppState.Config.GetBool(CreatePoiByDraggingIconIsEnabled, true);
            if (iconDragEnabled && (this.view != null) && (this.ActiveEditMode == EditMode.Icon))
            {
                var mf = sender;

                // Check whether the input device is in the ignore list.
                if (this.ignoredDeviceList.Contains(device))
                {
                    return;
                }

                InputDeviceHelper.InitializeDeviceState(device);

                // try to start drag-and-drop,
                // verify that the cursor the contact was placed at is a ListBoxItem
                // DependencyObject downSource = sender as DependencyObject;
                // FrameworkElement parrent = mf.Parent as FrameworkElement;
                // var source = GetVisualAncestor<FrameworkElement>(downSource);
                var findSource = src as FrameworkElement;

                // var parrent = GetVisualAncestor<FrameworkElement>(downSource);

                // var cursorVisual = new ucDocument(){Document = new Document(){Location = mf.Location}};
                // var cursorVisual = new ListFeatureView() { CanBeDragged = false };
                var cursorVisual = new ucPoiPreview { PoI = mf as PoI, DataContext = mf, Width = 40, Height = 40 };

                var devices = new List<InputDevice>(new[] { device });

                SurfaceDragDrop.BeginDragDrop(this.view, findSource, cursorVisual, mf, devices, DragDropEffects.Copy);

                // Reset the input device's state.
                InputDeviceHelper.ClearDeviceState(device);
                this.ignoredDeviceList.Remove(device);
            }
        }

        /// <summary>
        /// The start touch drag.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        public void StartTouchDrag(object sender, TouchEventArgs e)
        {
            if (e == null)
            {
                return;
            }

            this.StartDragDrop(sender, e.Device, e.OriginalSource);
            e.Handled = true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// The on view loaded.
        /// </summary>
        /// <param name="myView">
        /// The my view.
        /// </param>
        protected override void OnViewLoaded(object myView)
        {
            base.OnViewLoaded(myView);
            this.view = (TabItemView)myView;
            this.InitializeViewModel();
        }

        /// <summary>
        /// The create notifications.
        /// </summary>
        /// <param name="pHeader">
        /// The p Header.
        /// </param>
        /// <param name="pMessage">
        /// The p Message.
        /// </param>
        private void AddNotification(string pHeader, string pMessage)
        {
            this.HideNotification();
            this.mNotification = new NotificationEventArgs
                                     {
                                         Duration = new TimeSpan(1, 0, 0, 0), 
                                         Header = pHeader, 
                                         Background = this.AppState.AccentBrush, 
                                         Foreground = Brushes.White, 
                                         Text = pMessage, 
                                         Options =
                                             new List<string>
                                                 {
                                                     NotificationFinish, 
                                                     NotificationCancel
                                                 }
                                     };
            this.mNotification.OptionClicked += this.mIconModeNotification_OptionClicked;
            this.AppState.TriggerNotification(this.mNotification);
        }

        /// <summary>
        /// The app state on drop.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="dropEventArgs">
        /// The drop event args.
        /// </param>
        private void AppStateOnDrop(object sender, DropEventArgs dropEventArgs)
        {
            this.EndEditMode(); // Icon dropped on map; handled by csDataServerPlugin
        }

        /// <summary>
        /// The change poi service.
        /// </summary>
        /// <param name="pOldPoiService">
        /// The p old poi service.
        /// </param>
        /// <param name="pNewPoiService">
        /// The p new poi service.
        /// </param>
        private void ChangePoiService(PoiService pOldPoiService, PoiService pNewPoiService)
        {
            this.mPoiService = pNewPoiService;

            // Remove old subscriptions
            if (this.AvaliablePoITypesInService != null)
            {
                this.ServicePoITypes = null;
                this.AvaliablePoITypesInService = null;
                this.SelectedPoiType = null;
            }

            // Create new subscriptions
            if ((pNewPoiService != null) && (pNewPoiService.PoITypes != null))
            {
                this.AvaliablePoITypesInService = new ReadOnlyObservableCollection<BaseContent>(pNewPoiService.PoITypes);
                this.ServicePoITypes = this.CreateCollectionView();
                (this.AvaliablePoITypesInService as INotifyCollectionChanged).CollectionChanged += (sender, e) =>
                    {
                        if (this.mPoiTypesCollectionView != null)
                        {
                            this.mPoiTypesCollectionView.Refresh();
                        }
                    };
            }
        }

        /// <summary>
        ///     The configure and display circular menu.
        /// </summary>
        private void ConfigureAndDisplayCircularMenu()
        {
            this.AppState.AddCircularMenu(this.menu);
        }

        /// <summary>
        ///     The configure main border.
        /// </summary>
        private void ConfigureMainBorder()
        {
            if (this.SelectedPoiType == null)
            {
                this.HideMainBorder();
                return;
            }

            Color borderColor;
            switch (this.SelectedPoiType.DrawingMode)
            {
                case DrawingModes.Image:
                case DrawingModes.Point:
                    borderColor = Colors.Yellow;
                    break;
                case DrawingModes.Freehand:
                case DrawingModes.Polyline:
                case DrawingModes.Line:
                    borderColor = this.SelectedPoiType.StrokeColor;
                    break;
                case DrawingModes.Polygon:
                case DrawingModes.Circle:
                    borderColor = this.SelectedPoiType.FillColor;
                    break;
                default:
                    borderColor = Colors.Red;
                    Debug.Assert(false, "Not implemented");
                    break;
            }

            this.AppState.MainBorder.BorderThickness = new Thickness(4);
            this.AppState.MainBorder.BorderBrush = new SolidColorBrush(borderColor);
        }

        /// <summary>
        ///     The create circular menu for esri shapes.
        /// </summary>
        private void CreateCircularMenuForEsriShapes()
        {
            const string CircularStyleMenuFreehand = "Freehand";
            const string CircularStyleMenuPolygon = "Polygon";
            const string CircularStyleMenuPolyline = "Polyline";
            const string CircularStyleMenuCircle = "Circle";

            var lineWidth = new CircularMenuItem
                                {
                                    Title = "Line\nWidth", 
                                    Position = 1, // 5, 
                                    Items =
                                        new List<CircularMenuItem>
                                            {
                                                new CircularMenuItem
                                                    {
                                                        Title = "1", 
                                                        Position =
                                                            1
                                                    }, 
                                                new CircularMenuItem
                                                    {
                                                        Title = "2", 
                                                        Position =
                                                            2
                                                    }, 
                                                new CircularMenuItem
                                                    {
                                                        Title = "3", 
                                                        Position =
                                                            3
                                                    }, 
                                                new CircularMenuItem
                                                    {
                                                        Title = "5", 
                                                        Position =
                                                            4
                                                    }, 
                                                new CircularMenuItem
                                                    {
                                                        Title =
                                                            "10", 
                                                        Position
                                                            = 5
                                                    }
                                            }
                                };

            lineWidth.ItemSelected += (e, f) =>
                {
                    this.mEsriShapePoi.StrokeWidth = Convert.ToDouble(f.SelectedItem.Title);
                    this.mEsriShapePoi.UpdateEffectiveStyle();
                    this.SyncAndDisplayEsriShapeDrawingTool();
                    lineWidth.Menu.Back();
                };

            /*
            var drawStyle = new CircularMenuItem
                                {
                                    Title = "Style", 
                                    Position = 2, 
                                    Items =
                                        new List<CircularMenuItem>
                                            {
                                                new CircularMenuItem
                                                    {
                                                        Title =
                                                            CircularStyleMenuFreehand, 
                                                        Icon =
                                                            "pack://application:,,,/csCommon;component/Resources/Icons/freehand.png", 
                                                        Position
                                                            = 1
                                                    }, 
                                                new CircularMenuItem
                                                    {
                                                        Title =
                                                            CircularStyleMenuCircle, 
                                                        Icon =
                                                            "pack://application:,,,/csCommon;component/Resources/Icons/circle.png", 
                                                        Position
                                                            = 2
                                                    }, 
                                                new CircularMenuItem
                                                    {
                                                        Title =
                                                            CircularStyleMenuPolygon, 
                                                        Icon =
                                                            "pack://application:,,,/csCommon;component/Resources/Icons/polygon.png", 
                                                        Position
                                                            = 3
                                                    }, 
                                                new CircularMenuItem
                                                    {
                                                        Title =
                                                            CircularStyleMenuPolyline, 
                                                        Icon =
                                                            "pack://application:,,,/csCommon;component/Resources/Icons/polygon.png", 
                                                        Position
                                                            = 4
                                                    }
                                            }
                                };
            drawStyle.ItemSelected += (e, f) =>
                {
                    drawStyle.Menu.Back();

                    switch (f.SelectedItem.Title)
                    {
                        case CircularStyleMenuFreehand:
                            this.mEsriShapePoi.DrawingMode = DrawingModes.Freehand;
                            break;
                        case CircularStyleMenuPolygon:
                            this.mEsriShapePoi.DrawingMode = DrawingModes.Polygon;
                            break;
                        case CircularStyleMenuPolyline:
                            this.mEsriShapePoi.DrawingMode = DrawingModes.Polyline;
                            break;
                        case CircularStyleMenuCircle:
                            this.mEsriShapePoi.DrawingMode = DrawingModes.Circle;
                            break;
                        default:
                            Debug.Assert(false, "Not implemented menu item");
                            break;
                    }

                    this.mEsriShapePoi.UpdateEffectiveStyle();
                    this.SyncAndDisplayEsriShapeDrawingTool();
                };
            */
            // Color c = Colors.Blue;
            var fill = ColorCircularMenuItem.CreateColorMenu("Fill", 6);
            fill.ColorChanged += (e, f) =>
                {
                    // SolidColorBrush scb = fill.Fill as SolidColorBrush;
                    this.mEsriShapePoi.FillColor = fill.Color;
                    this.mEsriShapePoi.UpdateEffectiveStyle();
                    this.SyncAndDisplayEsriShapeDrawingTool();
                };

            var line = ColorCircularMenuItem.CreateColorMenu("Line\nColor", 7);
            line.ColorChanged += (e, f) =>
                {
                    this.mEsriShapePoi.StrokeColor = line.Color;
                    this.mEsriShapePoi.UpdateEffectiveStyle();
                    this.SyncAndDisplayEsriShapeDrawingTool();
                };

            this.menu = new CircularMenuItem
                            {
                                Id = "PoiDrawing" + this.Layer.ID, 
                                Title = "test", 
                                Icon =
                                    "pack://application:,,,/csCommon;component/Resources/Icons/paint.png", 
                                Items =
                                    new List<CircularMenuItem> { lineWidth, fill, line /*, drawStyle */ }
                            };
        }

        /// <summary>
        ///     The create collection view.
        /// </summary>
        /// <returns>
        ///     The <see cref="ICollectionView" />.
        /// </returns>
        private ICollectionView CreateCollectionView()
        {
            if (this.AvaliablePoITypesInService == null)
            {
                return null;
            }

            var collectionView = CollectionViewSource.GetDefaultView(this.AvaliablePoITypesInService);
            bool hasMultipleCategories = this.Service.PoITypes.Select(
                p =>
                    {
                        var poi = p as PoI;
                        return poi == null ? null : poi.Category;
                    }).Distinct().Count() > 1;
            if (hasMultipleCategories)
            {
                collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
            }

            collectionView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));

            // TODO EV Can we do this in a nicer way? I filter out the Default element, since it's only a placeholder for meta info. 
            collectionView.Filter = o =>
                {
                    var poiType = o as BaseContent;
                    return poiType != null && poiType.IsVisibleInMenu;
                };
            return collectionView;
        }

        /// <summary>
        ///     The end edit mode.
        /// </summary>
        private void EndEditMode()
        {
            this.HideMainBorder();
            this.HideCircularMenu();
            this.HideEsriShapeDrawingTool();
            this.HideNotification();
            this.AppState.ViewDef.MapControl.PreviewMouseLeftButtonDown -= this.PointClickedOnGisMap;
            this.AppState.Drop -= this.AppStateOnDrop;
            this.mEsriShapePoi = null;
            this.mSelectedPoiType = null;
        }

        /// <summary>
        /// The esri shape drawing completed.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void EsriShapeDrawingCompleted(object sender, DrawEventArgs e)
        {
            Debug.Assert(this.mEsriShapePoi != null, "Cannot be null");
            if (this.Layer != null)
            {
                this.mEsriShapePoi.Points = new ObservableCollection<Point>(e.Geometry.ToLatLonPoints());
                this.mEsriShapePoi.Position = e.Geometry.ToLatLonCenterPoint();
                foreach (var pl in this.mEsriShapePoi.EffectiveMetaInfo)
                {
                    if (pl.DefaultValue != null && !this.mEsriShapePoi.Labels.ContainsKey(pl.Label))
                    {
                        this.mEsriShapePoi.Labels[pl.Label] = pl.DefaultValue;
                    }
                }

                this.Layer.Service.PoIs.Add(this.mEsriShapePoi);
            }

            // foreach (var poiLabel in poiType.Labels) mEsriShapePoi.Labels[poiLabel.Key] = poiLabel.Value;
            // foreach (var pl in poiType.EffectiveMetaInfo) if (pl.DefaultValue != null && !mEsriShapePoi.Labels.ContainsKey(pl.Label)) mEsriShapePoi.Labels[pl.Label] = pl.DefaultValue;

            // Logger.Stat("Drawing.Completed." + ((Custom) ? "Custom." + e.DrawMode : "Template." + ActiveMode.Name));
            this.SelectedPoiType = null; // This will end edit session!
        }

        /// <summary>
        ///     The hide circular menu.
        /// </summary>
        private void HideCircularMenu()
        {
            this.AppState.RemoveCircularMenu(this.menu.Id);
        }

        /// <summary>
        ///     The hide esri shape drawing tool.
        /// </summary>
        private void HideEsriShapeDrawingTool()
        {
            this.Draw.IsEnabled = false;
        }

        /// <summary>
        ///     The configure main border.
        /// </summary>
        private void HideMainBorder()
        {
            this.AppState.MainBorder.BorderThickness = new Thickness(0);
        }

        /// <summary>
        ///     The hide notification.
        /// </summary>
        private void HideNotification()
        {
            if (this.mNotification != null)
            {
                this.mNotification.OptionClicked -= this.mIconModeNotification_OptionClicked;
                this.AppState.TriggerDeleteNotification(this.mNotification);
                this.mNotification = null;
            }
        }

        /// <summary>
        ///     The initialize view model.
        /// </summary>
        private void InitializeViewModel()
        {
            this.CreateCircularMenuForEsriShapes();

            // var b = Service.PoITypes;
            this.Draw = new Draw(this.AppState.ViewDef.MapControl);
            this.Draw.DrawBegin += this.MyDrawObject_DrawBegin;
            this.Draw.DrawComplete += this.EsriShapeDrawingCompleted;

            if (this.TabItem != null)
            {
                this.TabItem.Selected += (e, s) => { this.AppState.DataServer.ActiveService = this.Service; };
                this.TabItem.Deselected += (e, s) =>
                    {
                        this.AppState.DataServer.ActiveService = null;
                        this.SelectedPoiType = null;
                    };
            }
        }

        /// <summary>
        /// The my draw object_ draw begin.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void MyDrawObject_DrawBegin(object sender, EventArgs e)
        {
            this.HideCircularMenu();
        }

        /// <summary>
        /// The point clicked on gis map.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void PointClickedOnGisMap(object sender, MouseButtonEventArgs e)
        {
            // Convert screen point to lat/lon point
            var pos = e.GetPosition(this.AppState.ViewDef.MapControl);
            var latLonPosition = AppStateSettings.Instance.ViewDef.ViewToWorld(pos.X, pos.Y);
            var poiType = this.SelectedPoiType;

            // csDataServerPlugin will create the PoI:
            AppStateSettings.CreatePoiByPoiType(poiType, new Position(latLonPosition.Y, latLonPosition.X));
            this.SelectedPoiType = null; // Will end edit mode
        }

        /// <summary>
        ///     The start edit mode.
        /// </summary>
        private void StartEditMode()
        {
            if (this.ActiveEditMode == EditMode.None)
            {
                return;
            }

            switch (this.ActiveEditMode)
            {
                case EditMode.Icon:
                    this.mEsriShapePoi = null;

                    // Not used for ICON; PoI is generated DataServer class (drag and drop listener)
                    bool iconClickOnMapEnabled = this.AppState.Config.GetBool(CreatePoiByClickingOnMapIsEnabled, true);
                    bool createPoiByDraggingIconIsEnabled =
                        this.AppState.Config.GetBool(CreatePoiByDraggingIconIsEnabled, true);
                    this.Draw.IsEnabled = false;
                    if (iconClickOnMapEnabled)
                    {
                        this.ConfigureMainBorder();
                        this.AppState.ViewDef.MapControl.PreviewMouseLeftButtonDown += this.PointClickedOnGisMap;
                        this.AppState.Drop += this.AppStateOnDrop;
                        this.AddNotification(
                            "Add PoI", 
                            createPoiByDraggingIconIsEnabled ? "Drag icon on map or click on map." : "Click on map");
                    }
                    else
                    {
                        this.HideMainBorder();
                    }

                    break;
                case EditMode.Shape:
                    this.ConfigureMainBorder();
                    this.mEsriShapePoi = new PoI
                                             {
                                                 Service = this.Service, 
                                                 PoiTypeId = this.SelectedPoiType.ContentId, 
                                                 PoiType = this.SelectedPoiType, 
                                                 Id = Guid.NewGuid()
                                             };
                    this.SyncAndDisplayEsriShapeDrawingTool();
                    this.ConfigureAndDisplayCircularMenu();

                    switch (this.SelectedPoiType.DrawingMode)
                    {
                        case DrawingModes.Circle:
                            this.AddNotification(
                                "Add circle", 
                                "Click center point on map and keep left mouse button down.");
                            break;
                        case DrawingModes.Freehand:
                            this.AddNotification("Add freehand", "Click on map and keep left mouse button down.");
                            break;
                        case DrawingModes.Polygon:
                            this.AddNotification(
                                "Add polgygon", 
                                "Click on map to select the polygon points, double click to end.");
                            break;
                        case DrawingModes.Polyline:
                            this.AddNotification(
                                "Add polyline", 
                                "Click on map to select the polyline points, double click to end.");
                            break;
                        case DrawingModes.Line:
                            this.AddNotification("Add shape", "Drag icon on map or click on map.");
                            break;
                        default:
                            this.AddNotification("Add shape", string.Empty);
                            break;
                    }

                    break;
            }
        }

        /// <summary>
        ///     The sync and display esri shape drawing tool.
        /// </summary>
        private void SyncAndDisplayEsriShapeDrawingTool()
        {
            if (this.ActiveEditMode != EditMode.Shape)
            {
                this.HideEsriShapeDrawingTool();
                return;
            }

            switch (this.mEsriShapePoi.DrawingMode)
            {
                case DrawingModes.Image:
                case DrawingModes.Point:
                    this.Draw.IsEnabled = false;
                    break;
                case DrawingModes.Freehand:
                    this.Draw.DrawMode = DrawMode.Freehand;
                    this.Draw.LineSymbol = new LineSymbol
                                               {
                                                   Width = this.mEsriShapePoi.StrokeWidth, 
                                                   Color =
                                                       new SolidColorBrush(this.SelectedPoiType.StrokeColor)
                                                           {
                                                               Opacity
                                                                   =
                                                                   this
                                                                   .mEsriShapePoi
                                                                   .NEffectiveStyle
                                                                   .StrokeOpacity
                                                                   .Value
                                                           }
                                               };
                    break;
                case DrawingModes.Polygon:
                    this.Draw.DrawMode = DrawMode.Polygon;
                    this.Draw.FillSymbol = new FillSymbol
                                               {
                                                   BorderThickness = this.mEsriShapePoi.StrokeWidth, 
                                                   BorderBrush =
                                                       new SolidColorBrush(this.mEsriShapePoi.StrokeColor)
                                                           {
                                                               Opacity
                                                                   =
                                                                   this
                                                                   .mEsriShapePoi
                                                                   .NEffectiveStyle
                                                                   .StrokeOpacity
                                                                   .Value
                                                           }, 
                                                   Fill =
                                                       new SolidColorBrush(this.mEsriShapePoi.FillColor)
                                                           {
                                                               Opacity
                                                                   =
                                                                   this
                                                                   .mEsriShapePoi
                                                                   .NEffectiveStyle
                                                                   .FillOpacity
                                                                   .Value
                                                           }
                                               };
                    break;
                case DrawingModes.Polyline:
                    this.Draw.DrawMode = DrawMode.Polyline;
                    this.Draw.LineSymbol = new LineSymbol
                                               {
                                                   Color =
                                                       new SolidColorBrush(this.mEsriShapePoi.StrokeColor)
                                                           {
                                                               Opacity
                                                                   =
                                                                   this
                                                                   .mEsriShapePoi
                                                                   .NEffectiveStyle
                                                                   .StrokeOpacity
                                                                   .Value
                                                           }, 
                                                   Width = this.mEsriShapePoi.StrokeWidth
                                               };
                    break;
                case DrawingModes.Line:
                    this.Draw.DrawMode = DrawMode.LineSegment;
                    this.Draw.LineSymbol = new LineSymbol
                    {
                        Color =
                            new SolidColorBrush(this.mEsriShapePoi.StrokeColor)
                            {
                                Opacity
                                    =
                                    this
                                    .mEsriShapePoi
                                    .NEffectiveStyle
                                    .StrokeOpacity
                                    .Value
                            },
                        Width = this.mEsriShapePoi.StrokeWidth
                    };
                    break;
                case DrawingModes.Circle:
                    this.Draw.DrawMode = DrawMode.Circle;
                    this.Draw.FillSymbol = new FillSymbol
                                               {
                                                   BorderThickness = this.mEsriShapePoi.StrokeWidth, 
                                                   BorderBrush =
                                                       new SolidColorBrush(this.mEsriShapePoi.StrokeColor)
                                                           {
                                                               Opacity
                                                                   =
                                                                   this
                                                                   .mEsriShapePoi
                                                                   .NEffectiveStyle
                                                                   .StrokeOpacity
                                                                   .Value
                                                           }, 
                                                   Fill =
                                                       new SolidColorBrush(this.mEsriShapePoi.FillColor)
                                                           {
                                                               Opacity
                                                                   =
                                                                   this
                                                                   .mEsriShapePoi
                                                                   .NEffectiveStyle
                                                                   .FillOpacity
                                                                   .Value
                                                           }
                                               };
                    break;
                default:
                    Debug.Assert(false, "Not implemented");
                    break;
            }

            this.Draw.IsEnabled = true; // Activate
        }

        /// <summary>
        /// The m icon mode notification_ option clicked.
        /// </summary>
        /// <param name="sender">
        /// The sender.
        /// </param>
        /// <param name="e">
        /// The e.
        /// </param>
        private void mIconModeNotification_OptionClicked(object sender, NotificationOptionSelectedEventArgs e)
        {
            if ((e.Option == NotificationFinish) && (this.ActiveEditMode == EditMode.Shape))
            {
                this.Draw.CompleteDraw();
            }

            this.SelectedPoiType = null; // Will end edit session
        }

        #endregion
    }
}