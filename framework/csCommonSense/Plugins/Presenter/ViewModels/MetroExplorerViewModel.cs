using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using Caliburn.Micro;
using csCommon;


namespace nl.tno.cs.presenter
{
    using System.ComponentModel.Composition;

       

    public interface IMetroExplorer
    {
    }

    [Export(typeof(IMetroExplorer))]
    public class MetroExplorerViewModel : Screen,IMetroExplorer
    {

        public BindableCollection<Folder> Folders { get; set; }

        private string _title;

        public string Title
        {
            get { return _title; }
            set { _title = value; NotifyOfPropertyChange(()=>Title); }
        }

        private string _lastPath;

        public string LastPath
        {
            get { return _lastPath; }
            set { _lastPath = value; }
        }

        private double _segWidth = 800;

        public double SegWidth
        {
            get { return _segWidth; }
            set { _segWidth = value; NotifyOfPropertyChange(()=>SegWidth); }
        }

        private double _segHeight = 600;

        public double SegHeight
        {
            get { return _segHeight; }
            set { _segHeight = value; NotifyOfPropertyChange(()=>SegHeight); }
        }

        private double _scaleWidth = 800;

        public double ScaleWidth
        {
            get { return _scaleWidth; }
            set { _scaleWidth = value; NotifyOfPropertyChange(()=>ScaleWidth); }
        }

        private double _scaleHeight = 600;

        public double ScaleHeight
        {
            get { return _scaleHeight; }
            set { _scaleHeight = value; NotifyOfPropertyChange(()=>ScaleHeight); }
        }

        private bool _titleVisible = true;

        public bool TitleVisible
        {
            get { return _titleVisible; }
            set { _titleVisible = value; NotifyOfPropertyChange(()=>TitleVisible); }
        }


        private bool _itemTitleVisible;

        public bool ItemTitleVisible
        {
            get { return _itemTitleVisible; }
            set { _itemTitleVisible = value; NotifyOfPropertyChange(()=>ItemTitleVisible);}
        }
        
        

        private string _startPath;

        public string StartPath
        {
            get { return _startPath; }
            set { _startPath = value; }
        }
        

        private string _path;

        public string Path
        {
            get { return _path; }
            set { _path = value; NotifyOfPropertyChange(()=>Path); }
        }

        private string _parent;

        public string Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public Dictionary<string, ExtensionDelegate> Extensions = new Dictionary<string, ExtensionDelegate>();
        
        public MetroExplorerViewModel(string path)
        {
            Folders = new BindableCollection<Folder>();
            StartPath = path;
            SelectFolder(StartPath);
            
        }

        public bool CanBack { get { return (Path.Length > StartPath.Length); } }
        
        public void Back()
        {
            if (Path.Length > StartPath.Length)
            {
                //DirectoryInfo di = new DirectoryInfo(Path);
                SelectFolder(LastPath);                
            }
            
        }

        private void GetFolders()
        {
            Folders.Clear();
            DirectoryInfo di = new DirectoryInfo(Path);
            Title = di.Name;
            foreach (var d in di.GetDirectories())
            {
                //if (!d.Name.StartsWith("_")) Folders.Add(new FolderViewModel(d,this));
            }
        }

      



        internal void SelectFolder(string p)
        {
            LastPath = Path;
            Path = p;
            
            GetFolders();
            NotifyOfPropertyChange(() => CanBack);
        }
    }
}
