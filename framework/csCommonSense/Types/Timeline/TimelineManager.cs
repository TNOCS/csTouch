#region

using Caliburn.Micro;
using csEvents;
using csCommon.csMapCustomControls.CircularMenu;
using csShared.Controls.Popups.InputPopup;
using csShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

#endregion

namespace csShared.Timeline
{
    public class TimelineManager : PropertyChangedBase, ITimelineManager
    {
        private static readonly AppStateSettings AppStateSettings = AppStateSettings.Instance;
        private static readonly DateTime PlayStartTimeNotSet      = DateTime.MinValue;
        private static readonly DateTime PlayEndTimeNotSet        = DateTime.MaxValue;

        private static Brush background          = Brushes.White;
        private static Brush currentTimeBrush    = Brushes.Black;
        private static Brush dividerBrush        = Brushes.Black;
        private static Brush focusTimeBackground = Brushes.Red;
        private static Brush focusTimeForeground = Brushes.Black;
        private static Brush foreground          = Brushes.Black;
        private static Brush futureBrush         = Brushes.Gray;
        private readonly DispatcherTimer player  = new DispatcherTimer();

        private readonly Timer updateTimer;
        private bool canChangeFocusTime = true;
        private bool canChangeTimeInterval = true;
        private DateTime currentTime;
        private DateTime end = DateTime.Now;
        private bool eventsVisible;
        private DateTime focusTime;
        private bool focusVisible;
        private bool isPlaying;
        private DateTime lastStartTime;
        private DateTime playEnd = PlayEndTimeNotSet;
        private int playSpeedFactor = 1;
        private TimeSpan playStepSize;
        private DateTime playStart = PlayStartTimeNotSet;
        private BindableCollection<TimeRow> rows = new BindableCollection<TimeRow>();
        private DateTime start = new DateTime(2010, 6, 22);
        private bool timeUpdated;
        private TimelineFixStyles timelineFix = TimelineFixStyles.Custom;
        private ITimelinePlayer timelinePlayer;
        private bool visible;

        public TimelineManager()
        {
            updateTimer = new Timer { Interval = 1000 };
            updateTimer.Elapsed += UpdateTimerElapsed;
            updateTimer.Start();

            var eventAsObservable = Observable.FromEventPattern<TimeEventArgs>(ev => FocusTimeChanged += ev, ev => FocusTimeChanged -= ev);
            eventAsObservable.Throttle(TimeSpan.FromMilliseconds(333)).Subscribe(k => UpdateFocusTime());

            var timeChanged = Observable.FromEventPattern<TimeEventArgs>(ev => TimeChanged += ev, ev => TimeChanged -= ev);
            timeChanged.Sample(TimeSpan.FromMilliseconds(333)).Subscribe(k => OnTimeContentChanged());

            player.Tick += player_Tick;
        }

        public BindableCollection<TimeRow> Rows
        {
            get { return rows; }
            set { rows = value; }
        }

        public bool CanChangeTimeInterval
        {
            get { return canChangeTimeInterval; }
            set
            {
                canChangeTimeInterval = value;
                NotifyOfPropertyChange(() => CanChangeTimeInterval);
            }
        }

        public bool IsPlaying {
            get { return isPlaying; }
            set {
                if (isPlaying == value) return;
                isPlaying = value;
                NotifyOfPropertyChange(() => IsPlaying);
            }
        }

        public event EventHandler<TimeEventArgs> FocusTimeChanged;
        public event EventHandler<TimeEventArgs> FocusTimeUpdated; // Ignore Resharper. Interface requires this to be present.

        //public event EventHandler<TimeEventArgs> FocusTimeSampled;
        public event EventHandler<TimeEventArgs> FocusTimeThrottled; 

        public bool PlayerVisible
        {
            get
            {
                var val = AppStateSettings.Config.GetBool("Timeline.PlayerVisible", false);
                if (val && AppStateSettings.Instance.CircularMenus.All(k => k.Id != "TimePlayer"))
                    CreatePlayerMenu();
                return val;
            }
            set
            {
                AppStateSettings.Config.SetLocalConfig("Timeline.PlayerVisible", value.ToString());
                NotifyOfPropertyChange(() => PlayerVisible);
                if (value)
                    CreatePlayerMenu();
                else
                    AppStateSettings.Instance.RemoveCircularMenu("TimePlayer");
            }
        }

