using System.Windows.Media;
using csShared.Interfaces;
using csShared;
using System.Windows.Input;

namespace csBookmarkPlugin
{

    public class PinViewModel : IPluginScreen
    {
        
        public AppStateSettings AppState { get { 
            return AppStateSettings.Instance; }
            set { }
        }

        

        public Brush BackgroundBrush
        {
            get { 
                return AppState.AccentBrush; }
            set { }
        }
        

        public void Select(Pin p)
        {
            if (p!=null) p.TriggerClicked();
        }

        public void Select(Pin p, TouchEventArgs e)
        {
            e.Handled = true;
            if (p != null) p.TriggerClicked();
        }

        public string Name
        {
            get { return "Pins"; }
        }
    }
}
