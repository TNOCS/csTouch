#region

using System.IO;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csImb;
using csShared;
using csShared.Controls.Popups.InputPopup;
using csShared.Controls.Popups.MenuPopup;
using csShared.Utils;
using DataServer;
using Microsoft.Surface.Presentation.Controls;
using PoiServer.PoI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

#endregion

namespace csDataServerPlugin
{

    public class Backup : PropertyChangedBase
    {
        

        private DateTime date;

        public DateTime Date
        {
            get { return date; }
            set { date = value; NotifyOfPropertyChange(()=>Date); }
        }

        private string fileName;

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }
        
        
    }



    public class BackupTabViewModel : Screen
    {

        public AppStateSettings AppState => AppStateSettings.Instance;

        private PoiService service;

        public PoiService Service
        {
            get { return service; }
            set { service = value; NotifyOfPropertyChange(() => Service); }
        }

        public void CreateBackup()
        {
            if (Service.CreateBackup())
            {
                Refresh();
                AppState.TriggerNotification("Backup has been stored");
            }
            else
            {
                AppState.TriggerNotification("Error creating backup");
            }
        }

        private BindableCollection<Backup> backupHistory = new BindableCollection<Backup>();

        public BindableCollection<Backup> BackupHistory
        {
            get { return backupHistory; }
            set { backupHistory = value; NotifyOfPropertyChange(()=>BackupHistory); }
        }

        

        public string BackupInterval
        {
            get
            {
                return GetIntervalText(Service.Settings.BackupInterval);
            }
            
        }

        private string GetIntervalText(int minutes)
        {
            if (minutes == 0) return "Manually";
            return minutes + " minutes";
        }

        public int[] IntervalOptions = new[] {0, 1, 5, 10, 15, 30, 60, 90, 120, 240};

        

        private MenuPopupViewModel GetMenu(FrameworkElement fe)
        {
            var menu = new MenuPopupViewModel
            {
                RelativeElement = fe,
                RelativePosition = new System.Windows.Point(10, 30),
                TimeOut = new TimeSpan(0, 0, 0, 15),
                VerticalAlignment = VerticalAlignment.Top,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            return menu;
        }

        public void SelectInterval(FrameworkElement fe)
        {
            var m = GetMenu(fe);
            foreach (var i in IntervalOptions)
            {
                var mi = m.AddMenuItem(GetIntervalText(i));
                mi.Click += (e, f) =>
                {                    
                    Service.Settings.BackupInterval = i;
                    NotifyOfPropertyChange(()=>BackupInterval);
                    Service.InitBackupInterval();
                };
                m.Items.Add(mi);
            }
            AppState.Popups.Add(m);
        }

        public void Restore(Backup b)
        {
            var nea = new NotificationEventArgs
            {
                Text = "Are you sure?",
                Header = "Restoring this backup will discard all changes in current version",
                Duration = new TimeSpan(0, 0, 30),
                Background = Brushes.Red,
                PathData = MenuHelpers.BackupIcon,
                Foreground = Brushes.White,
                Options = new List<string> { "Yes", "No" }
            };
            nea.OptionClicked += (s, n) =>
            {
                if (n.Option != "Yes") return;
                Service.Layer.Stop();
                File.Copy(b.FileName,Service.FileName,true);                
                AppState.TriggerNotification("Backup was restored. You will need to start the layer manually");
                
            };
            AppState.TriggerNotification(nea);
            
        }

        public new void Refresh() // FIXME TODO "new" keyword missing?
        {            
            BackupHistory.Clear();
            BackupHistory.AddRange(Service.GetBackups());            
        }

        protected override void OnViewLoaded(object view)
        {
            Refresh();
        }
    }
}