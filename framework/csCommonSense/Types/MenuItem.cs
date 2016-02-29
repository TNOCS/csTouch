using System;
using System.Collections.Generic;
using Caliburn.Micro;

namespace csShared
{
  public class MenuItem : PropertyChangedBase
  {
    private string _name;

    public string Name
    {
      get { return _name; }
      set { _name = value; NotifyOfPropertyChange(()=>Name); }
    }

    private string _icon;

    public string Icon
    {
      get { return _icon; }
      set { _icon = value; }
    }

    private List<string> commands = new List<string>();

    public List<string> Commands
    {
        get { return commands; }
        set { commands = value; }
    }
    

    public event EventHandler Clicked;

    public void ForceClick()
    {
      if (Clicked != null) Clicked(this, null);
    }

    public event EventHandler HideTabPanel;

    public void ForceHideTabPanel()
    {
      if (HideTabPanel != null) HideTabPanel(this, null);
    }

  }
}