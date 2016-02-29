using System;
using Caliburn.Micro;

namespace csShared.Timeline
{
  public class TimeEvent : PropertyChangedBase
  {
    private DateTime _date;

    public DateTime Date
    {
      get { return _date; }
      set { _date = value; }
    }

    private DateTime _startDate;

    public DateTime StartDate
    {
      get { return _startDate; }
      set { _startDate = value; }
    }

    private DateTime _endDate;

    public DateTime EndDate
    {
      get { return _endDate; }
      set { _endDate = value; }
    }

    private string _title;

    public string Title
    {
      get { return _title; }
      set { _title = value; }
    }





    public bool Visible { get; set; }

    public string RowId { get; set; }
  }
}
