using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Caliburn.Micro;
using System.Net.Cache;

namespace csShared.Utils
{
  public class MediaToCache
  {
    public string Uri;
    public bool Refresh;
  }
  public class CachedMedia
  {
    public string Filename;
    public int RetryCount;
    public bool Done;
  }
  public class MediaCache
  {
    readonly Dictionary<string, CachedMedia> _hashTable = new Dictionary<string, CachedMedia>();
    public static string Folder = Path.Combine(AppStateSettings.CacheFolder,"Media");
    public WebClient Wc = new WebClient();
    public delegate void DownloadComplete(string orig, string cached, string hashcode);
    public event DownloadComplete DownloadCompleted;

    public Queue<MediaToCache> Queu = new Queue<MediaToCache>();

    readonly BackgroundWorker _bw = new BackgroundWorker();
    private readonly Object _obj = new object();
    private bool _done = true;

    public MediaCache()
    {
      if (!Execute.InDesignMode)
      {
        if (!Directory.Exists(Folder)) Directory.CreateDirectory(Folder);

        Wc.DownloadFileCompleted += WcDownloadFileCompleted;

        _bw.DoWork += BwDoWork;
        GetList();
        _bw.RunWorkerAsync();
      }
    }

    void BwDoWork(object sender, DoWorkEventArgs e)
    {
      if (!InternetConnection.IsConnected()) 
        return;
      var q = new List<MediaToCache>();
      lock (_obj)
      {
        q = Queu.ToList();
      }
      while (q.Count>0)
      {
        if (!InternetConnection.IsConnected())
          return;
        lock (_obj)
        {
          q = Queu.ToList();
        }
        try
        {
          if (q.Any() && _done)
          {
            _done = false;

            var u = new MediaToCache();
            lock (_obj)
            {

              u = Queu.Peek();
            }
            var idx = u.Uri.LastIndexOf(".");
            var type = "";
            if (idx > 0)
            {
              type = u.Uri.Substring(idx, u.Uri.Length - idx);
            }
            if (u.Refresh)
            {
              Wc.CachePolicy = new System.Net.Cache.RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
              Wc.DownloadFileAsync(new Uri(u.Uri), Path.Combine(Folder, u.Uri.GetHashCode().ToString() + type + ".refresh"), u); // REVIEW TODO: Used Path instead of String concat.
            }
            else
                Wc.DownloadFileAsync(new Uri(u.Uri), Path.Combine(Folder, u.Uri.GetHashCode().ToString() + type), u); // REVIEW TODO: Used Path instead of String concat.
          }
        }
        catch (Exception)
        {
          
          
        }
        
      }
      Thread.Sleep(10);
      
    }

    void WcDownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
    {
      try
      {

      var u = e.UserState as MediaToCache;
      string hashcode = u.Uri.GetHashCode().ToString();
      var filename = Path.Combine(Folder, "" + u.Uri.GetHashCode());
      var idx = u.Uri.LastIndexOf(".");
      var type = "";
      if (idx > 0)
      {
        type = u.Uri.Substring(idx, u.Uri.Length - idx);
      }

      if (e.Error == null)
      {
        if (u.Refresh)
        {
          if (File.Exists(filename + type+".refresh"))
          {
            //File.Delete(filename + type);
            File.Copy(filename + type + ".refresh", filename + type, true);
            File.Delete(filename + type + ".refresh");
            _hashTable[hashcode].Filename = Path.Combine(Folder, u.Uri.GetHashCode().ToString() + type);
            _hashTable[hashcode].Done = true;
            lock (_obj)
            {
              Queu.Dequeue();
            }
            if (DownloadCompleted != null)
              DownloadCompleted(u.Uri, _hashTable[hashcode].Filename, hashcode);
          }
        }
        else if (File.Exists(filename + type))
        {
          _hashTable[hashcode].Filename = Path.Combine(Folder + u.Uri.GetHashCode().ToString() + type);
          _hashTable[hashcode].Done = true;
          lock (_obj)
          {
            Queu.Dequeue();
          }
          if (DownloadCompleted != null)
            DownloadCompleted(u.Uri, _hashTable[hashcode].Filename, hashcode);
        }

      }
      else
      {
        if (File.Exists(filename + type))
        {
          _hashTable[hashcode].Filename = Path.Combine(Folder, u.Uri.GetHashCode().ToString() + type);
          _hashTable[hashcode].Done = true;
          lock (_obj)
          {
            Queu.Dequeue();
          }
          if (DownloadCompleted != null)
            DownloadCompleted(u.Uri, _hashTable[hashcode].Filename, hashcode);
        }
        else
        {
          //var filename = Folder + u.OriginalString.GetHashCode();
          _hashTable[hashcode].Filename = Path.Combine(Folder, "Failed.gif");
          _hashTable[hashcode].Done = true;
          lock (_obj)
          {
            Queu.Dequeue();
          }
        }
      }
      }
      catch (Exception err)
      {
        Logger.Log("MediaCache","DownloadMediaFile",err.Message,Logger.Level.Error);
      }
      finally
      {
        _done = true;
      }

    }

