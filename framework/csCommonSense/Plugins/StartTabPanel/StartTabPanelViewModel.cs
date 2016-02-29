#region

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Caliburn.Micro;
using csShared;
using csShared.Controls.SlideTab;
using csShared.Interfaces;
using csShared.TabItems;
using csShared.Utils;

#endregion

namespace csCommon.MapPlugins.StartTabPanel
{
    [Export(typeof (IPluginScreen))]
    public class StartTabPanelViewModel : Screen, IPluginScreen
    {
        private readonly ITimeline timeline;
        private Brush              accentBrush = Brushes.Red;
        private Thickness          bottomStartPanelMargin;
        private Thickness          bottomStartPanelMarginRight;
        private double             bottomStartPanelMaxHeight;
        private double             bottomStartPanelMinHeight;
        private CornerRadius       cornerRadius;
        private CornerRadius       cornerRadiusLeft;
        private string             id;
        private StartTabPanelView  spv;
        private Brush              defaultBrush = Brushes.Black;

        [ImportingConstructor]
        public StartTabPanelViewModel(CompositionContainer container)
        {
            timeline = container.GetExportedValueOrDefault<ITimeline>();
            AppState.StartPanelTabItems.CollectionChanged += StartPanelTabItemsCollectionChanged;
  
            AppState.FilteredStartTabItems.CollectionChanged += FilteredStartTabItems_CollectionChanged;
            AppState.ExcludedStartTabItems.CollectionChanged += ExcludedStartTabItems_CollectionChanged;
        }

        public string Id
        {
            get { return id; }
            set
            {
                id = value;
                NotifyOfPropertyChange(() => Id);
            }
        }

        private Brush opacityBrush;

        public Brush OpacityBrush
        {
            get { return opacityBrush; }
            set { opacityBrush = value; NotifyOfPropertyChange(()=>OpacityBrush);}
        }

        private Brush leftOpacityBrush;

        public Brush LeftOpacityBrush
        {
            get { return leftOpacityBrush; }
            set { leftOpacityBrush = value; NotifyOfPropertyChange(()=>LeftOpacityBrush); }
        }

        public Brush AccentBrush
        {
            get { return accentBrush; }
            set { accentBrush = value; NotifyOfPropertyChange(()=>AccentBrush); }
        }

