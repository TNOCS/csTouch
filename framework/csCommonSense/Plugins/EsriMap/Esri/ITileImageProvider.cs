namespace csShared.Geo.Esri
{
  public interface ITileImageProvider
  {
    string Title { get; set; }
    string CacheFolder { get; }
    string UserAgent { get; }
    string Refer { get; }
      string MBTileFile { get; set; }
    string WebUrl(int row, int col, int level);
      bool Activated { get; set; }
      
  }
}