        public double GetScreenPos(DateTime dt)
        {
            var th = (End - Start).TotalSeconds;
            var pos = (Application.Current.MainWindow.ActualWidth / th) * ((dt - Start).TotalSeconds);
            return pos;
        }

        public void CenterTime(DateTime t)
        {
            var r = End - Start;
            Start = t.AddMilliseconds(r.TotalMilliseconds / -2);
            End = t.AddMilliseconds(r.TotalMilliseconds / 2);
            ForceTimeChanged();
        }

        /// <summary>
        /// Show the focus 'cursor'.
        /// </summary>
        public bool FocusVisible
        {
            get { return focusVisible; }
            set
            {
                if (focusVisible == value) return;
                focusVisible = value;
                NotifyOfPropertyChange(() => FocusVisible);
                if (VisibilityChanged != null) VisibilityChanged(this, null);
                AppStateSettings.Config.SetLocalConfig("Timeline.FocusVisible", value.ToString(), true);
            }
        }

        public bool EventsVisible
        {
            get { return eventsVisible; }
            set
            {
                if (eventsVisible == value) return;
                eventsVisible = value;
                NotifyOfPropertyChange(() => EventsVisible);
                if (VisibilityChanged != null) VisibilityChanged(this, null);
                AppStateSettings.Config.SetLocalConfig("Timeline.EventsVisible", value.ToString(), true);
            }
        }

        public bool Visible
        {
            get { return visible; }
            set
            {
                if (visible == value) return;
                visible = value;
                NotifyOfPropertyChange(() => Visible);
                Execute.OnUIThread(() => { if (VisibilityChanged != null) VisibilityChanged(this, null); });
                AppStateSettings.Config.SetLocalConfig("Timeline.Visible", value.ToString(), true);
            }
        }

        public event EventHandler VisibilityChanged;

        public Brush Background
        {
            get { return background; }
            set
            {
                if (Equals(background, value)) return;
                background = value;
                NotifyOfPropertyChange(() => Background);
            }
        }

        public bool CanChangeFocuseTime
        {
            get { return canChangeFocusTime; }
            set
            {
                if (canChangeFocusTime == value) return;
                canChangeFocusTime = value;
                NotifyOfPropertyChange(() => CanChangeFocuseTime);
            }
        }

        /// <summary>
        /// Color used for that part of the time line that occurs in the future.
        /// </summary>
        public Brush FutureBrush
        {
            get { return futureBrush; }
            set
            {
                if (Equals(futureBrush, value)) return;
                futureBrush = value;
                NotifyOfPropertyChange(() => FutureBrush);
            }
        }

        public Brush Foreground
        {
            get { return foreground; }
            set
            {
                if (Equals(foreground, value)) return;
                foreground = value;
                NotifyOfPropertyChange(() => Foreground);
            }
        }

        public Brush FocusTimeBackground
        {
            get { return focusTimeBackground; }
            set
            {
                if (Equals(focusTimeBackground, value)) return;
                focusTimeBackground = value;
                NotifyOfPropertyChange(() => FocusTimeBackground);
            }
        }

        public Brush FocusTimeForeground
        {
            get { return focusTimeForeground; }
            set
            {
                if (Equals(focusTimeForeground, value)) return;
                focusTimeForeground = value;
                NotifyOfPropertyChange(() => FocusTimeForeground);
            }
        }

        public Brush DividerBrush
        {
            get { return dividerBrush; }
            set
            {
                if (Equals(dividerBrush, value)) return;
                dividerBrush = value;
                NotifyOfPropertyChange(() => DividerBrush);
            }
        }

        public Brush CurrentTimeBrush
        {
            get { return currentTimeBrush; }
            set
            {
                if (Equals(currentTimeBrush, value)) return;
                currentTimeBrush = value;
                NotifyOfPropertyChange(() => CurrentTimeBrush);
            }
        }

        public ITimelinePlayer TimelinePlayer
        {
            get { return timelinePlayer; }
            set
            {
                if (timelinePlayer == value) return;
                timelinePlayer = value;
                NotifyOfPropertyChange(() => TimelinePlayer);
            }
        }

