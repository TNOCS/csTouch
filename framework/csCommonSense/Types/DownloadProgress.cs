using System;
using Caliburn.Micro;

namespace csShared
{
  public class DownloadProgress : PropertyChangedBase
  {
    public Guid Id { get; set; }
    public String Name { get; set; }
    public double Progress { get; set; }
    public string State { get; set; }
  }
}