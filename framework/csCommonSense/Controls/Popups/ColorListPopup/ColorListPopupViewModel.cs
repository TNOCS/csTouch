using System;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Caliburn.Micro;
using csShared.Interfaces;

namespace csShared.Controls.Popups.ColorListPopup
{

  public class MenuSelectedEventArgs : EventArgs
  {
    private object _object;

    public object Object
    {
      get { return _object; }
      set { _object = value; }
    }
    
  }

  [Export(typeof(IPopupScreen))]
  public class ColorListPopupViewModel : Screen, IPopupScreen
  {
    private ColorListPopupView view;

    public AppStateSettings AppState
    {
      get { return AppStateSettings.Instance; }
    }

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
    

    public event EventHandler<MenuSelectedEventArgs> Selected;

    private string _displayProperty;

    public string DisplayProperty
    {
      get { return _displayProperty; }
      set { _displayProperty = value; NotifyOfPropertyChange(()=>DisplayProperty); }
    }

    private BindableCollection<object> _objects = new BindableCollection<object>();

    public BindableCollection<object> Objects
    {
      get { return _objects; }
      set { _objects = value; NotifyOfPropertyChange(()=>Objects); }
    }
    
    

    private BindableCollection<System.Windows.Controls.MenuItem> _items = new BindableCollection<System.Windows.Controls.MenuItem>();

    public BindableCollection<System.Windows.Controls.MenuItem> Items
    {
      get { return _items; }
      set { _items = value; NotifyOfPropertyChange(()=>Items); }
    }

    public System.Windows.Controls.MenuItem AddMenuItem(LinearGradientBrush brush)
    {
      var miRemove = new System.Windows.Controls.MenuItem() { Header = "", FontFamily = new FontFamily("Segoe360"), FontSize = 18 };
      //var c = (Color)ColorConverter.ConvertFromString(header as string);
      miRemove.Background = brush;
      miRemove.Width = 200;
      miRemove.Click += miRemove_Click;
      Items.Add(miRemove);
      return miRemove;
    }

    void miRemove_Click(object sender, RoutedEventArgs e)
    {
      if (AutoClose) Close();
      if (Selected != null) Selected(this, new MenuSelectedEventArgs() { Object = ((System.Windows.Controls.MenuItem)sender).Background});
    }

    public ColorListPopupViewModel()
    {
      Objects.CollectionChanged += Items_CollectionChanged;

    }

    void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
      if (e.Action == NotifyCollectionChangedAction.Add)
      {
        foreach (var a in e.NewItems)
        {
          var mi = new System.Windows.Controls.MenuItem() { Tag = a };
          
          if (!string.IsNullOrEmpty(DisplayProperty))
          {
            PropertyInfo displayInfo = a.GetType().GetProperty(DisplayProperty);
            mi.Header = displayInfo.GetValue(a, null);
          }
          else
          {
            mi.Header = a.ToString();
          }
          mi.FontSize = 20;
          mi.FontFamily = new FontFamily("Segoe360");

          Items.Add(mi);
          mi.Click += MiClick;
          mi.TouchDown += MiClick;
        }
      }
    }

    void MiClick(object sender, System.Windows.RoutedEventArgs e)
    {
      if (AutoClose) Close();
      if (Selected != null) Selected(this, new MenuSelectedEventArgs() { Object = ((System.Windows.Controls.MenuItem)sender).Tag });

      
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
      view = (ColorListPopupView)theView;

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
          view.Items.Margin = new Thickness(Point.X, Point.Y, 0, 0);
          break;
        case VerticalAlignment.Bottom:
          view.Items.Margin = new Thickness(Point.X, 0, 0, Application.Current.MainWindow.ActualHeight - Point.Y);
          //view.Items.Margin = new Thickness(Point.X, Point.Y, 0, 0);
          break;
      }
    }
  }

}