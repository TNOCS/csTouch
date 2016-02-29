using System.IO;

namespace csShared.ThirdParty.ImageLoaders
{
  internal interface ILoader
  {
    Stream Load(string source);
  }
}