        private string title = "Map Layers";

        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(()=>Title); }
        }
        
        public static AppStateSettings AppState
        {
            get { return AppStateSettings.Instance; } 
        }

        public Brush DefaultBrush
        {
            get { return defaultBrush; }
            set
            {
                defaultBrush = value;
                NotifyOfPropertyChange(() => DefaultBrush);
            }
        }
        
        public ITimelineManager TimelineManager
        {
            get { return AppStateSettings.Instance.TimelineManager; }
        }

        public Thickness BottomStartPanelMargin
        {
            get { return bottomStartPanelMargin; }
            set
            {
                bottomStartPanelMargin = value;
                NotifyOfPropertyChange(() => BottomStartPanelMargin);
            }
        }

        public Thickness BottomStartPanelMarginRight
        {
            get { return bottomStartPanelMarginRight; }
            set
            {
                bottomStartPanelMarginRight = value;
                NotifyOfPropertyChange(() => BottomStartPanelMarginRight);
            }
        }

        public double BottomStartPanelMinHeight
        {
            get { return bottomStartPanelMinHeight; }
            set
            {
                bottomStartPanelMinHeight = value;
                NotifyOfPropertyChange(() => BottomStartPanelMinHeight);
            }
        }

        public double BottomStartPanelMaxHeight
        {
            get { return bottomStartPanelMaxHeight; }
            set
            {
                bottomStartPanelMaxHeight = value;
                NotifyOfPropertyChange(() => BottomStartPanelMaxHeight);
            }
        }

        public CornerRadius CornerRadius
        {
            get { return cornerRadius; }
            set
            {
                cornerRadius = value;
                NotifyOfPropertyChange(() => CornerRadius);
            }
        }

        private Brush blackOpacity;

        public Brush BlackOpacity
        {
            get { return blackOpacity; }
            set { blackOpacity = value; NotifyOfPropertyChange(()=>BlackOpacity); }
        }
        
        public CornerRadius CornerRadiusLeft
        {
            get { return cornerRadiusLeft; }
            set
            {
                cornerRadiusLeft = value;
                NotifyOfPropertyChange(() => CornerRadiusLeft);
            }
        }

        public ITimeline Timeline
        {
            get { return timeline; }
        }

        #region IPluginScreen Members

        public string Name
        {
            get { return ""; }
        }

        #endregion

        private void ExcludedStartTabItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateTabItems();
        }

        private void UpdateTabItems()
        {
            try
            {
                if (spv == null) return;

                if (AppState.FilteredStartTabItems.Count > 0)
                {
                    foreach (SlideTabItem i in spv.tcMenuBottom.Items)
                    {
                        i.Visibility = AppState.FilteredStartTabItems.Contains(i.Item.Name)
                                           ? Visibility.Visible
                                           : Visibility.Collapsed;
                    }
                }
                else
                {
                    foreach (SlideTabItem i in spv.tcMenuBottom.Items) i.Visibility = Visibility.Visible;
                }

                if (AppState.ExcludedStartTabItems.Count <= 0) return;
                foreach (SlideTabItem i in spv.tcMenuBottom.Items)
                {
                    i.Visibility = AppState.ExcludedStartTabItems.Contains(i.Item.Name)
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                }
            }
            catch (Exception e)
            {
                Logger.Log("StartTabPanelViewModel.UpdateTabItems", "Unhandled exception", e.Message, Logger.Level.Error);
            }
        }

        private void FilteredStartTabItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateTabItems();
        }

        protected override void OnViewAttached(object view, object context)
        {
            base.OnViewAttached(view, context);
            spv = (StartTabPanelView) view;

            spv.MenuBottom.SetBinding(UIElement.VisibilityProperty,
                            new Binding("BottomTabMenuVisible")
                                {
                                    Source = AppState,
                                    Converter = new BooleanToVisibilityConverter()
                                });

            spv.MenuLeft.SetBinding(UIElement.VisibilityProperty,
                            new Binding("LeftTabMenuVisible")
                            {
                                Source = AppState,
                                Converter = new BooleanToVisibilityConverter()
                            });

            Init();
            UpdateLayout();
            UpdateTabItems();

            AppState.TimelineManager.VisibilityChanged += (e, s) => UpdateLayout();

            AppState.ScriptCommand += (e, s) =>
            {
                if (s == "HideTab") spv.MenuBottom.Height = spv.MenuBottom.MinHeight;
                if (s == "ExpandLeftTab") spv.MenuLeft.Width = spv.MenuLeft.MaxWidth;
            };
        }

        public void Init()
        {
            spv.tcMenuBottom.Items.Clear();
            foreach (var sp in AppState.StartPanelTabItems) AddSP(sp);
        }

        public void UpdateLayout()
        {
            OpacityBrush =  AppState.OpacityAccentBrush;
            LeftOpacityBrush = new SolidColorBrush(new Color { A = (byte)(AppState.LeftBarOpacity * 255), B = 255, R = 255, G = 255 });
                            
            AccentBrush = AppState.AccentBrush;
            BlackOpacity = new SolidColorBrush(Colors.Black) { Opacity = AppState.BottomBarOpacity };

            AppState.PropertyChanged += (e, s) =>
                {
                    if (s.PropertyName == "BottomBarOpacity")
                    {
                        OpacityBrush = AppState.OpacityAccentBrush;
                        BlackOpacity = new SolidColorBrush(Colors.Black) {Opacity = AppState.BottomBarOpacity};
                    }

                    if (s.PropertyName == "LeftBarOpacity")
                    {
                        LeftOpacityBrush = new SolidColorBrush(new Color {
                                A = (byte) (AppState.LeftBarOpacity*255),
                                B = 255,
                                R = 255,
                                G = 255
                            });
                    }
                };
        
            var margin = AppState.Config.Get("Startpanel.Bottom.Margin", "0 0 0 0");
            var c = new ThicknessConverter();
            var convertFrom = (Thickness) c.ConvertFrom(margin);
            convertFrom.Bottom = !AppState.TimelineManager.Visible ? 0 : AppState.Config.GetDouble("Timeline.Height", convertFrom.Bottom);
            var convertFromRight = (Thickness)c.ConvertFrom(margin);
            convertFromRight.Bottom = !AppState.TimelineManager.Visible ? 0 : AppState.Config.GetDouble("Timeline.Height", convertFrom.Bottom);
            convertFromRight.Top = 100;
            BottomStartPanelMargin = convertFrom;
            BottomStartPanelMarginRight = convertFromRight;
            
            BottomStartPanelMaxHeight = AppState.Config.GetDouble("Startpanel.Bottom.MaxHeight", 300.0);
            BottomStartPanelMinHeight = AppState.Config.GetDouble("Startpanel.Bottom.MinHeight", 30.0) + 10;
            var cr = AppState.Config.GetDouble("Layout.Floating.CornerRadius", 0);
            CornerRadius = new CornerRadius(cr, cr, 0, 0);
            CornerRadiusLeft = new CornerRadius(0, cr, cr, 0);
        }

        private void StartPanelTabItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && spv != null) foreach (StartPanelTabItem n in e.NewItems) AddSP(n);
            if (e.OldItems != null && spv != null)
            {
                var old = new List<SlideTabItem>();
                foreach (StartPanelTabItem o in e.OldItems)
                    old.AddRange(from object t in spv.tcMenuBottom.Items where ((SlideTabItem) t).Item == o select ((SlideTabItem) t));

                foreach (SlideTabItem o in old) spv.tcMenuBottom.Items.Remove(o);


                old = new List<SlideTabItem>();
                foreach (StartPanelTabItem o in e.OldItems)
                    for (var i = 0; i < spv.tcMenuLeft.Items.Count; i++)
                        if (((SlideTabItem)spv.tcMenuLeft.Items[i]).Item == o)
                            old.Add(((SlideTabItem)spv.tcMenuLeft.Items[i]));

                foreach (var o in old) spv.tcMenuLeft.Items.Remove(o);
            }
            UpdateTabItems();
        }

        private SlideTabItem AddSP(StartPanelTabItem n)
        {
            var ti = new SlideTabItem {
                Item = n,
                Image = n.Image,
                SupportImage = n.SupportImage,
                HeaderStyle = n.HeaderStyle,
                Width = Double.NaN,
                Height = Double.NaN
            };
            n.TabItem = ti;
            //ti.MinWidth = 100;
            //ti.Margin = new Thickness(5,0,5,0);

            var b = ViewLocator.LocateForModel(n.ModelInstance, null, null) as FrameworkElement;
            if (b != null)
            {
                b.HorizontalAlignment = HorizontalAlignment.Stretch;
                b.VerticalAlignment = VerticalAlignment.Stretch;
                ViewModelBinder.Bind(n.ModelInstance, b, null);
                ti.Content = b;
            }

            n.PropertyChanged += (e, s) => {
                ti.SupportImage = n.SupportImage;
                ti.ShowSupportImage = (ti.SupportImage == null) ? Visibility.Collapsed : Visibility.Visible;
                if (s.PropertyName == "IsSelected")
                    if (n.IsSelected && n.IsSelected != ti.IsSelected)
                    {
                        ti.IsSelected = true;
                        ti.Focus();
                    }
//                ti.Visibility = n.IsSelected ? Visibility.Visible: Visibility.Collapsed;
            };

            if (n.Position == StartPanelPosition.bottom) 
            {
                ti.Style = spv.FindResource("TabItemStyle1") as Style;
                ti.Container = spv.MenuBottom;
                spv.tcMenuBottom.Items.Add(ti);
            }
            if (n.Position == StartPanelPosition.left)
            {
                ti.Style = spv.FindResource("TabItemStyle2") as Style;
                ti.Container = spv.MenuLeft;
                ti.Orientation = TabOrientation.Vertical;

                spv.tcMenuLeft.Items.Add(ti);
                spv.tcMenuLeft.SelectedItem = ti;
            }
            if (n.Position == StartPanelPosition.right)
            {
                ti.Style = spv.FindResource("TabItemStyleRight") as Style;
                ti.Container = spv.MenuRight;
                ti.Orientation = TabOrientation.VerticalRight;

                spv.tcMenuRight.Items.Add(ti);
                spv.tcMenuRight.SelectedItem = ti;
            }

            return ti;
        }
    }
}