        /// <summary>
        /// Start time of the time line.
        /// </summary>
        public DateTime Start
        {
            get { return start; }
            set
            {
                if (start == value) return;
                start = value;
                NotifyOfPropertyChange(() => Start);
            }
        }

        /// <summary>
        /// End time of the time line.
        /// </summary>
        public DateTime End
        {
            get { return end; }
            set
            {
                if (end == value) return;
                end = value;
                NotifyOfPropertyChange(() => End);
            }
        }

        /// <summary>
        /// The position of the cursor on the time line.
        /// NOTE: Instead of setting it directly, consider using the SetFocusTime method.
        /// </summary>
        public DateTime FocusTime
        {
            get { return focusTime; }
            set
            {
                if (focusTime == value) return;
                focusTime = value;
                NotifyOfPropertyChange(() => FocusTime);
                OnFocusTimeChanged();
            }
        }

        private void OnFocusTimeChanged()
        {
            var handler = FocusTimeChanged;
            if (handler != null) handler(this, null);
        }

        public bool HasFocusTimeChanged { get; set; }

        ///// <summary>
        ///// Set the focus time centrally in the view.
        ///// It does not change the current time scale.
        ///// </summary>
        ///// <param name="now"></param>
        //public void SetTimelineFocusTime(DateTime now)
        //{
        //    FocusTime = now;
        //    HasFocusTimeChanged = true;
        //    var timespan = (End - Start).Ticks >> 1;
        //    Start = now.AddTicks(-timespan);
        //    End = now.AddTicks(timespan);
        //    ForceTimeChanged();
        //}

        /// <summary>
        /// Set the focus time.
        /// </summary>
        /// <param name="now"></param>
        public void SetFocusTime(DateTime now)
        {
            FocusTime = now;
            HasFocusTimeChanged = true;
            if (MoveFocusTime)
            {
                // Set the timeline to the entire range and move the focus time from left to right.
                var extent = End - Start;
                if (PlayEnd == PlayEndTimeNotSet) SetPlayEndTime(now.AddTicks(extent.Ticks >> 1));
                if (PlayStart == PlayStartTimeNotSet) SetPlayStartTime(now.AddTicks(-(extent.Ticks >> 1)));
                var timeSpan = (PlayEnd - PlayStart).Ticks / 10;
                Start = PlayStart.AddTicks(-timeSpan);
                End = PlayEnd.AddTicks(timeSpan);
            }
            else
            {
                // Set the focus time central in the view. It does not change the current time scale.
                var timeSpan = (End - Start).Ticks >> 1;
                Start = now.AddTicks(-timeSpan);
                End = now.AddTicks(timeSpan);
            }
            ForceTimeChanged();
        }

        /// <summary>
        /// Start playing at this time.
        /// </summary>
        public DateTime PlayStart
        {
            get { return playStart; }
            set
            {
                if (playStart == value) return;
                playStart = value;
                NotifyOfPropertyChange(() => PlayStart);
            }
        }

        /// <summary>
        /// Stop playing at this time.
        /// By default, this is DateTime.MaxValue, so the player won't stop automatically.
        /// </summary>
        public DateTime PlayEnd
        {
            get { return playEnd; }
            set
            {
                if (playEnd == value) return;
                playEnd = value;
                NotifyOfPropertyChange(() => PlayEnd);
            }
        }

        /// <summary>
        /// The time that the clock moves forward when the PlaySpeedFactor = 1.
        /// </summary>
        public TimeSpan PlayStepSize
        {
            get { return playStepSize; }
            set
            {
                if (playStepSize == value) return;
                playStepSize = value;

                var stepsizetext = ConvertStepToString(PlayStepSize);
                if (stepsizetext == "Size_Custom")
                {
                    var stepmenu = GetMenuItemById("Step");
                    var custom = stepmenu.Items.FirstOrDefault(k => k.Id == "Size_Custom");
                    if (custom != null) custom.Title = "Custom: " + PlayStepSize;
                }


                NotifyOfPropertyChange(() => PlaySpeed);
                NotifyOfPropertyChange(() => PlayStepSize);
            }
        }

        /// <summary>
        /// Default Step for custom time
        /// </summary>
        public TimeSpan DefaultCustomStep = new TimeSpan(0, 1, 0);

