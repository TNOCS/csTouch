using Caliburn.Micro;
using csShared.Documents;

namespace csShared.Geo
{
  public class Media : PropertyChangedBase
  {
    private string _receiver;

    public string Receiver
    {
      get { return _receiver; }
      set { _receiver = value; NotifyOfPropertyChange(()=>Receiver); }
    }

    private string _location;

    public string Location
    {
      get { return _location; }
      set { _location = value; NotifyOfPropertyChange(()=>Location); }
    }

    private FileTypes _type;

    public FileTypes Type
    {
      get { return _type; }
      set { _type = value; NotifyOfPropertyChange(()=>Type); }
    }
    
    
    
    
  }
}