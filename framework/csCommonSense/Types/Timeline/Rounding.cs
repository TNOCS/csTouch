using System;
using System.Collections.Generic;
using System.Linq;

namespace csShared.Timeline
{
  /// <summary>
  /// Helper methods for time rounding
  /// </summary>
  public class Rounding
  {

    public static List<long> MinuteSpan = new List<long> { 1, 2, 5, 10, 15, 30, 60, 90, 120, 300, 600, 900, 1200, 1800, 3600, 7200, 14400, 28800, 57600, 86400, 115200, 230400, 460800, 921600, 1843200, 3686400, 7372800, 14745600, 29491200, 58982400, 117964800, 471859200, 3774873600, 60397977600, 120795955200 };

    /// <summary>
    /// Rounds a date value to a given minute interval
    /// </summary>
    /// <param name="time">Original time value</param>
    /// <param name="minuteInterval">Number of minutes to round up or down to</param>
    /// <param name="direction"></param>
    /// <returns></returns>
    public static DateTime RoundDateToMinuteInterval(DateTime time, long minuteInterval,
                             RoundingDirection direction)
    {
      //TimeSpan.TicksPerMinute 
      if (minuteInterval > 0)
      {
        long t = time.Ticks / (TimeSpan.TicksPerSecond * minuteInterval);
        DateTime nt = new DateTime(t * minuteInterval * TimeSpan.TicksPerSecond);
        return nt;
      }
      return time;    
    }

    /// <summary>
    /// Get nearby interval for a given timespan difference
    /// </summary>
    /// <param name="th">time difference in seconds</param>
    /// <returns></returns>
    internal static long GetInterval(double th)
    {
      long interval = MinuteSpan.First();
      foreach (long i in MinuteSpan)
      {
        if (th / i < 5) break;
        interval = i;
      }
      return interval;
    }
  }

  public enum RoundingDirection
  {
    RoundUp,
    RoundDown,
    Round
  }

}