        /// <summary>
        /// The time that the clock moves forward.
        /// </summary>
        public TimeSpan PlaySpeed
        {
            get { return TimeSpan.FromTicks(playStepSize.Ticks * playSpeedFactor); }
            set
            {
                if (PlaySpeed == value) return;
                playSpeedFactor = (int)(value.Ticks / playStepSize.Ticks);
                NotifyOfPropertyChange(() => PlaySpeed);
            }
        }



        private TimeSpan playInterval;
        /// <summary>
        /// The time that the clock moves forward when the PlaySpeedFactor = 1.
        /// </summary>
        public TimeSpan PlayInterval
        {
            get { return playInterval; }
            set
            {
                if (playInterval == value) return;
                playInterval = value;
                NotifyOfPropertyChange(() => PlayInterval);
            }
        }

        private bool moveFocusTime;

        /// <summary>
        /// Start time of the time line.
        /// </summary>
        public bool MoveFocusTime
        {
            get { return moveFocusTime; }
            set
            {
                if (moveFocusTime == value) return;
                moveFocusTime = value;
                NotifyOfPropertyChange(() => MoveFocusTime);
            }
        }

        public DateTime CurrentTime
        {
            get { return currentTime; }
            set
            {
                if (currentTime == value) return;
                currentTime = value;
                try
                {
                    NotifyOfPropertyChange(() => CurrentTime);
                }
                catch { }
            }
        }

        public event EventHandler<TimeEventArgs> TimeChanged;
        public event EventHandler<TimeEventArgs> TimeContentChanged;

        
        /// <summary>
        /// Every time change, you get an update.
        /// </summary>
        public void ForceTimeChanged()
        {
            CalculateInterval();
            OnTimeChanged();
            timeUpdated = true;
        }

        private void OnTimeChanged()
        {
            var handler = TimeChanged;
            if (handler != null) handler(this, null);
        }

        /// <summary>
        /// Throttled time change.
        /// </summary>
        public void ForceTimeContentChanged()
        {
            CalculateInterval();
            timeUpdated = true;
            OnTimeContentChanged();
        }

        private void OnTimeContentChanged()
        {
            var handler = TimeContentChanged;
            if (handler != null) handler(this, null);
        }

        public TimelineFixStyles TimelineFix
        {
            get { return timelineFix; }
            set
            {
                if (timelineFix == value) return;
                timelineFix = value;
                NotifyOfPropertyChange(() => TimelineFix);
            }
        }

        public long Interval { get; set; }

        /// <summary>
        /// Start the time line (so the focus time will move forward) in real-time.
        /// </summary>
        /// <remarks>Consider replacing this functionality with the player.</remarks>
        public void Begin()
        {
            updateTimer.Start();
        }

        /// <summary>
        /// Stop the time line (so the focus time will not move forward anymore).
        /// </summary>
        public void Stop()
        {
            if (updateTimer != null && updateTimer.Enabled) updateTimer.Stop();
        }

        private void UpdateFocusTime()
        {
            var handler = FocusTimeThrottled;
            if (handler != null) handler(this, null);
        }

