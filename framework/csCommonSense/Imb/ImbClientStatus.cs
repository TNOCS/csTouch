using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using IMB3;

namespace csImb
{
    public delegate void PositionChanged(object sender, PositionChangedEventArgs e);

    public class ImbClientStatus : PropertyChangedBase
    {
        public TEventEntry Commands;
        public TEventEntry Media;
        public TEventEntry Positions;
        private string action;
        private string application;
        private string capabilities;
        private bool client = true;
        private string displayName;
        private Int32 id;
        private string manuifactor;
        private string myImage;
        private string name;
        private string orientation;
        private string os;

        private Position position;
        private int quality = 4;
        private int resolutionX;
        private int resolutionY;
        private string tagId;

        private string type;

        public Position Position
        {
            get { return position; }
            set
            {
                position = value;
                NotifyOfPropertyChange(() => Position);
            }
        }

        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        public string Action
        {
            get { return action; }
            set { action = value; }
        }

        private bool isFollowing;

        public bool IsFollowing
        {
            get { return isFollowing; }
            set
            {
                isFollowing = value;
                NotifyOfPropertyChange(() => IsFollowing);
            }
        }

        private bool isFollowingMyMap;

        public bool IsFollowingMyMap
        {
            get { return isFollowingMyMap; }
            set { isFollowingMyMap = value; NotifyOfPropertyChange(()=>IsFollowingMyMap); }
        }

        public string Capabilities
        {
            get
            {
                var r = AllCapabilities.Aggregate("", (current, s) => current + (s + ","));
                return r.TrimEnd(',');
            }
            set
            {
                capabilities = value;
                AllCapabilities = new List<string>();
                foreach (var s in capabilities.Split(','))
                {
                    AllCapabilities.Add(s);
                }
                NotifyOfPropertyChange(() => AllCapabilities);
            }
        }

        private List<string> _allCapabilities = new List<string>();

        public List<string> AllCapabilities
        {
            get { return _allCapabilities; }
            set
            {
                _allCapabilities = value;
                NotifyOfPropertyChange(() => AllCapabilities);
                NotifyOfPropertyChange(() => Capabilities);
            }
        }

        public int UserId { get; set; }


        public Int32 Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Manunifactor
        {
            get { return manuifactor; }
            set { manuifactor = value; }
        }

        public string DisplayName
        {
            get { return displayName; }
            set { displayName = value; }
        }

        public string Application
        {
            get { return application; }
            set { application = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string Orientation
        {
            get { return orientation; }
            set { orientation = value; }
        }

        public string Os
        {
            get { return os; }
            set { os = value; }
        }


        public int ResolutionX
        {
            get { return resolutionX; }
            set
            {
                resolutionX = value;
                NotifyOfPropertyChange(() => ResolutionX);
            }
        }

        public int ResolutionY
        {
            get { return resolutionY; }
            set
            {
                resolutionY = value;
                NotifyOfPropertyChange(() => ResolutionY);
            }
        }

        public string TagID
        {
            get { return tagId; }
            set
            {
                tagId = value;
                NotifyOfPropertyChange(() => TagID);
            }
        }

        public string MyImage
        {
            get { return myImage; }
            set
            {
                myImage = value;
                NotifyOfPropertyChange(() => MyImage);
            }
        }

        public int Quality
        {
            get { return quality; }
            set
            {
                quality = value;
                NotifyOfPropertyChange(() => Quality);
            }
        }

        public bool Client
        {
            get { return client; }
            set { client = value; }
        }

        public event PositionChanged PositionChanged;

        public void FromString(string value)
        {
            try
            {
                var s = value.Split('|');
                Action = s[0];
                Id = int.Parse(s[1]);
                Manunifactor = s[2];
                Name = s[3];
                Orientation = s[4];
                Os = s[5];
                ResolutionX = int.Parse(s[6]);
                ResolutionY = int.Parse(s[7]);
                TagID = s[8];
                MyImage = s[9];
                Quality = int.Parse(s[10]);
                Type = s[11];
                Client = Convert.ToBoolean(s[12]);
                Application = s[13];
                DisplayName = s[14];
                Capabilities = s[15];
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing client status:" + e.Message);
            }
        }



        public override string ToString()
        {
            return action + "|" + id + "|" + manuifactor + "|" + name + "|" + orientation + "|" + os + "|" +
                   resolutionX + "|" + resolutionY + "|" + tagId + "|" + myImage + "|" + quality + "|" + type +
                   "|" + client + "|" + application + "|" + displayName + "|" + Capabilities;
        }


        internal void UpdatePosition(string c)
        {
            var cc = c.Split('|');
            try
            {
                var p = new Position
                {
                    Date = DateTime.Now,
                    Latitude = double.Parse(cc[0], CultureInfo.InvariantCulture),
                    Longitude = double.Parse(cc[1], CultureInfo.InvariantCulture),
                    Precision = double.Parse(cc[2], CultureInfo.InvariantCulture),
                    Course = double.Parse(cc[3], CultureInfo.InvariantCulture),
                    Speed = double.Parse(cc[4], CultureInfo.InvariantCulture)
                };
                Position = p;
                OnPositionChanged(p);
            }
            catch (Exception)
            {
                Console.WriteLine("Error parsing position ");
            }
        }

        public void OnPositionChanged(Position position)
        {
            var handler = PositionChanged;
            if (handler != null) PositionChanged(this, new PositionChangedEventArgs(position, this));
        }

        public void AddCapability(string p)
        {
            if (!AllCapabilities.Contains(p))
            {
                AllCapabilities.Add(p);
                NotifyOfPropertyChange(() => AllCapabilities);
                NotifyOfPropertyChange(() => Capabilities);
            }
        }

        public void RemoveCapability(string p)
        {
            if (AllCapabilities.Contains(p)) AllCapabilities.Remove(p);
        }

        private bool allowFollowMap = true;

        public bool AllowFollowMap
        {
            get { return allowFollowMap; }
            set { allowFollowMap = value; }
        }

        public BitmapImage Image
        {
            get
            {
                switch (Type.ToLower())
                {
                    case "display":
                        return new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/screen.png"));
                    // FIXME TODO: Unreachable code
                        //break;
                }
                return new BitmapImage(new Uri("pack://application:,,,/csCommon;component/Resources/Icons/person.png"));
            }
        }

    }
}