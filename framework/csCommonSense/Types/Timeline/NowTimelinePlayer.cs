using System;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using csShared.Interfaces;
using IMB3;

namespace csShared.Timeline
{

  public class NowTimelinePlayer : PropertyChangedBase, ITimelinePlayer
  {
    #region properties

      private static readonly AppStateSettings AppState = AppStateSettings.Instance;

    Timer _timer;

    public ITimelineManager Timeline { get; set; }
    private bool _fixFocus;

    private bool _fixTimeline;
    public bool FixTimeline { get { return _fixTimeline; } set { _fixTimeline = value; NotifyOfPropertyChange(() => FixTimeline); } }

    bool _active;

    public TimeSpan Backward { get; set; }
    public TimeSpan Forward { get; set; }
    bool _oldFixTimeline;

    public bool FixFocus
    {
      get { return _fixFocus; }
      set { _fixFocus = value; NotifyOfPropertyChange(()=>FixFocus); }
    }

    #endregion

    public void Init()
    {
      _timer = new Timer {Interval = 1000};
        _timer.Elapsed += Elapsed;
      //_timer.Start();
    }

    void Elapsed(object sender, ElapsedEventArgs e)
    {
        if (!_active || Timeline == null) return;
        if (Timeline.CurrentTime != new DateTime())
        {
            if (!_oldFixTimeline && FixTimeline)
            {
                Forward = Timeline.End - Timeline.CurrentTime;
                Backward = Timeline.CurrentTime - Timeline.Start;
            }
        }
        switch (Timeline.TimelineFix)
        {
            case TimelineFixStyles.Custom:

                if (FixTimeline)
                {
                    Timeline.End = AppState.TimelineManager.CurrentTime + Forward;
                    Timeline.Start = AppState.TimelineManager.CurrentTime - Backward;
                } 

                break;
            case TimelineFixStyles.Year:
                Timeline.End = AppState.TimelineManager.CurrentTime;
                Timeline.Start = AppState.TimelineManager.CurrentTime.AddYears(-1);
                break;
            case TimelineFixStyles.Month:
                Timeline.End = AppState.TimelineManager.CurrentTime;
                Timeline.Start = AppState.TimelineManager.CurrentTime.AddMonths(-1);
                break;
            case TimelineFixStyles.Week:
                Timeline.End = AppState.TimelineManager.CurrentTime;
                Timeline.Start = AppState.TimelineManager.CurrentTime.AddDays(-7);
                break;
            case TimelineFixStyles.Day:
                Timeline.End = AppState.TimelineManager.CurrentTime;
                Timeline.Start = AppState.TimelineManager.CurrentTime.AddDays(-1);
                break;
            case TimelineFixStyles.Hour:
                Timeline.End = AppState.TimelineManager.CurrentTime;
                Timeline.Start = AppState.TimelineManager.CurrentTime.AddHours(-1);
                break;
            case TimelineFixStyles.Min15:
                Timeline.End = AppState.TimelineManager.CurrentTime;
                Timeline.Start = AppState.TimelineManager.CurrentTime.AddMinutes(-15);
                break;
            case TimelineFixStyles.Min5:
                Timeline.End = AppState.TimelineManager.CurrentTime;
                Timeline.Start = AppState.TimelineManager.CurrentTime.AddMinutes(-5);
                break;
            case TimelineFixStyles.Min1:
                Timeline.End = AppState.TimelineManager.CurrentTime;
                Timeline.Start = AppState.TimelineManager.CurrentTime.AddMinutes(-1);
                break;
        }
        
        
        //Timeline.CurrentTime = DateTime.Now;
        if (FixFocus)
        {
            Timeline.FocusTime = Timeline.CurrentTime;
          
        }

        Timeline.ForceTimeChanged();
        _oldFixTimeline = FixTimeline;

        Dispatcher.CurrentDispatcher.BeginInvoke(new System.Action(delegate
        {
            double w = Application.Current.MainWindow.ActualWidth;

            var interval = Math.Min(
                Math.Max(new TimeSpan((Timeline.End.Ticks - Timeline.Start.Ticks) / Convert.ToInt64(w)).TotalMilliseconds, 2000), 2000);
            _timer.Interval = interval;

                                      
        }));
    }

    public void Begin()
    {
      _active = true;
      //_timer.Start();
      if (AppStateSettings.Instance.Imb != null && AppStateSettings.Instance.Imb.IsConnected)
      {
        AppStateSettings.Instance.Imb.Imb.OnVariable += Imb_OnVariable;        
      }
      //Timeline.TimeChanged += Timeline_TimeChanged;
      //Timeline.TimeContentChanged += Timeline_TimeContentChanged;
    }

    void Imb_OnVariable(TConnection aConnection, string aVarName, byte[] aVarValue, byte[] aPrevValue)
    {
      if (aVarName == "timespan-old")
      {
        string v = Encoding.UTF8.GetString(aVarValue);
        string[] vs = v.Split('|');
        if (vs.Length > 2)
        {
          Timeline.Start = new DateTime(1970, 1, 1).AddMilliseconds(Convert.ToInt64(vs[0]));
          Timeline.End = new DateTime(1970, 1, 1).AddMilliseconds(Convert.ToInt64(vs[1]));
          Timeline.ForceTimeChanged();
          Timeline.ForceTimeContentChanged();
          _ignoreNext = true;
          //_timeUpdated = true;
        }

      }
    }

    void Timeline_TimeContentChanged(object sender, TimeEventArgs e)
    {
      SendUpdate();
    }

      //private AppStateSettings appState = AppStateSettings.Instance;

    private DateTime _lastImbTimelineUpdate = AppState.TimelineManager.CurrentTime;
    private bool _ignoreNext = false;

    void Timeline_TimeChanged(object sender, TimeEventArgs e)
    {
      if (_lastImbTimelineUpdate.AddSeconds(0.1) < AppState.TimelineManager.CurrentTime && AppStateSettings.Instance.Imb != null && AppStateSettings.Instance.Imb.IsConnected && !_ignoreNext)
      {
        SendUpdate();
        _ignoreNext = false;
      }
    }

    private void SendUpdate()
    {
      return;
/*
      long start = (long)(Timeline.Start - new DateTime(1970, 1, 1)).TotalMilliseconds;
      long end = (long)(Timeline.End - new DateTime(1970, 1, 1)).TotalMilliseconds;
      long focus = (long)(Timeline.FocusTime - new DateTime(1970, 1, 1)).TotalMilliseconds;
      if (AppStateSettings.Instance.Imb.Imb != null)
      {
        AppStateSettings.Instance.Imb.Imb.SetVariableValue("timespan", start + "|" + end + "|" + focus);
      }
      _lastImbTimelineUpdate = DateTime.Now;
*/
    }

    

    

    public void Stop()
    {
      _active = false;
      if (_timer != null && _timer.Enabled) _timer.Stop();
    }



  }
}
