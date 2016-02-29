using System;
using System.Windows.Threading;
using Caliburn.Micro;
using csShared.Documents;

namespace csShared
{
  using System.ComponentModel.Composition;

  [Export(typeof(IDocument))]
  public class VideoViewModel : Screen, IDocument
  {
    private Document _doc;
    private VideoView _view;

    private bool _paused;

    private bool _control;

    public bool Control
    {
      get { return _control; }
      set { _control = value; NotifyOfPropertyChange(()=>Control); }
    }

    private double positionvalue;

    public double PositionValue
    {
        get { return positionvalue; }
        set { positionvalue = value;
            UpdateVideo(value); NotifyOfPropertyChange(()=>PositionValue);}
    }
    

    [ImportingConstructor]
    public VideoViewModel()
    {
      
      
    }

    public DispatcherTimer ct = new DispatcherTimer();
    public DispatcherTimer pbt = new DispatcherTimer();

    public void UpdateState()
    {
      if (_view.meMain.NaturalDuration == _view.meMain.Position)
        _paused = true;
      NotifyOfPropertyChange(() => CanPause);
      NotifyOfPropertyChange(()=>CanPlay);
    }

    protected override void OnViewLoaded(object view)
    {
      base.OnViewLoaded(view);
      _view = view as VideoView;
      _view.PreviewMouseMove += _view_PreviewMouseMove;
      _view.PreviewTouchDown += _view_PreviewTouchDown;
      _view.meMain.BufferingEnded += (e, s) => { UpdateState(); };
      _view.meMain.BufferingStarted += (e, s) => { UpdateState(); };
      _view.meMain.IsEnabledChanged += (e, s) => { UpdateState(); };
      _view.meMain.MediaOpened += (e, s) => { UpdateState(); };
      _view.meMain.MediaFailed += (e, s) => { UpdateState(); };
      _view.meMain.MediaEnded += (e, s) => { UpdateState(); };
      _view.meMain.MediaEnded += meMain_MediaEnded;
      _view.meMain.Play();
      ct.Tick += ct_Tick;
      pbt.Interval = new TimeSpan(0,0,0,0,100);
      pbt.Tick += pbt_Tick;
        pbt.Start();
  }

    void pbt_Tick(object sender, EventArgs e)
    {
        if (_view != null && _view.meMain != null && _view.meMain.IsLoaded && _view.meMain.NaturalDuration.HasTimeSpan)
        {
            var pos = _view.meMain.Position.TotalMilliseconds / _view.meMain.NaturalDuration.TimeSpan.TotalMilliseconds;
            PositionValue = pos;
        }
    }

    void meMain_MediaEnded(object sender, System.Windows.RoutedEventArgs e)
    {
      UpdateState();
    }

      void UpdateVideo(double value)
      {
          if (_view != null && _view.meMain != null && _view.meMain.IsLoaded)
          {
              var position = (int) (value * _view.meMain.NaturalDuration.TimeSpan.TotalMilliseconds);
              _view.meMain.Position = new TimeSpan(0,0,0,0,position);
          }
      }

    void _view_PreviewTouchDown(object sender, System.Windows.Input.TouchEventArgs e)
    {
      ShowControl();
    }

    private DateTime _lastCheck;

    void ct_Tick(object sender, EventArgs e)
    {
      Control = false;
    }

    public void ShowControl()
    {
      if (!Control) Control = true;
      if (ct.IsEnabled) { ct.Stop(); }
      ct.Interval = new TimeSpan(0,0,0,3);
      ct.Start();
      pbt.Start();
      UpdateState();
    }

    void _view_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
      if (_lastCheck.AddMilliseconds(500) > DateTime.Now) ShowControl();
      _lastCheck = DateTime.Now;
    }

    public bool CanPause
    {
      get { if (_view != null) return _view.meMain.CanPause && !_paused;
        return false;
      }
    }

    public bool CanPlay
    {
      get
      {
        if (_view != null) return !_view.meMain.IsBuffering && _paused;
        return false;
      }
    }

    public void Play()
    {
      _paused = false;
      if (_view.meMain.NaturalDuration == _view.meMain.Position)
        _view.meMain.Position = new TimeSpan(0);
      _view.meMain.Play();
      UpdateState();      
    }

    public void Pause()
    {
      _paused = true;
      _view.meMain.Pause();
      UpdateState();
    }

    public Document Doc
    {
      get { return _doc; }
      set { _doc = value; NotifyOfPropertyChange(()=>Doc); }
    }
  }
}
