using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Timers;

namespace csShared.Utils
{

  public static class InternetConnection
  {
    [DllImport("wininet.dll", CharSet = CharSet.Auto)]
    private extern static bool InternetGetConnectedState(ref InternetConnectionState_e lpdwFlags, int dwReserved);

    private static bool online = true;

    private static readonly Timer T = new Timer();

    public static void Start()
    {
      T.Interval = 10000;
      T.Elapsed += t_Elapsed;
      T.Start();
    }

    public static void Stop()
    {
      T.Stop();
    }

    static void t_Elapsed(object sender, ElapsedEventArgs e)
    {
      var wc = new WebClient {CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore)};
      wc.Headers.Add("Cache-Control", "no-cache");
      wc.DownloadStringCompleted += wc_DownloadStringCompleted;
      wc.DownloadStringAsync(new Uri("http://134.221.210.43/onlinetest.txt"));
    }

    static void wc_DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
    {
      Stop();
      if (e.Error != null)
      {
        var we = e.Error as WebException;
        if (we != null)
        {
          if (we.Status == WebExceptionStatus.ConnectFailure)
            online = false;
        }
        online = false;
      }
      else
      {
        online = true;
      }

      //try
      //{
      //  var result = e.Result;
      //  Online = true;
      //}
      //catch (WebException err)
      //{
      //  var state = err.Status;
      //}
      //catch (TargetInvocationException tie)
      //{
      //  Online = false;
      //}
      //catch (Exception error)
      //{
      //  Online = false;
      //}
    }

    [Flags]
    enum InternetConnectionState_e : int
    {
      INTERNET_CONNECTION_MODEM = 0x1,
      INTERNET_CONNECTION_LAN = 0x2,
      INTERNET_CONNECTION_PROXY = 0x4,
      INTERNET_RAS_INSTALLED = 0x10,
      INTERNET_CONNECTION_OFFLINE = 0x20,
      INTERNET_CONNECTION_CONFIGURED = 0x40
    }

    public static bool IsConnected()
    {

      // In function for checking internet
      InternetConnectionState_e flags = 0;
      bool isConnected = InternetGetConnectedState(ref flags, 0);
      if (!isConnected)
        T.Stop();
      if (isConnected && T.Enabled == false)
        T.Start();


      return isConnected && online;
    }
  }
}
