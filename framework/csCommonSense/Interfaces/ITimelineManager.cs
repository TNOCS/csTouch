using System;
using System.Windows.Media;

namespace csShared.Interfaces
{

    public enum TimelineFixStyles
    {
        Custom,
        Year,
        Month,
        Week,
        Day,
        Hour,
        Min15,
        Min5,
        Min1
    }

    public class TimeEventArgs : EventArgs
    {
        
    }

    public interface ITimelineManager
    {
        bool Visible { get; set; }
        bool FocusVisible { get; set; }
        bool PlayerVisible { get; set; }
        bool EventsVisible { get; set; }
        Brush Background { get; set; }
        Brush FutureBrush { get; set; }
        Brush Foreground { get; set; }
        Brush FocusTimeBackground { get; set; }
        Brush FocusTimeForeground { get; set; }
        Brush DividerBrush { get; set; }
        Brush CurrentTimeBrush { get; set; }

        DateTime Start { get; set; }
        DateTime End { get; set; }
        DateTime CurrentTime { get; set; }
        DateTime FocusTime { get; set; }
        DateTime PlayStart { get; set; }
        DateTime PlayEnd { get; set; }
        TimeSpan PlayInterval { get; set; }
        TimeSpan PlayStepSize { get; set; }
        TimeSpan PlaySpeed { get; set; }
        long Interval { get; set; }
        ITimelinePlayer TimelinePlayer { get; set;  }
        bool CanChangeFocuseTime { get; set; }
        double GetScreenPos(DateTime dt);
        TimelineFixStyles TimelineFix { get; set; }
        event EventHandler VisibilityChanged;
        event EventHandler<TimeEventArgs> TimeChanged;
        event EventHandler<TimeEventArgs> TimeContentChanged;
        event EventHandler<TimeEventArgs> FocusTimeChanged;
        event EventHandler<TimeEventArgs> FocusTimeUpdated;
        void ForceTimeChanged();
        void ForceTimeContentChanged();
        void Begin();
        void Stop();
        void SetFocusTime(DateTime now);
        bool HasFocusTimeChanged { get; set; }

        void CenterTime(DateTime dateTime);
    }

    public enum TimelineControlBehavior
    {
        None,
        TimeSpan,
        UntilNow
    }
}
