using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Caliburn.Micro;
using System.IO.Compression;

namespace csShared.Utils
{
  public class DownloadCache
  {
      private const long BUFFER_SIZE = 4096;

      private BackgroundWorker bw = new BackgroundWorker();

      public DownloadCache()
      {
          bw.DoWork += bw_DoWork;
          bw.RunWorkerAsync();
      }

      private static object _lock = new object();

      void bw_DoWork(object sender, DoWorkEventArgs e)
      {
          
        lock (_lock)
        { 
              var dir = Path.Combine(AppStateSettings.CacheFolder, "downloadcache");
              if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
              if (!File.Exists(dir + "\\unzipped"))
              {
                  try
                  {
                      var zipfilename = Directory.GetCurrentDirectory() + "\\downloadcache.zip";
                      var zipfiles = ZipFile.Open(zipfilename, ZipArchiveMode.Read);
                      foreach (var zipfile in zipfiles.Entries)
                      {
                          var filename = dir + "\\" + zipfile.FullName;
                          if (!File.Exists(filename))
                              zipfile.ExtractToFile(filename);
                      }
                      var f = File.CreateText(dir + "\\unzipped");
                      f.Close();
                  }
                  catch (Exception err)
                  {
                      // FIXME TODO Deal with exception! 
                  }
              }
          }
      }

    public string CalculateMD5Hash(string input)
    {
      // step 1, calculate MD5 hash from input
      MD5 md5 = System.Security.Cryptography.MD5.Create();
      byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
      byte[] hash = md5.ComputeHash(inputBytes);

      // step 2, convert byte array to hex string
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < hash.Length; i++)
      {
        sb.Append(hash[i].ToString("X2"));
      }
      return sb.ToString();
    }

    private Guid downloadGuid;

    public void DownloadString(Uri u)
    {
      DownloadString(u, false);
    }


    public void DownloadString(Uri u, bool refresh)
    {
      downloadGuid = AppStateSettings.Instance.AddDownload(u.AbsoluteUri, "");

      var local = GetFile(u.AbsoluteUri);

      if (File.Exists(local) && !refresh)
      {
        AppStateSettings.Instance.FinishDownload(downloadGuid);
        ThreadPool.QueueUserWorkItem(delegate
          {
            string result = File.ReadAllText(local);
            if (DownloadCompleted != null) 
              Execute.OnUIThread(()=>
                {
                  DownloadCompleted(this, new DownloadCacheEventArgs() { Cache = true, Data = result, Url = u.AbsoluteUri });    
                });
              
          });
        
      }
      else
      {
        if (!InternetConnection.IsConnected())
        {
          AppStateSettings.Instance.FinishDownload(downloadGuid);
          if (File.Exists(local))
          {
            string result = File.ReadAllText(local);
            if (DownloadCompleted != null)
              Execute.OnUIThread(() => DownloadCompleted(this, new DownloadCacheEventArgs() { Cache = true, Data = result, Url = u.AbsoluteUri }));
          }
          return;
          //          else
//          {
////            if (DownloadCompleted != null)
////              Execute.OnUIThread(() => DownloadCompleted(this, new DownloadCacheEventArgs() { Cache = true, Data = null, Url = u.AbsoluteUri }));
//          }
        }
        WebClient wc = new WebClient();
        wc.DownloadStringCompleted += wc_DownloadStringCompleted;
        wc.DownloadStringAsync(u, u.AbsoluteUri);
      }
    }

    public bool ExistsLocal(Uri u)
    {
      var local = GetFile(u.AbsoluteUri);
      var b= File.Exists(local);
      return b;
    }

    public string LocalUrl(Uri u)
    {
      var local = GetFile(u.AbsoluteUri);

      if (File.Exists(local))
        return local;

      return "";
    }

    public void DownloadFile(Uri u)
    {
      DownloadFile(u,false);
    }

    public void DownloadFile(Uri u, bool refresh)
    {
      downloadGuid = AppStateSettings.Instance.AddDownload(u.AbsoluteUri, "");

      var local = GetFile(u.AbsoluteUri);

      if (File.Exists(local)&& !refresh)
      {
        AppStateSettings.Instance.FinishDownload(downloadGuid);
        ThreadPool.QueueUserWorkItem(delegate
        {
          byte[] result = File.ReadAllBytes(local);
          if (DownloadFileCompleted != null)
            Execute.OnUIThread(() => DownloadFileCompleted(this, new DownloadCacheEventArgs() { Cache = true, Bytes = result, Url = u.AbsoluteUri }));
        });
      }
      else
      {
        if (!InternetConnection.IsConnected())
        {
          AppStateSettings.Instance.FinishDownload(downloadGuid);
          if (File.Exists(local))
          {
            byte[] result = File.ReadAllBytes(local);
            if (DownloadFileCompleted != null)
              Execute.OnUIThread(() => DownloadFileCompleted(this, new DownloadCacheEventArgs() { Cache = true, Bytes = result, Url = u.AbsoluteUri }));
          }
          return;
//          else
//          {
//            AppStateSettings.Instance.FinishDownload(downloadGuid);
////            if (DownloadFileCompleted != null)
////              Execute.OnUIThread(() => DownloadFileCompleted(this, new DownloadCacheEventArgs() { Cache = true, Bytes = null, Url = u.AbsoluteUri }));
//          }
        }
        WebClient wc = new WebClient();
        wc.DownloadDataCompleted += wc_DownloadDataCompleted;
        wc.DownloadDataAsync(u,local);        
      }
    }

    void wc_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
    {

      AppStateSettings.Instance.FinishDownload(downloadGuid);
      if (e.Error == null)
      {
        var local = e.UserState.ToString();
        File.WriteAllBytes(local, e.Result);
        if (DownloadFileCompleted != null)
          DownloadFileCompleted(this, new DownloadCacheEventArgs() { Cache = true, Bytes = e.Result, Url = e.UserState.ToString() });
      }
      //else //if (DownloadFileCompleted != null)
      //{
      //  //AppStateSettings.Instance.FinishDownload(downloadGuid);
      //  //if (DownloadFileCompleted != null)
      //  //  DownloadFileCompleted(this, new DownloadCacheEventArgs() { Cache = true, Bytes = null, Url = e.UserState.ToString() });
      //}
    }

    void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
    {
      AppStateSettings.Instance.FinishDownload(downloadGuid);
      if (e.Error == null)
      {
        var local = GetFile(e.UserState.ToString());
         File.WriteAllText(local, e.Result);
        if (DownloadCompleted != null)
          DownloadCompleted(this, new DownloadCacheEventArgs() { Cache = true, Data = e.Result, Url = e.UserState.ToString() });
      }
//      else
//      {
//        AppStateSettings.Instance.FinishDownload(downloadGuid);
////        if (DownloadCompleted != null)
////          DownloadCompleted(this, new DownloadCacheEventArgs() { Cache = true, Data = "", Url = e.UserState.ToString() });
//      }
    }

    private string GetFile(string e)
    {
      var dir = Path.Combine(AppStateSettings.CacheFolder, "downloadcache");
      if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
      string p = CalculateMD5Hash(e);
      var local = Path.Combine(dir, p);
      return local;
    }

    

    public event EventHandler<DownloadCacheEventArgs> DownloadCompleted;

    public event EventHandler<DownloadCacheEventArgs> DownloadFileCompleted;

    public class DownloadCacheEventArgs : EventArgs
    {
      public string Data { get; set; }
      public string Url { get; set; }
      public byte[] Bytes { get; set; }
      public bool Cache { get; set; }
    }


    
  }
}