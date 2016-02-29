using System;
using System.Windows.Media;
using Caliburn.Micro;

namespace csShared.Controls.Popups.MapCallOut
{
    public class CallOutAction : PropertyChangedBase
    {
        private string path;
        private string title;

        private Brush iconBrush;
        public Brush IconBrush
        {
            get { return iconBrush; }
            set
            {
                iconBrush = value;
                NotifyOfPropertyChange(() => IconBrush);
            }
        }

        private bool isdraggable;
        public bool IsDraggable
        {
            get { return isdraggable; }
            set
            {
                isdraggable = value;
                NotifyOfPropertyChange(() => IsDraggable);
            }
        }

        private object datacontext;
        public object DataContext
        {
            get { return datacontext; }
            set
            {
                datacontext = value;
                NotifyOfPropertyChange(() => DataContext);
            }
        }

        public string Title
        {
            get { return title; }
            set
            {
                title = value;
                NotifyOfPropertyChange(() => Title);
            }
        }

        public string Path
        {
            get { return path; }
            set
            {
                path = value;
                NotifyOfPropertyChange(() => Path);
            }
        }

        public event EventHandler Clicked;

        public void TriggerClicked(EventArgs e)
        {
            if (Clicked != null) 
                Clicked(this, e);
        }


        public event DragEvent DragStart;

        public delegate void DragEvent(object sender, object datacontext, EventArgs e);

        public void TriggerDragStart(object sender, EventArgs e)
        {
            if (DragStart != null)
                DragStart(sender, DataContext, e);
        }
    }
}
