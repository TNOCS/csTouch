using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Media;
using csEvents;
using csShared.Controls.Popups.MenuPopup;
using csCommon.Logging;

namespace csShared.Utils
{
    public class Logger
    {
        #region Level enum

        public enum Level
        {
            Trace   = 0,
            Debug   = 1,
            Info    = 2,
            Warning = 3,
            Error   = 4,
            Fatal   = 5
        }

        #endregion

        private static Logger instance;

        private static readonly AppStateSettings AppState = AppStateSettings.Instance;

        private static readonly bool SendToWeb       = AppState.Config.GetBool("Logger.Web", false);
        private static readonly bool SaveToFile      = AppState.Config.GetBool("Logger.File", false);
        private static readonly bool SendToBus       = AppState.Config.GetBool("Logger.Bus", false);
        private static readonly bool WriteToConsole  = AppState.Config.GetBool("Logger.Screen", false);
        private static readonly bool WriteToLogFile  = AppState.Config.GetBool("Logger.LogFile", false);

        private static bool working      = true;
        private static bool statsSending = true;

        private static readonly List<string> List     = new List<string>();
        private static readonly object       Lck      = new object();
        private static readonly object       LckStats = new object();
        private static readonly Guid         Session  = Guid.NewGuid();
        private readonly BackgroundWorker    bw       = new BackgroundWorker();
        private readonly BackgroundWorker    bwstat   = new BackgroundWorker();

        private static readonly Dictionary<string, int> Stats = new Dictionary<string, int>();

        // Possible loglevels : 0 - Log all, 1 - Log errors and warnings only, 2 - log errors only
        private const int LogLevel = -1;

        private Logger() {
            if (AppState.Config.OnlineValues.Count == 0) throw new Exception("Config must be loaded before the logger can be started, as we use its settings!");
            bw.DoWork += BwDoWork;
            bwstat.DoWork += BwstatDoWork;
        }

        ~Logger() {
            bw.Dispose();
            bwstat.Dispose();
        }

        public static Logger Instance {
            get { return GetInstance(); }
        }

        public static Logger GetInstance() {
            return instance ?? (instance = new Logger());
        }

        private static void BwstatDoWork(object sender, DoWorkEventArgs e) {
            var lastStatSend = DateTime.Now;
            var interval = AppState.Config.GetInt("Logger.Stats.Interval", 1000*60);
            while (statsSending) {
                while (statsSending && lastStatSend.AddMilliseconds(interval) > DateTime.Now) Thread.Sleep(10);
                lastStatSend = DateTime.Now;

                if (Stats == null || Stats.Count <= 0) continue;
                try {
                    var r = "stats:";
                    lock (LckStats) {
                        r = Stats.Aggregate(r, (current, s) => current + (s.Key + "=" + s.Value + ";"));
                        Stats.Clear();
                    }
                    if (SendToBus) {
                        if (AppState.Imb != null && AppState.Imb.IsConnected) {
                            AppState.Imb.SendMessage(AppState.Imb.Id + ".log", r);
                        }
                    }
                }
                catch (Exception) {
                    Log("Logger", "Error logging stats", "", Level.Error);
                }
            }
        }

        public void StartWork() {
            if (!AppState.Config.GetBool("Logger.Enabled", false)) return;
            if (!bw.IsBusy) bw.RunWorkerAsync();
            if (!bwstat.IsBusy) bwstat.RunWorkerAsync();
            bw.WorkerSupportsCancellation = true;
            bwstat.WorkerSupportsCancellation = true;
        }

        public void StopWork() {
            // todo send last
            working = false;
            statsSending = false;
            if (AppState.Config.GetBool("Logger.Enabled", false)) SendData();
            if (bw != null && bw.IsBusy) bw.CancelAsync();
            if (bwstat != null && bwstat.IsBusy) bwstat.CancelAsync();
        }

        private static void BwDoWork(object sender, DoWorkEventArgs e) {
            while (working) {
                Thread.Sleep(AppState.Config.GetInt("Logger.Interval", 5000));
                SendData();
            }
        }

        private static void SendData() {
            var sendlist = new List<string>();
            lock (Lck) {
                if (List.Count > 0) {
                    sendlist.AddRange(List);
                    List.Clear();
                }
            }

            var result = sendlist.Aggregate("", (current, v) => current + (v + "\r\n"));
            try {
                if (String.IsNullOrEmpty(result)) return;
                try {
                    if (SaveToFile)
                        using (var log = GetWriter()) {
                            log.WriteLine(result);
                            log.Close();
                        }
                }
                catch (Exception) {
                    Log("Logger", "Error logging to file", "", Level.Error);
                }


                try {
                    if (!SendToWeb) return;
                    var wc = new WebClient();
                    wc.UploadString(
                        AppState.Config.Get("Logger.Web.Url",
                            "http://134.221.210.43/LoginService/Log.aspx"), result);
                }
                catch (Exception) {
                    Log("Logger", "Error logging to web", "", Level.Error);
                }
            }
            catch (Exception er) {
                Console.WriteLine(er.ToString());
                lock (Lck) {
                    foreach (var v in sendlist)
                        List.Add(v);
                }
            }
        }

