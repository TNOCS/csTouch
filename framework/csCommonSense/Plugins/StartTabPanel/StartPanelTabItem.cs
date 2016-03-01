using System;
using System.Windows.Media;
using Caliburn.Micro;
using csShared.Controls.SlideTab;
using System.Diagnostics;

namespace csShared.TabItems
{

    public enum StartPanelPosition
    {
        bottom,
        left,
        [Obsolete("MenuRight is made collapsed (invisible), see StartPanelView.xaml")]
        right
    }

    [DebuggerDisplay("{Name}, {Position}")]
    public class StartPanelTabItem : PropertyChangedBase
    {
        public StartPanelPosition Position { get; set; }

        private string name;

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                NotifyOfPropertyChange(() => Name);
            }
        }

        private string tabText;

        public string TabText
        {
            get { return tabText; }
            set { tabText = value;
                if (TabItem != null) TabItem.TabText = value;
                NotifyOfPropertyChange(()=>TabText); }
        }
        

        public SlideTabItem TabItem { get; set; }

        public object ModelInstance { get; set; }

        public PanelSelection Panel = PanelSelection.bottom;

        private TabHeaderStyle headerStyle = TabHeaderStyle.Text;

        public TabHeaderStyle HeaderStyle
        {
            get { return headerStyle; }
            set { headerStyle = value; }
        }

        private ImageSource supportImage;

        public ImageSource SupportImage
        {
            get { return supportImage; }
            set { supportImage = value; NotifyOfPropertyChange(() => SupportImage); }
        }

        public ImageSource Image { get; set; }

        private bool isSelected;

        public bool IsSelected
        {
            get { return isSelected; }
            set {
                if (isSelected == value) return;
                isSelected = value;
                NotifyOfPropertyChange(()=>IsSelected);
                if (value && Selected != null) Selected(this, null);
                if (!value && Deselected != null) Deselected(this, null);
            }
        }

        public event EventHandler Selected;
        public event EventHandler Deselected;
    }
}
