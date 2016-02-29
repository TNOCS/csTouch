using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using Microsoft.Surface.Presentation.Controls;

namespace csShared.Controls.TwoWaySliding
{


  public interface ITwoWay
  {
  }


  public class SelectableItem: PropertyChangedBase
  {
    private string _name;
    public string Name
    {
      get { return _name; }
      set { _name = value;NotifyOfPropertyChange(()=>Name); }
    }
    private bool _selected;
    public bool Selected
    {
      get { return _selected; }
      set { _selected = value; NotifyOfPropertyChange(() => Selected); }
    }
    private string _fullName;
    public string FullName
    {
      get { return _fullName; }
      set { _fullName = value; NotifyOfPropertyChange(() => FullName); }
    }
  }

  public class singleSlide : PropertyChangedBase
  {
    

    private double _colWidthHeader;

    public double ColWidthHeader
    {
      get { return _colWidthHeader; }
      set { _colWidthHeader = value; NotifyOfPropertyChange(() => ColWidthHeader); }
    }

    private double _colWidth;

    public double ColWidth
    {
      get { return _colWidth; }
      set { _colWidth = value; NotifyOfPropertyChange(() => ColWidth); }
    }

    private double _colHeight;

    public double ColHeight
    {
      get { return _colHeight; }
      set { _colHeight = value; NotifyOfPropertyChange(() => ColHeight); }
    }

    private Brush _backColor;

    public Brush BackColor
    {
      get { return _backColor; }
      set { _backColor = value; NotifyOfPropertyChange(() => BackColor); }
    }

    private string _category;

    public string Category
    {
      get { return _category; }
      set
      {
        _category = value;
        if (_category == "")
        {
          this.BackColor = Brushes.White;
        }
        else
        {
          this.BackColor = Brushes.Gray;
        }
        NotifyOfPropertyChange(() => Category);
      }
    }

    private SelectableItem _selectedItem;
    public SelectableItem SelectedItem
    {
      get { return _selectedItem; }
      set { _selectedItem = value;NotifyOfPropertyChange(()=>SelectedItem); }
    }

    private SelectableItem _removedItem;
    public SelectableItem RemovedItem
    {
      get { return _removedItem; }
      set { _removedItem = value; NotifyOfPropertyChange(() => RemovedItem); }
    }

    private BindableCollection<SelectableItem> _items = new BindableCollection<SelectableItem>();

    public BindableCollection<SelectableItem> Items
    {
      get { return _items; }
      set { _items = value; NotifyOfPropertyChange(() => Items); }
    }

  }
  /// <summary>
  /// Interaction logic for twowaysliding.xaml
  /// </summary>
  [Export(typeof(ITwoWay))]
  public class TwoWaySlidingViewModel : Screen
  {
    private TwoWaySlidingView _viewModel = null;

    private BindableCollection<singleSlide> _collection = new BindableCollection<singleSlide>();

    public BindableCollection<singleSlide> CollectionList
    {
      get { return _collection; }
      set { _collection = value; NotifyOfPropertyChange(() => CollectionList); }
    }

    private BindableCollection<singleSlide> _collectionHeader = new BindableCollection<singleSlide>();

    public BindableCollection<singleSlide> HeaderList
    {
      get
      {
        /*BindableCollection<singleSlide> tempSlide = new BindableCollection<singleSlide>();
        tempSlide.Add(new singleSlide() { Category = "", ColWidthHeader = CollectionList[0].ColWidth / 2 });
        foreach(singleSlide ss in _collection)
        {
          tempSlide.Add(ss);
        }
        tempSlide.Add(new singleSlide() { Category = "", ColWidthHeader = CollectionList[0].ColWidth / 2 });
        */
        return _collectionHeader;
      }
      set { _collectionHeader = value; NotifyOfPropertyChange(() => HeaderList); }
    }

    // Using a DependencyProperty as the backing store for SensorId. This enables animation, styling, binding, etc...
    //public static readonly DependencyProperty CollectionProperty =
    //  DependencyProperty.Register("Collection", typeof(int), typeof(DikeSensorView), new UIPropertyMetadata(0));



    public void TWSSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
      if (args.AddedItems.Count > 0)
      {
        foreach (object selIt in args.AddedItems)
        {
          TwoWaySliding.SelectableItem sit = selIt as TwoWaySliding.SelectableItem;
          var slb = sender as singleSlide;
          if (slb !=null)
            slb.SelectedItem= sit;
          var rem = new List<SelectableItem>();
          
        }
      }
    }



    public TwoWaySlidingViewModel()
    {
      bool firstCol = true;
      int firstElem = 1;
      int lastElem = 12;
      int colWidth = 150;

      var firstElement = new singleSlide() { Category = "", ColWidth = colWidth * 0.5, ColWidthHeader = colWidth * 0.25 };
      HeaderList.Add(firstElement);
      for (int cnt = firstElem; cnt <= lastElem; cnt++)
      {
        string colName = new DateTime(2011, cnt, 1).ToString("MMMM");
        var element = new singleSlide() { Category = colName, ColWidth = colWidth };
        element.ColWidthHeader = element.ColWidth * 0.5;
        string itemName = "first";
        if (!firstCol)
          itemName = "second";
        for (int cnt2 = 1; cnt2 < 6; cnt2++)
        {
          element.Items.Add(new SelectableItem() { Selected = false, Name = itemName + cnt2});
        }
        CollectionList.Add(element);
        HeaderList.Add(element);
      }
      var lastElement = new singleSlide() { Category = "", ColWidth = colWidth * 0.5, ColWidthHeader = colWidth * 0.25 };
      HeaderList.Add(lastElement);
    }

    protected override void OnViewLoaded(object view)
    {
      _viewModel = (TwoWaySlidingView) view;
      base.OnViewLoaded(view);
      var svi = (ScatterViewItem)BaseWPFHelpers.Helpers.FindElementOfTypeUp(_viewModel, new ScatterViewItem().GetType());
      if (svi != null)
      {
        svi.MinHeight = 160;
        svi.MinWidth = 160;
      }
    }

    public void ScrollToLastVerPos(string selItem)
    {
      if (_viewModel != null)
        _viewModel.selectedItemString = selItem;
    }

    public void TEnableParameter(object sender, TouchEventArgs e)
    {
      var t = e.Source as TextBlock;
      if (t != null)
      {
        var v = sender as SelectableItem;
        if (v != null)
          v.Selected = !v.Selected;
        //foreach (var pp in PParameterList.Where(k => k.Name == t.Text))
        //{
        //  pp.Selected = !pp.Selected;
        //}
      }
      e.Handled = true;
    }

    public void MEnableParameter(object sender, MouseEventArgs e)
    {
      var t = e.Source as TextBlock;
      if (t != null)
      {
        var v = sender as SelectableItem;
        if (v != null)
          v.Selected = !v.Selected;
      }
      //e.Handled = true;
    }
  }
}
