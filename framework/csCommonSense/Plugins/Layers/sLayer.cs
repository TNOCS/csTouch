using System;
using System.Linq;
using Caliburn.Micro;
using csDataServerPlugin;
using ESRI.ArcGIS.Client;

namespace csCommon
{
    public class sLayer : PropertyChangedBase
    {
        private bool showOpacity;

        public bool ShowOpacity
        {
            get { return showOpacity; }
            set { showOpacity = value; NotifyOfPropertyChange(()=>ShowOpacity); }
        }

        private Layer layer;

        public Layer Layer
        {
            get { return layer; }
            set
            {
                layer = value; NotifyOfPropertyChange(() => Layer);
                var loadingLayer = layer as ILoadingLayer;
                if (loadingLayer == null) return;
                loadingLayer.StartedLoading -= sLayer_StartedLoading;
                loadingLayer.StartedLoading += sLayer_StartedLoading;
                loadingLayer.StoppedLoading -= sLayer_StoppedLoading;
                loadingLayer.StoppedLoading += sLayer_StoppedLoading;
            }
        }

        void sLayer_StoppedLoading(object sender, System.EventArgs e)
        {
            NotifyOfPropertyChange(()=>IsLoading);
        }

        public sLayer FindRecursive(Func<sLayer, bool> pPredicaat)
        {
            if (pPredicaat(this)) return this;
            foreach(var layer in Children)
            {
                var result = layer.FindRecursive(pPredicaat);
                if (result != null) return result;
            }
            return null;
        }

        void sLayer_StartedLoading(object sender, System.EventArgs e)
        {
            NotifyOfPropertyChange(() => IsLoading);           
        }

        private bool isService;

        public bool IsService
        {
            get { return isService; }
            set { isService = value; NotifyOfPropertyChange(() => IsService); }
        }

        private bool isShared;

        public bool IsShared
        {
            get { return isShared; }
            set { isShared = value; NotifyOfPropertyChange(()=>IsShared); }
        }

        private bool isLoading;

        public bool IsLoading
        {
            get
            {
                if (!(Layer is ILoadingLayer)) return isLoading;
                return (((ILoadingLayer)Layer).IsLoading);
            }
            set
            {
                isLoading = value; NotifyOfPropertyChange(()=>IsLoading);
            }
        }

        public bool Visible
        {
            get
            {
                //if (Layer is GroupLayer)
                //{
                //    return (Children.Any(k => k.Visible));
                //}
                return Layer.Visible;
            }
            set
            {
                Layer.Visible = value; 
                NotifyOfPropertyChange(() => Visible);
            }
        }

        private bool hasMenu;

        public bool HasMenu
        {
            get { return hasMenu; }
            set { hasMenu = value; NotifyOfPropertyChange(()=>HasMenu); }
        }
        

        private bool isOnline;

        public bool IsOnline
        {
            get { return isOnline; }
            set { isOnline = value; NotifyOfPropertyChange(()=>IsOnline); }
        }
        
        public int GetPos()
        {
            if (Parent != null) return Parent.Children.IndexOf(this);
            return -1;
        }

        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(() => Title); }
        }

        private string path;

        public string Path
        {
            get { return path; }
            set { path = value; NotifyOfPropertyChange(() => Path); }
        }

        private bool isTabAvailable;

        public bool IsTabAvailable
        {
            get { return isTabAvailable; }
            set
            {
                isTabAvailable = value;
                NotifyOfPropertyChange(() => IsTabAvailable);
            }
        }

        private bool isConfigAvailable;

        public bool IsConfigAvailable
        {
            get { return isConfigAvailable; }
            set
            {
                isConfigAvailable = value;
                NotifyOfPropertyChange(() => IsConfigAvailable);
            }
        }

        private BindableCollection<sLayer> children = new BindableCollection<sLayer>();

        public BindableCollection<sLayer> Children
        {
            get { return children; }
            set { children = value; NotifyOfPropertyChange(() => Children); }
        }

        private bool isExpanded;

        public bool IsExpanded
        {
            get { return isExpanded; }
            set { isExpanded = value; NotifyOfPropertyChange(()=>IsExpanded); }
        }

        private bool isSelected;

        public bool IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; NotifyOfPropertyChange(()=>IsSelected); }
        }

        private bool isVisible;

        public bool IsVisible
        {
            get { return isVisible; }
            set { isVisible = value; NotifyOfPropertyChange(()=>IsVisible); }
        }

        public bool HasChildren
        {
            get { return Children.Any(); }
        }

        public sLayer Parent { get; set; }
    }
}