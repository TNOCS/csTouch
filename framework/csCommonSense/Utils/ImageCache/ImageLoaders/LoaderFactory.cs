using System;

namespace csShared.ThirdParty.ImageLoaders
{
  internal static class LoaderFactory
  {
    public static ILoader CreateLoader(SourceType sourceType, string source)
    {
      switch (sourceType)
      {
        case SourceType.Both:
          if (source.ToLower().StartsWith("http")) return new ExternalLoader();
          return new LocalDiskLoader();
        case SourceType.LocalDisk:
          return new LocalDiskLoader();
        case SourceType.ExternalResource:
          return new ExternalLoader();

        default:
          throw new ApplicationException("Unexpected exception");
      }
    }
  }
}