    public string GetFile(string uri, bool refresh)
    {
      if (!uri.StartsWith("http")) return Path.Combine(Folder, "Failed.gif");
      var u = uri.GetHashCode().ToString();
      if (!_hashTable.ContainsKey(u))
      {
        _hashTable.Add(u, new CachedMedia(){Done = false, Filename = "",RetryCount = 0});
        lock (_obj)
        {
          Queu.Enqueue(new MediaToCache() { Uri = uri, Refresh = refresh });
        }
        if (!_bw.IsBusy)
          _bw.RunWorkerAsync();
        return uri;
      }
      else if (_hashTable[u].Filename != "" && !_hashTable[u].Filename.EndsWith("Failed.gif"))
      {
        if (refresh)
        {
          if (!InternetConnection.IsConnected())
          {
            if (DownloadCompleted != null)
              DownloadCompleted(uri, _hashTable[u].Filename, u);
            return _hashTable[u].Filename;
          }
          lock (_obj)
          {
            Queu.Enqueue(new MediaToCache() { Uri = uri, Refresh = refresh });
          }
          if (!_bw.IsBusy)
            _bw.RunWorkerAsync();
          return _hashTable[u].Filename;
        }
        if (Queu.Count > 0 && !_bw.IsBusy) _bw.RunWorkerAsync();
        return _hashTable[u].Filename;
      }
      else if (_hashTable[u].Filename == "Failed.gif")
      {
        lock (_obj)
        {
          if (!Queu.Contains(new MediaToCache() {Uri = uri, Refresh = refresh}))
            Queu.Enqueue(new MediaToCache() {Uri = uri, Refresh = refresh});
        }
        if (!_bw.IsBusy)
          _bw.RunWorkerAsync();
        return _hashTable[u].Filename;
      }
      else
      {
        lock (_obj)
        {
          if (!Queu.Contains(new MediaToCache() {Uri = uri, Refresh = refresh}))
            Queu.Enqueue(new MediaToCache() {Uri = uri, Refresh = refresh});
        }
        if (!_bw.IsBusy)
          _bw.RunWorkerAsync();
        return uri;
      }
    }

    public void GetList()
    {
      {
        lock (_obj)
        {
          var fls = Directory.GetFiles(Folder);
          foreach (var fl in fls)
          {
            string f = "";
            try
            {
              f = fl.Substring(fl.LastIndexOf('\\') + 1);
              f = f.Substring(0, f.LastIndexOf('.'));
              _hashTable.Add(f, new CachedMedia() { Done = true, Filename = fl, RetryCount = -1 });
            }
            catch (Exception strerr)
            {
              var err = strerr.ToString();
            }
          }
          
        }
      }
    }

   

  }
}
