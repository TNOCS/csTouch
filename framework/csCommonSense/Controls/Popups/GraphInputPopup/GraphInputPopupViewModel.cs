using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Caliburn.Micro;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using csShared.Interfaces;
using VerticalAlignment = System.Windows.VerticalAlignment;

namespace csShared.Controls.Popups.GraphInputPopup
{



  public class GraphInputPopupEventArgs : EventArgs
  {
    public SortedDictionary<double,double> Result;
  }

  

  [Export(typeof(IPopupScreen))]
  public class GraphInputPopupViewModel : Screen, IPopupScreen
  {
    private GraphInputPopupView view;

    public AppStateSettings AppState
    {
      get { return AppStateSettings.Instance; }
    }

    public event EventHandler<GraphInputPopupEventArgs> Saved;

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

    private double miny;
    public double MinimumY
    {
      get { return miny; }
      set { miny = value; NotifyOfPropertyChange(() => MinimumY); }
    }

    private double maxy;
    public double MaximumY
    {
      get { return maxy; }
      set { maxy = value; NotifyOfPropertyChange(() => MaximumY); }
    }


    private double minx;
    public double MinimumX
    {
      get { return minx; }
      set { minx = value; NotifyOfPropertyChange(() => MinimumX); }
    }

    private double maxx;
    public double MaximumX
    {
      get { return maxx; }
      set { maxx = value; NotifyOfPropertyChange(() => MaximumX); }
    }



    public GraphInputPopupViewModel()
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


    public PlotModel model;
    private DispatcherTimer toTimer;
   
    protected override void OnViewLoaded(object theView)
    {
      base.OnViewLoaded(theView);
      view = (GraphInputPopupView)theView;
      model = new PlotModel(Title);
      view.Plot.Model = model;
      
      s1.Title = Title;
      
      firstAxis.Key = "Left";
      firstAxis.Title = Title;
      firstAxis.MajorGridlineStyle = LineStyle.Solid;
      firstAxis.MinorGridlineStyle = LineStyle.Solid;
      firstAxis.Minimum = MinimumY;
      firstAxis.Maximum = MaximumY;
      firstAxis.Zoom(MinimumY,MaximumY);
      firstAxis.IsZoomEnabled = false;
      firstAxis.IsPanEnabled = false;

      
      bottomAxis.Key = "Bottom";
      bottomAxis.MajorGridlineStyle = LineStyle.Solid;
      bottomAxis.MinorGridlineStyle = LineStyle.Solid;
      bottomAxis.Minimum = MinimumX;
      bottomAxis.Maximum = MaximumX;
      bottomAxis.Zoom(MinimumX,MaximumX);
      bottomAxis.IsZoomEnabled = false;
      bottomAxis.IsPanEnabled = false;

      model.Axes.Clear();
      model.Axes.Add(firstAxis);
      model.Axes.Add(bottomAxis);
      model.Series.Add(s1);
      if (!s1.Points.Any())
      for (int i = (int)Math.Round(MinimumX,0); i <= Math.Round(MaximumX,0); i++)
      {
        s1.Points.Add(new DataPoint(i,MinimumY));
      }
      model.RefreshPlot(false);

        UpdatePosition();

      view.PreviewMouseLeftButtonDown += _view_PreviewMouseLeftButtonDown;
      view.PreviewMouseLeftButtonUp += _view_PreviewMouseLeftButtonUp;
      view.PreviewTouchDown += view_PreviewTouchDown;
      view.PreviewTouchUp += view_PreviewTouchUp;
      //Items = new BindableCollection<System.Windows.Controls.MenuItem>();

      if (TimeOut.HasValue)
      {
        toTimer = new DispatcherTimer();
        toTimer.Interval = TimeOut.Value;
        toTimer.Tick += toTimer_Tick;
        toTimer.Start();
      }

    }

    void view_PreviewTouchUp(object sender, TouchEventArgs e)
    {
      view.PreviewTouchMove -= view_PreviewTouchMove;
      view.ReleaseTouchCapture(e.TouchDevice);
      e.Handled = true;
      //var p = e.TouchDevice.GetTouchPoint(view.Plot).Position;
      //if (p.X > 0 && p.Y > 0 && p.X < view.Plot.ActualWidth && p.Y < view.Plot.ActualHeight)
      // {
      //  e.Handled = true;
      //}
      //else
      //{
      //
      //}
    }

    void view_PreviewTouchDown(object sender, TouchEventArgs e)
    {
      var p = e.TouchDevice.GetTouchPoint(view.Plot).Position;
      if (p.X > 0 && p.Y > 0 && p.X < view.Plot.ActualWidth && p.Y < view.Plot.ActualHeight)
      {
        view.CaptureTouch(e.TouchDevice);
        view.PreviewTouchMove += view_PreviewTouchMove;

        model.RefreshPlot(false);
        e.Handled = true;
      }
      else
      {

      }
    }

