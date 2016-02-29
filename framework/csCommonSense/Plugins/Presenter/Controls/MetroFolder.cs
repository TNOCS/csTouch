using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using nl.tno.cs.presenter;

namespace csPresenterPlugin.Controls
{
    public class MetroFolder : Control
    {
        // Using a DependencyProperty as the backing store for TitleVisibility.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleVisibilityProperty =
            DependencyProperty.Register("TitleVisibility", typeof (Visibility), typeof (MetroFolder),
                                        new UIPropertyMetadata(Visibility.Visible));

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register("Items", typeof (BindableCollection<ItemClass>), typeof (MetroFolder),
                                        new UIPropertyMetadata(null));



        public Brush TextBrush
        {
            get { return (Brush)GetValue(TextBrushProperty); }
            set { SetValue(TextBrushProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TextBrush.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextBrushProperty =
            DependencyProperty.Register("TextBrush", typeof(Brush), typeof(MetroFolder), new PropertyMetadata(Brushes.White));

        

        public static readonly DependencyProperty FolderProperty =
            DependencyProperty.Register("Folder", typeof (Folder), typeof (MetroFolder), new UIPropertyMetadata(null));

        private Grid _gTitle;

        static MetroFolder()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof (MetroFolder),
                                                     new FrameworkPropertyMetadata(typeof (MetroFolder)));
        }

        public Visibility TitleVisibility
        {
            get { return (Visibility) GetValue(TitleVisibilityProperty); }
            set { SetValue(TitleVisibilityProperty, value); }
        }

        public BindableCollection<ItemClass> Items
        {
            get { return (BindableCollection<ItemClass>) GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Items.  This enables animation, styling, binding, etc...

        public List<FolderTemplate> Templates { get; set; }

        public Folder Folder
        {
            get { return (Folder) GetValue(FolderProperty); }
            set { SetValue(FolderProperty, value); }
        }

        public void InitTemplates()
        {
            Templates = new List<FolderTemplate>
                            {
                                new FolderTemplate
                                    {
                                        Items = new List<ItemTemplate>
                                                    {
                                                        new ItemTemplate {Col = 0, Row = 0, Colspan = 12, Rowspan = 12}
                                                    },
                                    },
                                new FolderTemplate
                                    {
                                        Items = new List<ItemTemplate>
                                                    {
                                                        new ItemTemplate {Col = 0, Row = 0, Colspan = 12, Rowspan = 6},
                                                        new ItemTemplate {Col = 0, Row = 6, Colspan = 12, Rowspan = 6},
                                                    },
                                    },
                                new FolderTemplate
                                    {
                                        Items = new List<ItemTemplate>
                                                    {
                                                        new ItemTemplate {Col = 0, Row = 0, Colspan = 4, Rowspan = 12},
                                                        new ItemTemplate {Col = 4, Row = 0, Colspan = 4, Rowspan = 12},
                                                        new ItemTemplate {Col = 8, Row = 0, Colspan = 4, Rowspan = 12},
                                                    },
                                    },
                                new FolderTemplate
                                    {
                                        Items = new List<ItemTemplate>
                                                    {
                                                        new ItemTemplate {Col = 0, Row = 0, Colspan = 6, Rowspan = 6},
                                                        new ItemTemplate {Col = 6, Row = 0, Colspan = 6, Rowspan = 6},
                                                        new ItemTemplate {Col = 0, Row = 6, Colspan = 6, Rowspan = 6},
                                                        new ItemTemplate {Col = 6, Row = 6, Colspan = 6, Rowspan = 6},
                                                    },
                                    },
                                new FolderTemplate
                                    {
                                        Items = new List<ItemTemplate>
                                                    {
                                                        new ItemTemplate {Col = 0, Row = 0, Colspan = 8, Rowspan = 8},
                                                        new ItemTemplate {Col = 8, Row = 0, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 8, Row = 4, Colspan = 4, Rowspan = 8},
                                                        new ItemTemplate {Col = 0, Row = 8, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 4, Row = 8, Colspan = 4, Rowspan = 4}
                                                    },
                                    },
                                new FolderTemplate
                                    {
                                        Items = new List<ItemTemplate>
                                                    {
                                                        new ItemTemplate {Col = 0, Row = 0, Colspan = 4, Rowspan = 6},
                                                        new ItemTemplate {Col = 4, Row = 0, Colspan = 4, Rowspan = 6},
                                                        new ItemTemplate {Col = 8, Row = 0, Colspan = 4, Rowspan = 6},
                                                        new ItemTemplate {Col = 0, Row = 6, Colspan = 4, Rowspan = 6},
                                                        new ItemTemplate {Col = 4, Row = 6, Colspan = 4, Rowspan = 6},
                                                        new ItemTemplate {Col = 8, Row = 6, Colspan = 4, Rowspan = 6},
                                                    },
                                    },
                                new FolderTemplate
                                    {
                                        Items = new List<ItemTemplate>
                                                    {
                                                        new ItemTemplate {Col = 0, Row = 0, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 4, Row = 0, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 8, Row = 0, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 0, Row = 4, Colspan = 12, Rowspan = 4},
                                                        new ItemTemplate {Col = 0, Row = 8, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 4, Row = 8, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 8, Row = 9, Colspan = 4, Rowspan = 4},
                                                    },
                                    },
                                    new FolderTemplate
                                    {
                                        Items = new List<ItemTemplate>
                                                    {
                                                        new ItemTemplate {Col = 0, Row = 0, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 4, Row = 0, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 8, Row = 0, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 0, Row = 4, Colspan = 6, Rowspan = 4},
                                                        new ItemTemplate {Col = 6, Row = 4, Colspan = 6, Rowspan = 4},
                                                        new ItemTemplate {Col = 0, Row = 8, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 4, Row = 8, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 8, Row = 9, Colspan = 4, Rowspan = 4},
                                                    },
                                    },
                                    new FolderTemplate
                                    {
                                        Items = new List<ItemTemplate>
                                                    {
                                                        new ItemTemplate {Col = 0, Row = 0, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 4, Row = 0, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 8, Row = 0, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 0, Row = 4, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 4, Row = 4, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 8, Row = 4, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 0, Row = 8, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 4, Row = 8, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate {Col = 8, Row = 9, Colspan = 4, Rowspan = 4},
                                                    },
                                    }
                            };
        }


        public void GetFiles()
        {
            InitTemplates();

            Items = new BindableCollection<ItemClass>();

            if (Folder == null || Folder.Directory == null || !Folder.Directory.Exists) return;
            var l = Folder.Explorer.GetItems(Folder.Directory.FullName).Where(k => k.Visible && !string.IsNullOrEmpty(k.ImagePath)).ToList();

            if (!Folder.SeeThru) l.Clear();

            //var view = Caliburn.Micro.ViewModelLocator.(new MetroItemViewModel(fi[0], 0, 0, 2, 2));

            if (Folder.Templated)
            {
                // geen inhouden gevonden, kijken of folder ook een bestand heeft
                //if (l.Count == 0)
                //{                 
                //}
                //var it = l.Where(k => k.Type == ItemType.image).ToList();
                var ft = Templates.FirstOrDefault(k => k.Items.Count == l.Count);
                if (l.Count>0 && ft == null) ft = Templates.Last();

                var c = 0;
                if (ft != null)
                {
                    foreach (var i in ft.Items)
                    {
                        l[c].Col = i.Col;
                        l[c].Row = i.Row;
                        l[c].ColSpan = i.Colspan;
                        l[c].RowSpan = i.Rowspan;
                        l[c].Explorer = Folder.Explorer;
                        c += 1;
                    }
                }
                else
                {
                    var i = Folder.Explorer.GetItems(Folder.Directory.Parent.FullName);
                    var f = i.FirstOrDefault(k => k.Path == Folder.Directory.FullName);
                    if (f != null)
                    {
                        f.Col = 0;
                        f.Row = 0;
                        f.ColSpan = 12;
                        f.RowSpan = 12;
                        f.Explorer = Folder.Explorer;
                        l.Add(f);
                    }
                }

                var slb = GetTemplateChild("Items") as ItemsControl;
                if (slb != null) slb.ItemsSource = l;
            }
            else
            {
                foreach (var i in l)
                {
                    i.Explorer = Folder.Explorer;
                }

                var slb = GetTemplateChild("AllItems") as ItemsControl;
                if (slb != null) slb.ItemsSource = l;
            }
        }


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (Folder != null && Folder.Explorer != null)
                SetBinding(ForegroundProperty, new Binding("Foreground") {Source = Folder.Explorer});

            _gTitle = GetTemplateChild("gTitle") as Grid;
            if (Folder != null && (_gTitle != null && Folder.Explorer.CanSelectFolder))
            {
                _gTitle.TouchDown += GTitleTouchDown;
                _gTitle.PreviewMouseDown += GTitlePreviewMouseDown;
            }

            //if (!Folder.Templated) this.Style = Application.Current.FindResource("MetroFolderList") as Style;
            GetFiles();
            if (Folder == null || Folder.Explorer == null) return;
            TitleVisibility = Folder.TitleVibility;
            Folder.Explorer.Refreshed += ExplorerRefreshed;
        }

        private void GTitlePreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Folder.Explorer.SelectFolder(Folder.Directory.FullName);
            e.Handled = true;
        }

        private void GTitleTouchDown(object sender, TouchEventArgs e)
        {
            Folder.Explorer.SelectFolder(Folder.Directory.FullName);
            e.Handled = true;
        }

        private void ExplorerRefreshed(object sender, EventArgs e)
        {
            GetFiles();
        }
    }
}