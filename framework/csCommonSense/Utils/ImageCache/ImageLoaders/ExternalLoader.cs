using System;
using System.IO;
using System.Linq;
using System.Net;

namespace csShared.ThirdParty.ImageLoaders
{
  class ExternalLoader : ILoader
  {
    #region ILoader Members

    public System.IO.Stream Load(string source)
    {
      try
      {

      
      var webClient = new WebClient();
      webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; .NET CLR 1.0.3705;)");
      webClient.Headers.Add("Referer","http://maps.google.com");
      byte[] html = webClient.DownloadData(source);

      if (html == null || html.Count() == 0) return null;

      return new MemoryStream(html);
      }
      catch (Exception)
      {

        return null;
      }
    }

    #endregion
  }
}