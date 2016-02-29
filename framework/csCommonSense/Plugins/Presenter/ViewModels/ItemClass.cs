using System;
using System.Collections.Generic;
using System.IO;
using Caliburn.Micro;
using csPresenterPlugin.Controls;
using csShared;
using csShared.Documents;

namespace nl.tno.cs.presenter
{
    public class ItemClass : PropertyChangedBase
    {
        public string Path { get; set; }
        public ItemType Type { get; set; }
        public string Name { get; set; }
        public string ImagePath { get; set; }

        public string Title
        {
            get { return Name.CleanName(); }
        }

        private bool seeThru = true;

        public bool SeeThru
        {
            get { return seeThru; }
            set { seeThru = value; }
        }

        private bool _presentation;

        public bool Presentation
        {
            get { return _presentation; }
            set { _presentation = value; NotifyOfPropertyChange(() => Presentation); }
        }

        private bool _attractor;

        public bool Attractor
        {
            get { return _attractor; }
            set { _attractor = value; NotifyOfPropertyChange(() => Attractor); }
        }

        private bool _visible = true;

        public bool Visible
        {
            get { return _visible; }
            set { _visible = value; NotifyOfPropertyChange(() => Visible); }
        }

        public override string ToString()
        {
            return Path;
        }

        private MetroExplorer _explorer;

        public MetroExplorer Explorer
        {
            get { return _explorer; }
            set { _explorer = value; }
        }


        private int _row;

        public int Row
        {
            get { return _row; }
            set { _row = value; NotifyOfPropertyChange(() => Row); }
        }

        private int _colSpan;

        public int ColSpan
        {
            get { return _colSpan; }
            set { _colSpan = value; NotifyOfPropertyChange(() => ColSpan); }
        }

        private int _rowSpan;

        public int RowSpan
        {
            get { return _rowSpan; }
            set { _rowSpan = value; NotifyOfPropertyChange(() => RowSpan); }
        }

        private string sharePath;

        public string SharePath
        {
            get
            {
                if (string.IsNullOrEmpty(sharePath))
                {
                    sharePath = this.Path.Replace(this.Explorer.StartDirectory.FullName, "");
                    var WwwString = AppStateSettings.Instance.Config.Get("Presenter.Www", "");
                    if (!string.IsNullOrEmpty(WwwString))
                    { 
                        sharePath = WwwString + System.Web.HttpUtility.UrlDecode(sharePath.Replace(@"\", @"/")).Replace(" ", "%20");                        
                    }
                }
                return sharePath;
            }
            set { sharePath = value; }
        }

        private Dictionary<string,string> config = new Dictionary<string, string>();

        public Dictionary<string,string> Config
        {
            get { return config; }
            set { config = value; }
        }

        public bool ReadConfig()
        {
            if (!File.Exists(Path)) return false;
            try
            {
                config = new Dictionary<string, string>();
                var ll = File.ReadAllLines(Path);
                foreach (var l in ll)
                {
                    var cc = l.Split('=');
                    if (cc.Length == 2) config[cc[0].Trim()] = cc[1].Trim();
                }
                return true;
            }
            catch (Exception)
            {

                return false;
            }
            
        }

        public void SaveConfig()
        {
            var res = "";
            foreach (var c in Config)
            {
                res += c.Key + "=" + c.Value + Environment.NewLine;
            }
            File.WriteAllText(Path,res);
        }
        

        private int col;

        public int Col
        {
            get { return col; }
            set { col = value; NotifyOfPropertyChange(() => Col); }
        }

        public Document GetDocument()
        {
            var d = new Document { Location = Path, FileType = FileTypes.unknown, IconUrl = ImagePath,OriginalUrl = Path,ShareUrl = SharePath};
            // TODO EV To switch
            if (Type == ItemType.image) d.FileType = FileTypes.image;
            if (Type == ItemType.video) d.FileType = FileTypes.video;
            if (Type == ItemType.presentation) { d.FileType = FileTypes.imageFolder;            
            }
            //if (Type == ItemType.web) d.FileType = FileTypes.web;
            if (Type == ItemType.website) { d.FileType = FileTypes.html;}
            return d;
        }
    }
}