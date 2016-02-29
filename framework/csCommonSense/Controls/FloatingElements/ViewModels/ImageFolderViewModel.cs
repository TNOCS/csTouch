using System;
using System.Windows.Threading;
using Caliburn.Micro;
using csShared.Documents;

namespace csShared.FloatingElements
{
  using System.ComponentModel.Composition;

  [Export(typeof(IDocument))]
  public class ImageFolderViewModel : Screen, IDocument
  {
    private Document _doc;
    private ImageFolderView _view;

    private bool _control;

    public bool Control
    {
      get { return _control; }
      set { _control = value; NotifyOfPropertyChange(()=>Control); }
    }
    

    [ImportingConstructor]
    public ImageFolderViewModel()
    {
      
      
    }

    void _view_PreviewTouchDown(object sender, System.Windows.Input.TouchEventArgs e)
    {
      ShowControl();
    }

    private DateTime _lastCheck;
    public DispatcherTimer ct = new DispatcherTimer();

    void ct_Tick(object sender, EventArgs e)
    {
      Control = false;
    }

    public void ShowControl()
    {
      if (!Control) Control = true;
      if (ct.IsEnabled) { ct.Stop(); }
      ct.Interval = new TimeSpan(0, 0, 0, 3);
      ct.Start();
    }

    void _view_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
      if (_lastCheck.AddMilliseconds(500) > DateTime.Now) ShowControl();
      _lastCheck = DateTime.Now;
    }

    private string _selectedPreviewImage;

    public string SelectedPreviewImage
    {
      get { return _selectedPreviewImage; }
      set
      {
        _selectedPreviewImage = value; NotifyOfPropertyChange(()=>SelectedPreviewImage);
      }
    }
    

    
    

    protected override void OnViewLoaded(object view)
    {
      base.OnViewLoaded(view);
      _view = (ImageFolderView) view;
      _view.PreviewMouseMove += _view_PreviewMouseMove;
      _view.PreviewTouchDown += _view_PreviewTouchDown;
      ct.Tick += ct_Tick;
        _view.PreviewImages.ItemsSource = _view.Images;
        //PreviewImages.AddRange(_view.Images);
    }

    public Document Doc
    {
      get { return _doc; }
      set { _doc = value; NotifyOfPropertyChange(()=>Doc); }
    }
  }
}