        public static void Stat(string key, int count = 1) {
            lock (LckStats) {
                if (!Stats.ContainsKey(key)) {
                    Stats[key] = count;
                }
                else {
                    Stats[key] += count;
                }
            }
        }

        /// <summary>
        /// When logging a message, you can also show it in the GUI by setting showInGUI=true.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="message"></param>
        /// <param name="content"></param>
        /// <param name="level"></param>
        /// <param name="showInGUI"></param>
        /// <param name="showInEventLog"></param>
        public static void Log(string source, string message, string content, Level level, bool showInGUI = false, bool showInEventLog = false) {
            LogCs.LogMessage(message ?? "" + " " + content ?? "");
            if (((int) level) < LogLevel) return;
            var s = string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}", 
                AppState.Config.UserName, AppState.Config.ApplicationName, level, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), source, message, content, AppState.Config.UserId, Session);

            //var s2 = AppState.Config.UserName + "|" +
            //        AppState.Config.ApplicationName + "|" + level + "|" +
            //        DateTime.UtcNow.ToString(CultureInfo.InvariantCulture) +
            //        "|" + source + "|" + message + "|" + content + "|" + AppState.Config.UserId +
            //        "|" + Session;
            //Log to logging service?
            lock (Lck) {
                List.Add(s);
            }

            try {
                if (SendToBus && (AppState.Imb != null && AppState.Imb.IsConnected))
                    AppState.Imb.SendMessage(AppState.Imb.Id + ".log", s);
            }
            catch (Exception) {
                Log("Logger", "Error logging to bus", "", Level.Error);
            }

            try {
                if (WriteToConsole) Console.WriteLine(s);
            }
            catch (Exception) {
                Log("Logger", "Error logging to screen", "", Level.Error);
            }

            try {
                if (WriteToLogFile) {
                    lock (Lck) {
                        using (var logFile = File.AppendText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log.csv")))
                        {
                            logFile.WriteLine(s.Replace('|', ';'));
                        }
                    }
                }
            }
            catch (Exception e) {
                Log("Logger", "Error logging to screen: " + e.Message, "", Level.Error);
            }

            //Console.WriteLine(prefix + DateTime.UtcNow + " : " + message);
            if (showInEventLog) {
                AppState.EventList.Add(new EventBase {
                    Category       = source,
                    Name           = message,
                    Description    = content,
                    Source         = source,
                    ShowOnTimeline = true,
                    ShowInList     = true,
                    Icon           = ConvertToImageUri(level),
                    State          = ConvertToEventState(level),
                    Date           = AppState.TimelineManager.CurrentTime
                });
            }

            if (!showInGUI) return;
            switch (level) {
                default:
                    AppState.TriggerNotification(message, content, AppState.AccentBrush);
                    //Execute.OnUIThread(() => AppState.TriggerNotification(message, content, AppState.AccentBrush));
                    break;
                case Level.Error:
                    AppState.TriggerNotification(source + ": " + message, content, Brushes.DarkRed, pathData: MenuHelpers.ErrorIcon);
                    //Execute.OnUIThread(() => AppState.TriggerNotification(source + ": " + message, content, Brushes.DarkRed, pathData: MenuHelpers.ErrorIcon));
                    break;
            }
        }

        private static EventState ConvertToEventState(Level level) {
            switch (level) {
                case Level.Error:   return EventState.red;
                case Level.Warning: return EventState.orange;
                case Level.Info:    return EventState.green;
                default:            return EventState.grey;
            }
        }

        private static string ConvertToImageUri(Level level) {
            switch (level) {
                case Level.Error:   return "pack://application:,,,/csCommon;component/Resources/Icons/Wrong.png";
                case Level.Warning: return "pack://application:,,,/csCommon;component/Resources/Icons/flag.png";
                //case Level.Info:    return "pack://application:,,,/csCommon;component/Resources/Icons/Message.png";
                default:            return "pack://application:,,,/csCommon;component/Resources/Icons/Message.png";
            }
        }

        private static StreamWriter GetWriter() {
            try {
                var filename = Path.Combine(AppStateSettings.CacheFolder, "logfile.txt"); // REVIEW TODO: Used Path instead of String concat.
                var log = !System.IO.File.Exists(filename)
                    ? new StreamWriter(filename)
                    : System.IO.File.AppendText(filename);
                return log;
            }
            catch {
                Console.WriteLine("Error while opening Logfile.");
            }
            return null;
        }
    }
}