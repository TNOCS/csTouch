using System;
using System.Windows.Media.Imaging;
using Caliburn.Micro;

namespace csImb
{
    public class Media : PropertyChangedBase
    {

        private BitmapImage image;

        public BitmapImage Image
        {
            get { return image; }
            set { image = value; }
        }
        

        private string _sender;

        public string Sender
        {
            get { return _sender; }
            set { _sender = value; NotifyOfPropertyChange(() => Sender); }
        }

        private string _location;

        public string Location
        {
            get { return _location; }
            set { _location = value; NotifyOfPropertyChange(()=>Location); }
        }

        private string _type;

        public string Type
        {
            get { return _type; }
            set { _type = value; NotifyOfPropertyChange(()=>Type); }
        }

        public override string ToString()
        {
            return this.Type + "|" + this.Location + "|" + this.Sender;
        }

        public static Media FromString(string value)
        {
            try
            {
                var s = value.Split('|');
                var result = new Media();
                result.Type = s[0];
                result.Location = s[1];
                result.Sender= s[2];
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing client status:" + e.Message);
                return null;
            }
        }
        
        
    }
}