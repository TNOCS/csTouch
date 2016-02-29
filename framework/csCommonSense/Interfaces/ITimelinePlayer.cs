using System;

namespace csShared.Interfaces
{
  public interface ITimelinePlayer
  {
    ITimelineManager Timeline { get; set; }
    bool FixTimeline { get; set; }
    bool FixFocus { get; set; }
    void Init();
    void Begin();
    void Stop();

    TimeSpan Backward { get; set; }
    TimeSpan Forward { get; set; }

  }

}
