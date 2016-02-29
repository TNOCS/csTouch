using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using csPresenterPlugin.Controls;

namespace nl.tno.cs.presenter
{
    public class Folder: PropertyChangedBase
    {
        
        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyOfPropertyChange(()=>Name); NotifyOfPropertyChange(()=>Title); }
        }

        private bool _seeThru;

        public bool SeeThru
        {
            get { return _seeThru; }
            set { _seeThru = value; }
        }
        

        public string Title
        {
            get { return Name.CleanName(); }
        }

        private MetroExplorer _explorer;

        public MetroExplorer Explorer
        {
            get { return _explorer; }
            set { _explorer = value; }
        }

        private Brush textBrush;

        public Brush TextBrush
        {
            get { return textBrush; }
            set { textBrush = value; NotifyOfPropertyChange(()=>TextBrush); }
        }
        

        private BindableCollection<MetroItemViewModel> _items;

        public BindableCollection<MetroItemViewModel> Items
        {
            get { return _items; }
            set { _items = value; NotifyOfPropertyChange(()=>Items); }
        }

        public List<FolderTemplate> Templates { get; set; }

        private DirectoryInfo _directory;

        public DirectoryInfo Directory
        {
            get { return _directory; }
            set { _directory = value; NotifyOfPropertyChange(()=>Directory); }
        }

        private Visibility _titleVisibility;

        public Visibility TitleVibility
        {
            get { return _titleVisibility; }
            set { _titleVisibility = value; NotifyOfPropertyChange(()=>TitleVibility); }
        }

        private bool _templated = true;

        public bool Templated
        {
            get { return _templated; }
            set { _templated = value; NotifyOfPropertyChange(()=>Templated); }
        }
        

        public Folder(ItemClass item, MetroExplorer explorer)
        {

            Directory = new DirectoryInfo(item.Path);

            Name = Directory.Name;

            InitTemplates();
            Explorer = explorer;

            
        }

        public void InitTemplates()
        {
            Templates = new List<FolderTemplate>()
                            {
                                new FolderTemplate()
                                    {
                                        Items = new List<ItemTemplate>()
                                                    {
                                                        new ItemTemplate() { Col = 0, Row = 0, Colspan = 12,  Rowspan= 12}                                      
                                                    },

                                    },
                                new FolderTemplate()
                                    {
                                        Items = new List<ItemTemplate>()
                                                    {
                                                        new ItemTemplate() { Col = 0, Row = 0, Colspan = 12,  Rowspan= 6},
                                                        new ItemTemplate() { Col = 0, Row = 6, Colspan = 12,  Rowspan= 6},                                      
                                                    },

                                    },
                                new FolderTemplate()
                                    {
                                        Items = new List<ItemTemplate>()
                                                    {
                                                        new ItemTemplate() { Col = 0, Row = 0, Colspan = 4,  Rowspan= 12},
                                                        new ItemTemplate() { Col = 4, Row = 0, Colspan = 4,  Rowspan= 12},
                                                        new ItemTemplate() { Col = 8, Row = 0, Colspan = 4,  Rowspan= 12},
                                                    },

                                    },
                                new FolderTemplate()
                                    {
                                        Items = new List<ItemTemplate>()
                                                    {
                                                        new ItemTemplate() { Col = 0, Row = 0, Colspan = 6,  Rowspan= 6},
                                                        new ItemTemplate() { Col = 6, Row = 0, Colspan = 6, Rowspan = 6},
                                                        new ItemTemplate() { Col = 0, Row = 6, Colspan = 6, Rowspan = 6},
                                                        new ItemTemplate() { Col = 6, Row = 6, Colspan = 6, Rowspan = 6},                                                        
                                                    },

                                    },
                                new FolderTemplate()
                                    {
                                        Items = new List<ItemTemplate>()
                                                    {
                                                        new ItemTemplate() { Col = 0, Row = 0, Colspan = 8,  Rowspan= 8},
                                                        new ItemTemplate() { Col = 8, Row = 0, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate() { Col = 8, Row = 4, Colspan = 4, Rowspan = 8},
                                                        new ItemTemplate() { Col = 0, Row = 8, Colspan = 4, Rowspan = 4},
                                                        new ItemTemplate() { Col = 4, Row = 8, Colspan = 4, Rowspan = 4}
                                                    },

                                    }
                                    
                            };
        }

        

        //protected override void OnViewLoaded(object view)
        //{
        //    base.OnViewLoaded(view);
        //    _view = (FolderView) view;

        //    _view.SetBinding(FrameworkElement.WidthProperty, new Binding("SegWidth") {Source = Explorer});
        //    _view.SetBinding(FrameworkElement.HeightProperty, new Binding("SegHeight") { Source = Explorer });

        //    _view.gScaleGrid.SetBinding(Grid.WidthProperty, new Binding("ScaleWidth") { Source = Explorer });
        //    _view.gScaleGrid.SetBinding(Grid.HeightProperty, new Binding("ScaleHeight") { Source = Explorer });

        //    GetFiles();

        //}


        

        public void GetFiles()
        {
            var l = new List<ItemClass>();
            Items = new BindableCollection<MetroItemViewModel>();
            
            if (Directory.Exists)
            {
                List<DirectoryInfo> di = Directory.GetDirectories().ToList().OrderBy(k => k.Name).ToList();                
                List<FileInfo> fi = Directory.GetFiles().ToList().OrderBy(k => k.Name).ToList();

                foreach (var d in di)
                {
                    if (!d.Name.StartsWith("_"))
                    {
                        var ic = new ItemClass() {Path = d.FullName, ImagePath = d.FullName, Type = ItemType.folder};
                        l.Add(ic);
                    }
                }

                

                foreach (var d in fi)
                {
                    if (!d.Name.StartsWith("_"))
                    {
                        string[] dd = d.Name.ToLower().Split('.');
                        if (dd[0] != Directory.Name.ToLower())
                        {
                            if (d.Extension.ToLower() == ".pdf")
                            {
                                
                            }
                            if (Explorer.images.Contains(d.Extension.ToLower()))
                            {
                                var ic = new ItemClass() {Path = d.FullName, Type = ItemType.image, Name = dd[0]};
                                l.Add(ic);
                            }
                            if (Explorer.videos.Contains(d.Extension.ToLower()))
                            {
                                var ic = new ItemClass()
                                             {
                                                 Path = d.FullName,
                                                 ImagePath = d.FullName,
                                                 Type = ItemType.video,
                                                 Name = dd[0]
                                             };
                                l.Add(ic);
                            }
                            if (Explorer.scripts.Contains(d.Extension.ToLower()))
                            {
                                var ic = new ItemClass()
                                             {
                                                 Path = d.FullName,
                                                 Type = ItemType.script,
                                                 Name = dd[0]
                                             };
                            }
                        }
                        if (Explorer.Extensions.ContainsKey(d.Extension.ToLower()))
                        {
                            var ic = new ItemClass()
                                         {Path = d.FullName, ImagePath = "", Type = ItemType.unknown, Name = dd[0]};
                            l.Add(ic);
                        }
                    }
                }

                List<ItemClass> tbr = new List<ItemClass>();
                foreach (var ic in l.Where(k => k.Type != ItemType.image))
                {
                    if (ic != null && ic.Name!=null)
                    {
                        var i = l.FirstOrDefault(k => k.Name!=null && k.Name.ToLower() == ic.Name.ToLower() && k.Type == ItemType.image);
                        if (i != null)
                        {
                            tbr.Add(i);
                            ic.ImagePath = i.Path;
                        }
                    }
                }
                foreach (var i in tbr) l.Remove(i);

                //var view = Caliburn.Micro.ViewModelLocator.(new MetroItemViewModel(fi[0], 0, 0, 2, 2));

                if (Templated)
                {
                    FolderTemplate ft = Templates.FirstOrDefault(k => k.Items.Count == l.Count);

                    int c = 0;
                    if (ft != null)
                    {
                        foreach (var i in ft.Items)
                        {
                            Items.Add(new MetroItemViewModel(l[c], i.Col, i.Row, i.Colspan, i.Rowspan, Explorer));
                            c += 1;
                        }
                    }
                }



            }
        }
        
    }
}