using System.Windows.Media.Imaging;
using Caliburn.Micro;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace csEvents
{
    

    public class TimeRow : PropertyChangedBase
    {
        private int actualOrder;
        private string id;
        private int order;

        private bool visible;

        public string Id
        {
            get { return id; }
            set
            {
                id = value;
                NotifyOfPropertyChange(() => Id);
            }
        }

        public bool Visible
        {
            get { return visible; }
            set
            {
                visible = value;
                NotifyOfPropertyChange(() => Visible);
            }
        }

        private System.Windows.Media.Color color;

        public System.Windows.Media.Color Color
        {
            get { return color; }
            set { color = value; NotifyOfPropertyChange(()=>Color); }
        }

        private object data;

        public object Data
        {
            get { return data; }
            set { data = value; NotifyOfPropertyChange(()=>Data); }
        }

        private BitmapSource image;

        public BitmapSource Image
        {
            get { return image; }
            set { image = value; NotifyOfPropertyChange(()=>Image); }
        }
        
        
        
        

        public int Order
        {
            get { return order; }
            set
            {
                order = value;
                NotifyOfPropertyChange(() => Order);
            }
        }

       
        
        public int ActualOrder
        {
            get { return actualOrder; }
            set
            {
                actualOrder = value;
                NotifyOfPropertyChange(() => ActualOrder);
            }
        }

        //public TimeTabViewModel Tab { get; set; }
    }
}
