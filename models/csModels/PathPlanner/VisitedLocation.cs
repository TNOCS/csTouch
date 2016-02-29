using System;
using System.Collections.Generic;
using System.Globalization;
using Caliburn.Micro;
using DataServer;

namespace csModels.PathPlanner
{
    public class VisitedLocation : PropertyChangedBase
    {
        private const char Separator = '@';
        private Guid id = Guid.NewGuid();
        private string title;
        private DateTime timeOfVisit;
        private double longitude;
        private double latitude;
        private string strokeColor;
        private string transition = "Idle";

        public Guid Id
        {
            get { return id; }
        }

        public DateTime TimeOfVisit
        {
            get { return timeOfVisit; }
            set { timeOfVisit = value; NotifyOfPropertyChange(() => TimeOfVisit); }
        }

        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(() => Title); }
        }

        public double Latitude
        {
            get { return latitude; }
            set { latitude = value; NotifyOfPropertyChange(() => Latitude); }
        }

        public double Longitude
        {
            get { return longitude; }
            set { longitude = value; NotifyOfPropertyChange(() => Longitude); }
        }

        public string StrokeColor
        {
            get { return strokeColor; }
            set { strokeColor = value; NotifyOfPropertyChange(() => StrokeColor); }
        }

        public Position Position
        {
            get { return new Position(longitude, latitude); }
            set
            {
                Longitude = value.Longitude;
                Latitude  = value.Latitude;
            }
        }

        /// <summary>
        /// Specifies the way you want to transition from one location to another.
        /// </summary>
        public string Transition
        {
            get { return transition; }
            set { transition = value; NotifyOfPropertyChange(() => Transition); }
        }

        public List<string> Animations { get; set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{1}{0}{2:yyyy/MM/dd HH:mm:ss}{0}{3:0.000000}{0}{4:0.000000}{0}{5}{0}{6}{0}{7}",
                Separator, Title, timeOfVisit.ToUniversalTime(), longitude, latitude, id, strokeColor, transition);
        }

        public void FromString(string s)
        {
            var split = s.Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length < 7) return; // throw new Exception(string.Format("Expected input: 'title{0}yyyy/MM/dd HH:mm:ss{0}Longitude{0}Latitude{0}Guid{0}Transition'.", Separator));
            Title = split[0];
            DateTime.TryParse(split[1], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out timeOfVisit);
            double.TryParse(split[2], NumberStyles.Number, CultureInfo.InvariantCulture, out longitude);
            double.TryParse(split[3], NumberStyles.Number, CultureInfo.InvariantCulture, out latitude);
            Guid.TryParse(split[4], out id);
            strokeColor = split[5];
            transition = split[6];
        }

        //public void SelectTransition(FrameworkElement el)
        //{
        //    if (Animations == null) return;
        //    var m = GetMenu(el);
        //    foreach (var animation in Animations)
        //        m.AddMenuItem(animation.ToSentenceCase());

        //    //foreach (MovementAnimationMode mode in Enum.GetValues(typeof(MovementAnimationMode)))
        //    //{
        //    //    m.AddMenuItem(mode.ToString().ToSentenceCase());
        //    //}
        //    m.Selected += (s, f) =>
        //    {
        //        var selectedMode = f.Object.ToString();
        //        foreach (var mode in Animations.Where(mode => string.Equals(selectedMode, mode.ToSentenceCase())))
        //            Transition = mode;
        //    };
        //    AppStateSettings.Instance.Popups.Add(m);
        //}

        //private static MenuPopupViewModel GetMenu(FrameworkElement fe)
        //{
        //    var menu = new MenuPopupViewModel
        //    {
        //        RelativeElement = fe,
        //        RelativePosition = new Point(10, 30),
        //        TimeOut = new TimeSpan(0, 0, 0, 15),
        //        VerticalAlignment = VerticalAlignment.Bottom,
        //        DisplayProperty = string.Empty,
        //        AutoClose = true
        //    };
        //    return menu;
        //}

    }
}