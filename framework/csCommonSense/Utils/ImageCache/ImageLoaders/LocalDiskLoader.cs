using System.IO;

namespace csShared.ThirdParty.ImageLoaders
{
  internal class LocalDiskLoader: ILoader
  {

    public Stream Load(string source)
    {
      //Thread.Sleep(1000);
      return File.OpenRead(source);
    }
  }
}