        /// <summary>
        /// Create the circular menu for the player.
        /// </summary>
        public void CreatePlayerMenu()
        {
            var speeds = new List<CircularMenuItem>
            {
                new CircularMenuItem {Id = "Speed_1",  Title = "1X",  Position = 1},
                new CircularMenuItem {Id = "Speed_2",  Title = "2X",  Position = 2},
                new CircularMenuItem {Id = "Speed_4",  Title = "4X",  Position = 3},
                new CircularMenuItem {Id = "Speed_-1", Title = "-1X", Position = 7},
                new CircularMenuItem {Id = "Speed_-2", Title = "-2X", Position = 6},
                new CircularMenuItem {Id = "Speed_-4", Title = "-4X", Position = 5}
            };
            var sizes = new List<CircularMenuItem>
            {
                new CircularMenuItem {Id = "Size_Sec",    Title = "1 sec",   Position = 1},
                new CircularMenuItem {Id = "Size_Min",    Title = "1 min",   Position = 2},
                new CircularMenuItem {Id = "Size_Hr",     Title = "1 hour",  Position = 3},
                new CircularMenuItem {Id = "Size_Day",    Title = "1 day",   Position = 4},
                new CircularMenuItem {Id = "Size_Mnth",   Title = "1 month", Position = 5},
                new CircularMenuItem {Id = "Size_Yr",     Title = "1 year",  Position = 6},
                new CircularMenuItem {Id = "Size_Custom", Title = "Custom",  Position = 7}
            };

            foreach (var mi in speeds)
                mi.ItemSelected += Menu_ItemSelected;
            var playMenuItem = new CircularMenuItem
            {
                Id = "Play",
                Title = "Play",
                Position = 1,
                Icon = "pack://application:,,,/csCommon;component/Resources/Icons/play_black.png",
                Background = Brushes.LightGray
            };

            var setPlayEndTimeMenuItem = new CircularMenuItem
            {
                Id = "SetPlayEndTime",
                Title = "Set end",
                Position = 5,
                Icon = "pack://application:,,,/csCommon;component/Resources/Icons/play_Finish.png",
                Background = Brushes.LightGray
            };
            var setPlayStartTimeMenuItem = new CircularMenuItem
             {
                 Id = "SetPlayStartTime",
                 Title = "Set start",
                 Position = 6,
                 Icon = "pack://application:,,,/csCommon;component/Resources/Icons/play_SetStart.png",
                 Background = Brushes.LightGray
             };
            var speedMenuItem = new CircularMenuItem
            {
                Id = "Speed",
                Title = "Speed 1x",
                Position = 3,
                Icon = "pack://application:,,,/csCommon;component/Resources/Icons/Gauge.png",
                Background = Brushes.LightGray,
                Items = speeds
            };
            var stepsize = new CircularMenuItem
            {
                Id = "Step",
                Title = "Stepsize",
                Position = 4,
                Icon = "pack://application:,,,/csCommon;component/Resources/Icons/Gauge.png",
                Background = Brushes.LightGray,
                Items = sizes
            };
            var rootMenuItems = new List<CircularMenuItem>
            {
                playMenuItem,
                new CircularMenuItem
                {
                    Id = "Stop",
                    Title = "Stop",
                    Position = 2,
                    Icon = "pack://application:,,,/csCommon;component/Resources/Icons/stop_black.png",
                    Background = Brushes.LightGray
                },
                speedMenuItem,
                stepsize,
                setPlayEndTimeMenuItem,
                setPlayStartTimeMenuItem
            };
            var menu = new CircularMenuItem
            {
                Id = "TimePlayer",
                Title = "Player",
                Icon = "pack://application:,,,/csCommon;component/Resources/Icons/play_media.png",
                Background = Brushes.Black,
                Items = rootMenuItems
            };

            foreach (var mi in rootMenuItems)
                mi.ItemSelected += Menu_ItemSelected;
            menu.ItemSelected += Menu_ItemSelected;

            HighlightSelectedItem("Speed_" + playSpeedFactor, speedMenuItem);
            var stepsizetext = ConvertStepToString(PlayStepSize);
            HighlightSelectedItem(stepsizetext, stepsize);
            if (stepsizetext == "Size_Custom")
            {
                var custom = sizes.FirstOrDefault(k => k.Id == "Size_Custom");
                if (custom != null) custom.Title = "Custom: " + PlayStepSize;
            }
            AppStateSettings.Instance.AddCircularMenu(menu);
        }

        public TimeRow GetRow(string id)
        {
            var r = Rows.FirstOrDefault(k => k.Id == id);
            if (r == null)
            {
                r = new TimeRow { Id = id, Order = Rows.Count + 1, Visible = true };
                Rows.Add(r);
            }
            r.ActualOrder = Rows.Count(k => k.Visible && k.Order < r.Order);
            return r;
        }

        public void CalculateInterval()
        {
            var dif = End - Start;
            Interval = Rounding.GetInterval(dif.TotalSeconds);
        }

        private void UpdateTimerElapsed(object sender, ElapsedEventArgs e) {
            CurrentTime = DateTime.Now;
            //if (!timeUpdated || TimeContentChanged == null) return;
            //if (lastStartTime == FocusTime) return;
            ////TimeContentChanged(this, null);
            //timeUpdated = false;
            //lastStartTime = FocusTime;
        }

