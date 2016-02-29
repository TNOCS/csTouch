using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Caliburn.Micro;
using csCommon.Types.DataServer.Interfaces;
using ProtoBuf;

namespace DataServer {
    public enum MediaType {
        Photo,
        Audio,
        PTT
    }

    [ProtoContract]
    public class Media : PropertyChangedBase, IConvertibleXml {
        private byte[] byteArray;
        private BaseContent content;
        private string id;
        private BitmapSource image;
        private string localPath;
        private List<byte> packages = new List<byte>();
        private string publicUrl;
        private string title;
        private MediaType type;
        

        [ProtoMember(1)]
        public MediaType Type {
            get { return type; }
            set {
                type = value;
                NotifyOfPropertyChange(() => Type);
            }
        }

        [ProtoMember(2)]
        public string Id {
            get { return id; }
            set {
                id = value;
                NotifyOfPropertyChange(() => Id);
            }
        }


        [ProtoMember(3)]
        public string Title {
            get { return title; }
            set {
                title = value;
                NotifyOfPropertyChange(() => Title);
            }
        }


        public byte[] ByteArray {
            get { return byteArray; }
            set {
                byteArray = value;
                NotifyOfPropertyChange(() => ByteArray);
            }
        }

        [ProtoMember(4)]
        public string PublicUrl {
            get { return publicUrl; }
            set {
                publicUrl = value;
                NotifyOfPropertyChange(() => PublicUrl);
            }
        }

        public string LocalPath {
            get { return localPath; }
            set {
                localPath = value;
                NotifyOfPropertyChange(() => LocalPath);
            }
        }

        public BaseContent Content {
            get { return content; }
            set {
                content = value;
                NotifyOfPropertyChange(() => Content);
            }
        }

        public BitmapSource Image {
            get { return image; }
            set {
                image = value;
                NotifyOfPropertyChange(() => Image);
            }
        }

        public List<byte> Packages {
            get { return packages; }
            set { packages = value; }
        }

        //public Document Doc
        //{
        //    get
        //    {
        //        if (LocalUrl == null) return null;
        //        Document d = new Document();
        //        if (Type == MediaType.Photo) d.FileType = FileTypes.image;
        //        d.Location = "file://" + LocalUrl;
        //        return d;
        //    }
        //}

        public string Duration {
            get {
                //Microphone m = Microphone.Default;
                return (ByteArray.Length/(1600.0*2)).ToString() + " seconds";
            }
        }

        public event EventHandler MediaUpdated;

        public void UpdateDuration() {
            NotifyOfPropertyChange(() => Duration);
        }

        public string XmlNodeId
        {
            get { return "Media"; }
        }

        public XElement ToXml() {
            var res = new XElement("Media");
            res.SetAttributeValue("Id", Id);
            res.SetAttributeValue("Title", Title);
            res.SetAttributeValue("Type", Type);
            res.SetAttributeValue("PublicUrl", PublicUrl);
            return res;
        }

        public void FromXml(XElement element) {
            Id = element.GetString("Id");
            Title = element.GetString("Title");
            Type = (MediaType) Enum.Parse(typeof (MediaType), element.GetString("Type"));
            PublicUrl = element.GetString("PublicUrl");
        }

        public void PhotoReceived(string id, byte[] newByteArray, Service s) {
            ByteArray = newByteArray; 
            var img = new BitmapImage();
            //using (var ms = new MemoryStream(newByteArray)) {
            //    img.BeginInit();
            //    img.StreamSource = ms;
            //    img.EndInit();                
            //}
            Image = img;
            //using (var ms = new FileStream(Path.Combine(LocalPath, id), FileMode.Create)) {
            //    var encoder = new PngBitmapEncoder();
            //    encoder.Frames.Add(BitmapFrame.Create(img));
            //    encoder.Save(ms);
            //}

            if (MediaUpdated != null) MediaUpdated(this, null);

            //LocalPath = s.store.GetLocalUrl(s.Folder, Id);
            //s.store.SaveBytes(LocalPath, content);

            //BitmapSource bs = new BitmapImage();
            //FileStore.LoadImage(content, ref bs);
            //FileStore.LoadPhoto(content, ref bs);
            //Image = bs;
        }


        public void LoadPhoto(PoiService service) {
            LocalPath = service.store.GetLocalUrl(service.Folder, Id);
            if (!service.store.HasFile(LocalPath))
                service.RequestData(Id, PhotoReceived);
            else {
                LocalPath = service.store.GetLocalUrl(service.Folder,Id);
                BitmapSource bs = new BitmapImage(new Uri(LocalPath, UriKind.RelativeOrAbsolute));
                //bs.Freeze();
                //var b = service.store.GetBytes("", Id);
                //FileStore.LoadPhoto(b, ref bs);
                Image = bs;
            }
        }
    }
}