using Caliburn.Micro;
using csShared.Timeline;

namespace csShared.StartPanel
{
  public class StartPanelSettings : PropertyChangedBase
  {


    private string _modelViewID;

    public string ModelViewID
    {
      get { return _modelViewID; }
      set { _modelViewID = value; }
    }
    

    private StartPanelOrientation _orientation;

    public StartPanelOrientation Orientation
    {
      get { return _orientation; }
      set { _orientation = value; NotifyOfPropertyChange(()=>Orientation); }
    }

    private TimelineSettings _timelineSettings;

    public TimelineSettings TimelineSettings
    {
      get { return _timelineSettings; }
      set { _timelineSettings = value; NotifyOfPropertyChange(()=>TimelineSettings); }
    }
    

  }
}
