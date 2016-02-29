using System;
using System.Globalization;
using System.IO;
using System.Windows.Media.Imaging;
using Caliburn.Micro;

namespace csShared.Documents
{
    public class Document : PropertyChangedBase, IEquatable<Document>
    {
        private FileTypes fileType = FileTypes.unknown;
        private int height;
        private string iconUrl;
        private string id;
        private bool isPrivate;
        private bool iscachable;
        private string location;
        private string name;
        private string originalUrl;
        private string sender;
        private string shareUrl;
        private bool showthumbnail;
        private string tag;
        private User user;
        private int width;

        private BitmapImage image;

        public BitmapImage Image
        {
            get { return image; }
            set
            {
                image = value;
                NotifyOfPropertyChange(() => Image);
            }
        }


        public string Tag
        {
            get { return tag; }
            set
            {
                tag = value;
                NotifyOfPropertyChange(() => Tag);
            }
        }

        public string Sender
        {
            get { return sender; }
            set
            {
                sender = value;
                NotifyOfPropertyChange(() => Sender);
            }
        }

        public string IconUrl
        {
            get { return iconUrl; }
            set
            {
                iconUrl = value;
                NotifyOfPropertyChange(() => IconUrl);
            }
        }


        public bool IsCachable
        {
            get { return iscachable; }
            set
            {
                iscachable = value;
                NotifyOfPropertyChange(() => IsCachable);
            }
        }

        public bool ShowThumbNail
        {
            get { return showthumbnail; }
            set
            {
                showthumbnail = value;
                NotifyOfPropertyChange(() => ShowThumbNail);
            }
        }

        public bool IsPrivate
        {
            get { return isPrivate; }
            set
            {
                isPrivate = value;
                NotifyOfPropertyChange(() => IsPrivate);
            }
        }

        public string OriginalUrl
        {
            get { return originalUrl; }
            set
            {
                originalUrl = value;
                NotifyOfPropertyChange(() => OriginalUrl);
            }
        }


        public string ShareUrl
        {
            get { return shareUrl; }
            set
            {
                shareUrl = value;
                NotifyOfPropertyChange(() => ShareUrl);
            }
        }

        public double? OriginalRotation { get; set; }


        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                NotifyOfPropertyChange(() => Name);
            }
        }

        public string Id
        {
            get { return id; }
            set
            {
                id = value;
                NotifyOfPropertyChange(() => Id);
            }
        }

        public string Location
        {
            get { return location; }
            set
            {
                location = ParseFileLocation(value);
                NotifyOfPropertyChange(() => Location);
            }
        }

        public User User
        {
            get { return user; }
            set
            {
                user = value;
                NotifyOfPropertyChange(() => User);
            }
        }

        public int Width
        {
            get { return width; }
            set
            {
                width = value;
                NotifyOfPropertyChange(() => Width);
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                height = value;
                NotifyOfPropertyChange(() => Height);
            }
        }

        /// <summary>
        ///     returns possible file type
        /// </summary>
        public FileTypes FileType
        {
            get
            {
                if (fileType != FileTypes.unknown) return fileType;
                if (Image != null) return FileTypes.image;
                if (Location == null) return FileTypes.unknown;
                string l = Location.ToLower();

                if (l.EndsWith(".avi") || l.EndsWith(".m4v") || l.EndsWith(".vob") || l.EndsWith(".wmv") ||
                    l.EndsWith(".mp4") || l.EndsWith(".asx")) return FileTypes.video;
                if (l.StartsWith("mms:")) return FileTypes.video;
                if (l.EndsWith(".xps")) return FileTypes.xps;
                if (l.EndsWith(".png") || l.EndsWith(".gif") || l.EndsWith(".jpg") || l.EndsWith(".bmp"))
                    return FileTypes.image;
                if (l.EndsWith(".3ds")) return FileTypes.threed;
                return FileTypes.unknown;
            }
            set
            {
                fileType = value;
            }
        }

        public string Channel { get; set; }

        #region IEquatable<Document> Members

        public bool Equals(Document other)
        {
            return Id == other.Id;
        }

        #endregion

        private string ParseFileLocation(string value)
        {
            string result = value;
            if (value != null && value.ToLower().StartsWith("file://"))
            {
                result = Directory.GetCurrentDirectory() + "\\" + value.Remove(0, 7);
            }
            return result;
        }

        public override string ToString()
        {
            if (Sender == null) Sender = AppStateSettings.Instance.Imb.Id.ToString(CultureInfo.InvariantCulture);
            return FileType + "|" + OriginalUrl + "|" + Sender;
        }

        public static Document FromString(string value)
        {
            try
            {
                string[] s = value.Split('|');
                var result = new Document();
                switch (s[0])
                {
                    case "image":
                        result.FileType = FileTypes.image;
                        break;
                    case "video":
                        result.FileType = FileTypes.video;
                        break;
                    case "web":
                        result.FileType = FileTypes.web;
                        break;
                }
                result.Location = s[1];
                if (s.Length > 2) result.Sender = s[2];

                //result.User = s[2];
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