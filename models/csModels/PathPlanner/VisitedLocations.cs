using System;
using Caliburn.Micro;

namespace csModels.PathPlanner
{
    public class VisitedLocations : BindableCollection<VisitedLocation>
    {
        public DateTime StartTime
        {
            get
            {
                return Count == 0 
                    ? DateTime.MinValue 
                    : this[0].TimeOfVisit;
            }
        }

        public DateTime EndTime
        {
            get
            {
                return Count == 0 
                    ? DateTime.MinValue 
                    : this[Count-1].TimeOfVisit;
            }
        }

        public TimeSpan Duration { get { return EndTime - StartTime; } }

        /// <summary>
        /// Returns true if these locactions are still active, within the current time frame.
        /// </summary>
        /// <param name="currentTime"></param>
        /// <returns></returns>
        public bool IsActive(DateTime currentTime)
        {
            return StartTime <= currentTime && currentTime <= EndTime;
        }
    }
}