        /// <summary>
        /// Handle a click on a player menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Menu_ItemSelected(object sender, MenuItemEventArgs e)
        {
            switch (e.SelectedItem.Id)
            {
                case "Play":
                    e.SelectedItem.Background = Brushes.Red;
                    if (isPlaying)
                        PausePlaying();
                    else
                        StartPlaying();
                    break;
                case "Stop":
                    StopPlaying();
                    break;
                case "SetPlayEndTime":
                    SetPlayEndTime();
                    break;
                case "SetPlayStartTime":
                    SetPlayStartTime();
                    break;
                case "Size_Sec":
                    SetStepSize(e.SelectedItem.Id);
                    break;
                case "Size_Min":
                    SetStepSize(e.SelectedItem.Id);
                    break;
                case "Size_Hr":
                    SetStepSize(e.SelectedItem.Id);
                    break;
                case "Size_Day":
                    SetStepSize(e.SelectedItem.Id);
                    break;
                case "Size_Mnth":
                    SetStepSize(e.SelectedItem.Id);
                    break;
                case "Size_Yr":
                    SetStepSize(e.SelectedItem.Id);
                    break;
                case "Size_Custom":
                    CreateCustomStepMenu(e);
                    //SetStepSize(e.SelectedItem.Id);
                    break;
                default:
                    int spd;
                    if (int.TryParse(e.SelectedItem.Id.Replace("Speed_", ""), out spd)) SetPlaySpeed(spd);
                    break;
            }
        }

        private void CreateCustomStepMenu(MenuItemEventArgs fe, bool error = false)
        {
            var inp = new InputPopupViewModel
            {
                Title = error ? "Supply correct format: dd:hh:mm:ss" : "Interval: dd:hh:mm:ss",
                RelativeElement = fe.Menu.Main,
                RelativePosition = new Point(10, 10),
                DefaultValue = DefaultCustomStep.ToString()
            };

            inp.Saved += (o, r) =>
            {
                var stepMenu = GetMenuItemById("Step");
                var custom = stepMenu.Items.FirstOrDefault(k => k.Id == "Size_Custom");
                if (custom == null) return;
                try
                {
                    var timespan = TimeSpan.Parse(r.Result);
                    PlayStepSize = timespan;
                    custom.Title = "Custom: " + timespan;
                    SetStepSize("Size_Custom");
                    fe.Menu.Update();
                }
                catch (Exception)
                {
                    CreateCustomStepMenu(fe, true);
                }
            };
            AppStateSettings.Popups.Add(inp);
        }

        private string ConvertStepToString(TimeSpan step)
        {
            if (step == new TimeSpan(0, 0, 1))
                return "Size_Sec";
            if (step == new TimeSpan(0, 1, 0))
                return "Size_Min";
            if (step == new TimeSpan(1, 0, 0))
                return "Size_Hr";
            if (step == new TimeSpan(1, 0, 0, 0))
                return "Size_Day";
            if (step == new TimeSpan(31, 0, 0, 0))
                return "Size_Mnth";
            return step == new TimeSpan(365, 0, 0, 0) ? "Size_Yr" : "Size_Custom";
        }


        /// <summary>
        /// Set the player's speed, i.e. the multiplication factor that determines how many seconds 
        /// pass every time the focus time is updated.
        /// </summary>
        /// <param name="speed"></param>
        private void SetPlaySpeed(int speed)
        {
            var speedMenu = GetMenuItemById("Speed");
            if (speedMenu == null) return;
            playSpeedFactor = speed;
            speedMenu.Title = string.Format("Speed: {0}x", speed);
            HighlightSelectedItem(string.Format("Speed_{0}", speed), speedMenu);
        }

        private static void HighlightSelectedItem(string speed, CircularMenuItem speedMenu)
        {
            foreach (var i in speedMenu.Items)
            {
                i.Fill = i.Id.Equals(speed)
                    ? Brushes.LightGray
                    : Brushes.White;
            }
        }


