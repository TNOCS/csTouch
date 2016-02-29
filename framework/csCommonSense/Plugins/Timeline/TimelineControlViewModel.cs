using Caliburn.Micro;
using csShared;
using csShared.Geo;
using csShared.Interfaces;
using System.ComponentModel.Composition;

namespace csCommon.MapPlugins.Timeline
{

    public interface ITimelineControl
    { }

    [Export(typeof(ITimelineControl))]
    public class TimelineControlViewModel : Screen, ITimelineControl
    {
        public AppStateSettings AppState { get { return AppStateSettings.Instance; } }
        public FloatingElement Element { get; set; }


        public MapViewDef ViewDef
        {
            get
            {
                return AppState.ViewDef;
            }
        }

        private TimelineControlView mtv;

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            mtv = (TimelineControlView)view;
        }

        public void Custom()
        {
            AppState.TimelineManager.TimelineFix = TimelineFixStyles.Custom;
        }

        public void LastMonth()
        {
            AppState.TimelineManager.TimelineFix = TimelineFixStyles.Month;
        }

        public void LastYear()
        {
            AppState.TimelineManager.TimelineFix = TimelineFixStyles.Year;
        }

        public void LastWeek()
        {
            AppState.TimelineManager.TimelineFix = TimelineFixStyles.Week;
        }
        public void LastDay()
        {
            AppState.TimelineManager.TimelineFix = TimelineFixStyles.Day;
        }

        public void LastHour()
        {
            AppState.TimelineManager.TimelineFix = TimelineFixStyles.Hour;
        }

        public void Last15Min()
        {
            AppState.TimelineManager.TimelineFix = TimelineFixStyles.Min15;
        }

        public void Last5Min()
        {
            AppState.TimelineManager.TimelineFix = TimelineFixStyles.Min5;
        }

        public void Last1Min()
        {
            AppState.TimelineManager.TimelineFix = TimelineFixStyles.Min1;
        }

        public TimelineControlViewModel()
        {
            Caption = "Timeline";
        }

        public string Caption { get; set; }
    }
}
