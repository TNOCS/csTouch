using System;
using Caliburn.Micro;

namespace csShared
{
    public class Pin : PropertyChangedBase
    {

        public delegate void UnPinHandler(Pin p);

        public UnPinHandler DoUnpin;

        private Guid id;

        public Guid Id
        {
            get { return id; }
            set { id = value; NotifyOfPropertyChange(()=>Id); }
        }
        

        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(()=>Title);
                
            }
        }

        private System.Windows.Media.Brush background = System.Windows.Media.Brushes.Black;

        public System.Windows.Media.Brush BackgroundBrush
        {
            get { return background; }
            set { background = value; NotifyOfPropertyChange(() => BackgroundBrush); }
        }

        private System.Windows.Media.Brush foreground = System.Windows.Media.Brushes.White;

        public System.Windows.Media.Brush ForegroundBrush
        {
            get { return foreground; }
            set { foreground = value; NotifyOfPropertyChange(() => ForegroundBrush); }
        }

        public object Tag { get; set; }

        public EventHandler Clicked;

        public void TriggerClicked()
        {
            if (Clicked != null) Clicked(this, null);
        }

        public void UnPin()
        {
            if (DoUnpin != null) DoUnpin(this);
        }

    }
}
