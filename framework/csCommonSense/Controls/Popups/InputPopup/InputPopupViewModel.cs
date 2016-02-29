using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Caliburn.Micro;
using csShared.Interfaces;

namespace csShared.Controls.Popups.InputPopup
{



  public class InputPopupEventArgs : EventArgs
  {
    public string Result;
  }

  

  [Export(typeof(IPopupScreen))]
  public class InputPopupViewModel : Screen, IPopupScreen
  {
    private InputPopupView view;

    public AppStateSettings AppState
    {
      get { return AppStateSettings.Instance; }
    }

    public event EventHandler<InputPopupEventArgs> Saved;

    private bool _autoClose = true;

    public bool AutoClose
    {
      get { return _autoClose; }
      set { _autoClose = value; }
    }

    private VerticalAlignment _verticalAlignment;
    public VerticalAlignment VerticalAlignment { get { return _verticalAlignment; } set { _verticalAlignment = value; UpdatePosition(); NotifyOfPropertyChange(() => VerticalAlignment); } }

    private Point _point;

    public Point Point
    {
      get { return _point; }
      set { _point = value; NotifyOfPropertyChange(()=>Point); }
    }
    
    private string title;

    public string Title
    {
      get { return title; }
      set { title = value; NotifyOfPropertyChange(()=>Title); }
    }
    

    
    
    


    public InputPopupViewModel()
    {
      

    }


    

    public void Close()
    {
      AppState.Popups.Remove(this);
    }

    private Brush _background = Brushes.White;

    public Brush Background
    {
      get { return _background; }
      set { _background = value; NotifyOfPropertyChange(()=>Background); }
    }

    private FrameworkElement _relativeElement;

    public FrameworkElement RelativeElement
    {
      get { return _relativeElement; }
      set { _relativeElement = value; UpdatePosition();
      
      RelativeElement.LayoutUpdated += RelativeElement_LayoutUpdated;
        NotifyOfPropertyChange(()=>RelativeElement);}
    }

    void RelativeElement_LayoutUpdated(object sender, EventArgs e)
    {
      UpdatePosition();
    }

    private TimeSpan? _timeOut;

    public TimeSpan? TimeOut
    {
      get { return _timeOut; }
      set { _timeOut = value; }
    }
    

    private Point _relativePosition;

    public Point RelativePosition
    {
      get { return _relativePosition; }
      set { _relativePosition = value; UpdatePosition(); NotifyOfPropertyChange(() => RelativePosition); }
    }
    
    

    private Brush _border = Brushes.Black;

    public Brush Border
    {
      get { return _border; }
      set { _border = value; NotifyOfPropertyChange(()=>Border); }
    }


    private DispatcherTimer toTimer;
   
    protected override void OnViewLoaded(object theView)
    {
      base.OnViewLoaded(theView);
      view = (InputPopupView)theView;

      UpdatePosition();

      //Items = new BindableCollection<System.Windows.Controls.MenuItem>();

      if (TimeOut.HasValue)
      {
        toTimer = new DispatcherTimer();
        toTimer.Interval = TimeOut.Value;
        toTimer.Tick += toTimer_Tick;
        toTimer.Start();
      }

    }

    void toTimer_Tick(object sender, EventArgs e)
    {
      toTimer.Stop();
      Close();
    }

    public void Save()
    {
      if (Saved != null) Saved(this, new InputPopupEventArgs() {Result = DefaultValue});
      if (AutoClose) AppState.Popups.Remove(this);
    }

    public void Cancel()
    {
      AppState.Popups.Remove(this);
    }

    private void UpdatePosition()
    {
      if (view == null) return;
        if (_relativeElement != null)
        {
            Point = RelativeElement.TranslatePoint(RelativePosition, Application.Current.MainWindow);
        }
        else
        {
            Point = RelativePosition;
        }

      view.VerticalAlignment = this.VerticalAlignment;

      switch (view.VerticalAlignment)
      {
        case VerticalAlignment.Top:
          view.bInput.Margin = new Thickness(Point.X, Point.Y, 0, 0);
          break;
        case VerticalAlignment.Bottom:
          view.bInput.Margin = new Thickness(Point.X, 0, 0, Application.Current.MainWindow.ActualHeight - Point.Y);
          //view.Items.Margin = new Thickness(Point.X, Point.Y, 0, 0);
          break;
      }
      
    }

    private string defaultValue;

    public string DefaultValue
    {
      get { return defaultValue; }
      set { defaultValue = value; NotifyOfPropertyChange(()=>DefaultValue); }
    }


    private double width;

    public double Width
    {
      get { return width; }
      set { width = value; NotifyOfPropertyChange(()=>Width); }
    }
    
  }

}