    void view_PreviewTouchMove(object sender, TouchEventArgs e)
    {
      if (s1 != null)
      {
        var p = e.TouchDevice.GetTouchPoint(view.Plot).Position;
        var plotpoint = Axis.InverseTransform(new ScreenPoint(p.X, p.Y), bottomAxis, firstAxis);
        bool found = false;
        foreach (var po in s1.Points)
        {
          if (Math.Round(po.X, 0) == Math.Round(plotpoint.X, 0) && plotpoint.X >= MinimumX && plotpoint.X <= MaximumX && plotpoint.Y >= MinimumY && plotpoint.Y <= MaximumY)
          {
            found = true;
            po.Y = plotpoint.Y;
          }
        }
        if (!found && plotpoint.X >= MinimumX && plotpoint.X <= MaximumX && plotpoint.Y >= MinimumY && plotpoint.Y <= MaximumY)
        {
          plotpoint.X = Math.Round(plotpoint.X, 0);
          bool found2 = false;
          foreach (var po in s1.Points)
          {
            if (po.X > plotpoint.X)
            {
              found2 = true;
              var idx = s1.Points.IndexOf(po);
              s1.Points.Insert(idx, plotpoint);
            }
          }
          if (!found2 && plotpoint.X >= MinimumX && plotpoint.X <= MaximumX && plotpoint.Y >= MinimumY && plotpoint.Y <= MaximumY)
            s1.Points.Add(plotpoint);
        }

        model.RefreshPlot(false);
        e.Handled = true;
      }
    }

    

    public LineSeries s1 = new LineSeries(){ MarkerType = MarkerType.None, StrokeThickness = 2};
    public LinearAxis firstAxis = new LinearAxis(AxisPosition.Left, "");
    public LinearAxis bottomAxis = new LinearAxis(AxisPosition.Bottom, "Time");

    void _view_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      var p = e.GetPosition(view.Plot);
      if (p.X > 0 && p.Y > 0 && p.X < view.Plot.ActualWidth && p.Y < view.Plot.ActualHeight)
      {
        view.CaptureMouse();
        view.PreviewMouseMove += _view_PreviewMouseMove;

        model.RefreshPlot(false);
        e.Handled = true;
      }
      else
      {
        
      }
    }

    void _view_PreviewMouseMove(object sender, MouseEventArgs e)
    {
      if (s1 != null)
      {
        var p = e.GetPosition(view.Plot);
        var plotpoint = Axis.InverseTransform(new ScreenPoint(p.X, p.Y), bottomAxis, firstAxis);
        bool found = false;
        foreach (var po in s1.Points)
        {
          if (Math.Round(po.X, 0) == Math.Round(plotpoint.X, 0) && plotpoint.X >= MinimumX && plotpoint.X <= MaximumX && plotpoint.Y >= MinimumY && plotpoint.Y <= MaximumY)
          {
            found = true;
            po.Y = plotpoint.Y;
          }
        }
        if (!found && plotpoint.X >= MinimumX && plotpoint.X <= MaximumX && plotpoint.Y >= MinimumY && plotpoint.Y <= MaximumY)
        {
          plotpoint.X = Math.Round(plotpoint.X, 0);
          bool found2 = false;
          foreach (var po in s1.Points)
          {
            if (po.X > plotpoint.X)
            {
              found2 = true;
              var idx = s1.Points.IndexOf(po);
              s1.Points.Insert(idx, plotpoint);
            }
          }
          if (!found2 && plotpoint.X >= MinimumX && plotpoint.X <= MaximumX && plotpoint.Y >= MinimumY && plotpoint.Y <= MaximumY)
            s1.Points.Add(plotpoint);
        }

        model.RefreshPlot(false);
        e.Handled = true;
      }
    }

    void _view_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      view.PreviewMouseMove -= _view_PreviewMouseMove;
      view.ReleaseMouseCapture();
      var p = e.GetPosition(view.Plot);
      if (p.X > 0 && p.Y > 0 && p.X < view.Plot.ActualWidth && p.Y < view.Plot.ActualHeight)
      {
        e.Handled = true;
      }
      else
      {
        
      }
    }

    void toTimer_Tick(object sender, EventArgs e)
    {
      toTimer.Stop();
      Close();
    }

    public void Save()
    {
      var slist = new SortedDictionary<double, double>();
      foreach (var p in s1.Points)
        slist.Add(p.X,p.Y);
      if (Saved != null) 
        Saved(this, new GraphInputPopupEventArgs(){ Result = slist });
      if (AutoClose)
        AppState.Popups.Remove(this);
    }

    public void Cancel()
    {
      AppState.Popups.Remove(this);
    }

    public void Clear()
    {
      s1.Points.Clear();
      for (int i = (int)Math.Round(MinimumX, 0); i <= Math.Round(MaximumX, 0); i++)
      {
        s1.Points.Add(new DataPoint(i, MinimumY));
      }
      model.RefreshPlot(false);
    }

    private void UpdatePosition()
    {
      if (view == null) return;
      if (_relativeElement != null)
      {
        Point = RelativeElement.TranslatePoint(RelativePosition, Application.Current.MainWindow);
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

    private double defaultValue;

    public double DefaultValue
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