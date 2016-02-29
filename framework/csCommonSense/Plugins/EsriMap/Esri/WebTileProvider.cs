using System.IO;
using Caliburn.Micro;
using System;

namespace csShared.Geo.Esri
{
    public class WebTileProvider : PropertyChangedBase, ITileImageProvider
    {
        private readonly string _cacheFolder;
        private readonly string _refer;
        private readonly string _userAgent;
        private readonly string _webUrl;
        private string _title;

        public string MBTileFile { get; set; }

        private bool activated;

        public bool Activated
        {
            get { return activated; }
            set { activated = value; NotifyOfPropertyChange(() => Activated); }
        }

        public string PreviewImage { get; set; }
        
        private TimeSpan cacheTimeout = new TimeSpan(356, 0, 0, 0);

        public TimeSpan CacheTimeout
        {
            get { return cacheTimeout; }
            set { cacheTimeout = value; }
        }

        public int DomainStart { get; set; }

        public WebTileProvider()
        {

        }

        public WebTileProvider(string title, string folder, string webUrl, string userAgent, string refer, string previewImage)
        {
            _title = title;
            _cacheFolder = folder;
            _webUrl = webUrl;
            _userAgent = userAgent;
            _refer = refer;

            //http://{0}.maptile.lbs.ovi.com/maptiler/v2/maptile/279af375be/terrain.day/{3}/{2}/{1}/256/png8?token=fee2f2a877fd4a429f17207a57658582&appId=nokiaMaps
            //http://khm0.google.nl/kh/v=104&src=app&x=16&y=10&z=5&s=Ga
            PreviewImage = string.IsNullOrEmpty(MBTileFile) 
                ? WebUrl(85, 132, 8) 
                : string.Format("file://{0}", Path.ChangeExtension(MBTileFile, "png"));
        }

        #region ITileImageProvider Members

        public string Title
        {
            get { return _title; }
            set { _title = value; NotifyOfPropertyChange(() => Title); }
        }

        public string CacheFolder
        {
            get { return _cacheFolder; }
           
        }

        public string UserAgent
        {
            get { return _userAgent; }
        }

        public string Refer
        {
            get { return _refer; }
        }

        public string WebUrl(int row, int col, int level)
        {
            var domain = (col + 2 * row) % 4 + DomainStart;
            return string.Format(_webUrl, domain, row, col, level);
        }



        #endregion
    }
}