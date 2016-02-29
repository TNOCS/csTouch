using System.Globalization;
using System.Text;
using Caliburn.Micro;

namespace csShared.Geo.Esri
{

    

  public class BingTileProvider : PropertyChangedBase, ITileImageProvider
  {
    private readonly string cacheFolder;
    private readonly string refer;
    private readonly string userAgent;
    private readonly string webUrl;
    private  string title;
    private readonly string type;
    private readonly string version;

      private bool activated;

    public bool Activated
    {
        get { return activated; }
        set { activated = value; NotifyOfPropertyChange(() => Activated); }
    }

      private string _previewImage;

    public string PreviewImage
    {
        get { return _previewImage; }
        set { _previewImage = value; }
    }

    public BingTileProvider(string title, string folder, string webUrl, string userAgent, string refer, string type, string version, string key)
    {
      this.title = title;
      cacheFolder = folder;
      this.webUrl = webUrl;
      this.userAgent = userAgent;
      this.refer = refer;
      this.type = type;
      this.version = version;
      _previewImage = key;
    }

    #region ITileImageProvider Members

    public string Title
    {
      get { return title;  } set { title = value; }
    }

    public string CacheFolder
    {
      get { return cacheFolder; }
    }

    public string UserAgent
    {
      get { return userAgent; }
    }

    public string Refer
    {
      get { return refer; }
    }

    public string WebUrl(int row, int col, int level)
    {
      int domain = (col + 2*row)%4;
      string url = string.Format(CultureInfo.InvariantCulture,
       webUrl,domain, type, TileXYToQuadKey(col, row, level));
      return url;
    }

    /// <summary>
    /// Converts tile XY coordinates into a QuadKey at a specified level of detail.
    /// </summary>
    /// <param name="tileX">Tile X coordinate.</param>
    /// <param name="tileY">Tile Y coordinate.</param>
    /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
    /// to 23 (highest detail).</param>
    /// <returns>A string containing the QuadKey.</returns>
    /// Stole this methode from this nice blog: http://www.silverlightshow.net/items/Virtual-earth-deep-zooming.aspx. PDD.
    private static string TileXYToQuadKey(int tileX, int tileY, int levelOfDetail)
    {
      var quadKey = new StringBuilder();

      for (int i = levelOfDetail; i > 0; i--)
      {
        char digit = '0';
        int mask = 1 << (i - 1);

        if ((tileX & mask) != 0)
        {
          digit++;
        }

        if ((tileY & mask) != 0)
        {
          digit++;
          digit++;
        }

        quadKey.Append(digit);
      }

      return quadKey.ToString();
    }

    #endregion


    public string MBTileFile { get; set; }
  }
}