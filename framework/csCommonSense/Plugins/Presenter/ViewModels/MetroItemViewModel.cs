using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using Caliburn.Micro;
using csPresenterPlugin.Controls;


namespace nl.tno.cs.presenter
{
    using System.ComponentModel.Composition;

    public interface IMetroItem
    {
    }

    [Export(typeof(IMetroItem))]
    public class MetroItemViewModel : Screen, IMetroItem
    {
        public MetroExplorer Explorer { get; set; }

        private int _row;

        public int Row
        {
            get { return _row; }
            set { _row = value; NotifyOfPropertyChange(()=>Row); }
        }

        private int _colSpan;

        public int ColSpan
        {
            get { return _colSpan; }
            set { _colSpan = value; NotifyOfPropertyChange(()=>ColSpan); }
        }

        private int _rowSpan;

        public int RowSpan
        {
            get { return _rowSpan; }
            set { _rowSpan = value; NotifyOfPropertyChange(()=>RowSpan); }
        }
        

        private int col;

        public int Col
        {
            get { return col; }
            set { col = value; NotifyOfPropertyChange(()=>Col); }
        }
        

        private ItemClass _item;

        public ItemClass Item
        {
            get { return _item; }
            set { _item = value; NotifyOfPropertyChange(() => Item); }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
           // MetroItemView miv = (MetroItemView) view;

            //miv.ItemTitle.SetBinding(Grid.VisibilityProperty, new Binding("ItemTitleVisible") { Source = Explorer, Converter = new BooleanToVisibilityConverter()});
        
        }

        private string _image;

        public string Image
        {
            get { return _image; }
            set { _image = value; NotifyOfPropertyChange(()=>Image); }
        }
        

        private string _name;

        public string Name
        {
            get { return _name; }
            set { _name = value; NotifyOfPropertyChange(()=>Name); NotifyOfPropertyChange(()=>Title);}
        }

        
        public string Title
        {
            get
            {
                string s = Name;
                while (s.Length > 0 && (Regex.IsMatch(s[0].ToString(), @"\d") || s[0]==' ' || s[0]=='.')) s = s.Remove(0, 1);                
                return s;
            }        
        }
        
        
        public void SelectItem(MetroItemViewModel vm)
        {
            switch (vm.Item.Type)
            {
                case ItemType.folder:
                    var di = new DirectoryInfo(vm.Item.Path);
                    Explorer.SelectFolder(di.FullName);                    
                    break;
                case ItemType.image:
                    MessageBox.Show("Image");
                    break;
                case ItemType.video:
                    MessageBox.Show("Video");
                    break;
                case ItemType.unknown:
                    var fi = new FileInfo(vm.Item.Path);
                    if (Explorer.Extensions.ContainsKey(fi.Extension))
                    {
                        Explorer.Extensions[fi.Extension].Invoke(Item);
                    }
                    break;
            }
        }

        public MetroItemViewModel(ItemClass item, int x, int y, int spanx, int spany, MetroExplorer explorer)
        {
            Explorer = explorer;
            Item = item;
            Col = x;
            Row = y;
            ColSpan = spanx;
            RowSpan = spany;
            Image = Item.ImagePath;
            switch (item.Type)
            {
                case ItemType.folder:
                    var di = new DirectoryInfo(item.Path);
                    Name = di.Name;
                    Image = "http://www.tno.nl/images/contactpers/mihielvandermeulen.jpg";
                    foreach (var f in di.GetFiles())
                    {
                        if (f.Name.ToLower().Contains(di.Name.ToLower() + "."))
                        {
                            Image = f.FullName;
                        }
                    }
                    
                    break;
                case ItemType.image:
                    var fi = new FileInfo(item.Path);
                    Name = fi.Name.Split('.')[0];
                    Image = item.Path;
                    break;
                case ItemType.video:
                    var fv = new FileInfo(item.Path);
                    Name = fv.Name.Split('.')[0];
                    Image = item.ImagePath;
                    break;
                case ItemType.unknown:
                    var fu = new FileInfo(item.Path);
                    Name = fu.Name.Split('.')[0];
                    Image = item.ImagePath;                         
                    break;
            }
            
        }

        
        
    }
}
