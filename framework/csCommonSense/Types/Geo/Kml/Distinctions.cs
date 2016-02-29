using System.Collections.ObjectModel;
using Caliburn.Micro;

namespace csShared.Geo
{
  public class Distinction : PropertyChangedBase
  {
    private bool _active;
    private ObservableCollection<FieldDistinctions> _fields;
    private string _name;
    private DistinctionSortTypes _type;

    public string Name
    {
      get { return _name; }
      set
      {
        _name = value;
        NotifyOfPropertyChange(() => Name);
      }
    }

    public bool Active
    {
      get { return _active; }
      set
      {
        _active = value;
        NotifyOfPropertyChange(() => Active);
      }
    }

    public ObservableCollection<FieldDistinctions> Fields
    {
      get { return _fields; }
      set
      {
        _fields = value;
        NotifyOfPropertyChange(() => Fields);
      }
    }


    public DistinctionSortTypes Type
    {
      get { return _type; }
      set
      {
        _type = value;
        NotifyOfPropertyChange(() => Type);
      }
    }
  }

  public class FieldDistinctions : PropertyChangedBase
  {
    private bool _active;
    private int _count;
    private string _name;

    public string Name
    {
      get { return _name; }
      set
      {
        _name = value;
        NotifyOfPropertyChange(() => Name);
      }
    }

    public bool Active
    {
      get { return _active; }
      set
      {
        _active = value;
        NotifyOfPropertyChange(() => Active);
      }
    }

    public int Count
    {
      get { return _count; }
      set
      {
        _count = value;
        NotifyOfPropertyChange(() => Count);
      }
    }

    public override string ToString()
    {
      return Name + " (" + Count + ")";
    }
  }
}