        /// <summary>
        /// Set the player's step size, i.e. the amount of time that the focus time is increased each tick.
        /// </summary>
        /// <param name="speed"></param>
        private void SetStepSize(string speed)
        {
            var stepMenu = GetMenuItemById("Step");

            if (stepMenu == null) return;

            switch (speed)
            {
                case "Size_Sec":
                    PlayStepSize = new TimeSpan(0, 0, 1);
                    stepMenu.Title = "Step: sec";
                    break;
                case "Size_Min":
                    PlayStepSize = new TimeSpan(0, 1, 0);
                    stepMenu.Title = "Step: min";
                    break;
                case "Size_Hr":
                    PlayStepSize = new TimeSpan(1, 0, 0);
                    stepMenu.Title = "Step: hour";
                    break;
                case "Size_Day":
                    PlayStepSize = new TimeSpan(1, 0, 0, 0);
                    stepMenu.Title = "Step: day";
                    break;
                case "Size_Mnth":
                    PlayStepSize = new TimeSpan(31, 0, 0, 0);
                    stepMenu.Title = "Step: month";
                    break;
                case "Size_Yr":
                    PlayStepSize = new TimeSpan(365, 0, 0, 0);
                    stepMenu.Title = "Step: year";
                    break;
                case "Size_Custom":
                    var custom = stepMenu.Items.FirstOrDefault(k => k.Id == "Size_Custom");
                    if (custom != null)
                    {
                        var timespan = TimeSpan.Parse(custom.Title.Replace("Custom: ", ""));
                        PlayStepSize = timespan;
                    }
                    stepMenu.Title = "Step: custom";
                    break;
            }

            HighlightSelectedItem(speed, stepMenu);
        }

        private void player_Tick(object sender, EventArgs e)
        {
            var newFocusTime = FocusTime.AddTicks(PlayStepSize.Ticks * playSpeedFactor);
            if (newFocusTime <= PlayEnd)
                SetFocusTime(newFocusTime);
            else
                StopPlaying();
        }

        private void StartPlaying()
        {
            IsPlaying = true;
            if (PlayStart == PlayStartTimeNotSet)
                SetPlayStartTime();
            if (PlayStepSize == new TimeSpan())
                PlayStepSize = TimeSpan.FromSeconds(1);
            if (PlayInterval == new TimeSpan())
                PlayInterval = TimeSpan.FromSeconds(1); // EV in case this isn't 1, you cannot keep the clock in sync. Use speed factor to speed things up...

            player.Interval = PlayInterval;
            player.Start();

            var playMenu = GetMenuItemById("Play");
            if (playMenu == null) return;
            playMenu.Icon = "pack://application:,,,/csCommon;component/Resources/Icons/pause_black.png";
            playMenu.Title = "Pause";
            playMenu.Fill = Brushes.PaleGreen;
        }

        private void PausePlaying()
        {
            IsPlaying = false;
            player.Stop();
            var playMenu = GetMenuItemById("Play");
            if (playMenu == null) return;
            playMenu.Icon = "pack://application:,,,/csCommon;component/Resources/Icons/play_black.png";
            playMenu.Title = "Play";
            playMenu.Fill = Brushes.White;
        }

        private void StopPlaying()
        {
            PausePlaying();
            if (PlayStart != PlayStartTimeNotSet) SetFocusTime(PlayStart);
        }

        private void SetPlayStartTime()
        {
            SetPlayStartTime(focusTime);
        }

        private void SetPlayStartTime(DateTime time)
        {
            var menuItem = GetMenuItemById("SetPlayStartTime");
            if (menuItem == null) return;
            if (PlayStart == PlayStartTimeNotSet)
            {
                PlayStart = time;
                menuItem.Title = playStart.ToString("HH:mm:ss");
            }
            else
            {
                PlayStart = PlayStartTimeNotSet;
                menuItem.Title = "Set start";
            }
        }

        private void SetPlayEndTime()
        {
            SetPlayEndTime(focusTime);
        }

        private void SetPlayEndTime(DateTime time)
        {
            var menuItem = GetMenuItemById("SetPlayEndTime");
            if (menuItem == null) return;
            if (PlayEnd == PlayEndTimeNotSet)
            {
                PlayEnd = time;
                menuItem.Title = playEnd.ToString("HH:mm:ss");
            }
            else
            {
                PlayEnd = PlayEndTimeNotSet;
                menuItem.Title = "Set end";
            }
        }

        /// <summary>
        /// Get the menu item by ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private static CircularMenuItem GetMenuItemById(string id)
        {
            var rootMenu = AppStateSettings.CircularMenus.FirstOrDefault(k => k.Id == "TimePlayer");
            return rootMenu == null
                ? null
                : rootMenu.Items.FirstOrDefault(k => k.Id == id);
        }

    }
}