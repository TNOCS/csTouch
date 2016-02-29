using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using Caliburn.Micro;
using csShared.Utils;

namespace csCommon.Plugins.ServicesPlugin
{
    public class ServiceInstance : PropertyChangedBase
    {
        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; NotifyOfPropertyChange(()=>Title); }
        }

        private string folder;

        public string Folder
        {
            get { return folder; }
            set { folder = value; NotifyOfPropertyChange(()=>Folder); NotifyOfPropertyChange(()=>IsAvailable); }
        }

        private string application;

        public string Application
        {
            get { return application; }
            set { application = value; NotifyOfPropertyChange(()=>Application); NotifyOfPropertyChange(()=>IsAvailable); }
        }

        private bool isRunning;

        public bool IsRunning
        {
            get { return isRunning; }
            set { isRunning = value; NotifyOfPropertyChange(() => IsRunning); NotifyOfPropertyChange(() => Status); NotifyOfPropertyChange(()=>StatusColor); }
        }

        private string arguments;

        public string Arguments
        {
            get { return arguments; }
            set { arguments = value; NotifyOfPropertyChange(()=>Arguments); }
        }

        private string processName;

        public string ProcessName
        {
            get { return processName; }
            set { processName = value; NotifyOfPropertyChange(()=>ProcessName); }
        }
        
        
        public bool IsAvailable
        {
            get { return File.Exists(FullFilename); }
            
        }

        public string FullFilename
        {
            get
            {
                return (Folder!=null) ? Path.Combine(Folder, Application) : Application;
            }
        }

        private BindableCollection<string> output = new BindableCollection<string>();

        /// <summary>
        /// List of strings with output text
        /// </summary>
        public BindableCollection<string> Output
        {
            get { return output; }
            set { output = value; NotifyOfPropertyChange(() => Output); }
        }

        private Process process;

        /// <summary>
        /// Start service
        /// </summary>
        public void Start()
        {
            try
            {
                var startInfo              = new ProcessStartInfo {
                    RedirectStandardInput  = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    ErrorDialog            = false,
                    FileName               = FullFilename
                };

                if (string.IsNullOrEmpty(folder)) startInfo.WorkingDirectory = folder;
                startInfo.Arguments = Arguments;

                process = Process.Start(startInfo);
                if (process == null) return;
                process.EnableRaisingEvents = true;
                process.BeginOutputReadLine();
                process.OutputDataReceived += DisplayDataReceived;
                process.ErrorDataReceived  += DisplayDataReceived;
                IsRunning = true;
            }
            catch (Exception e)
            {
                Logger.Log("ServicesPlugin.ServiceInstance", "Cannot start process: " + Path.GetFileNameWithoutExtension(FullFilename), e.Message, Logger.Level.Error, true);
            }
        }

        /// <summary>
        /// Service status text
        /// </summary>
        public string Status
        {
            get
            {
                if (!IsAvailable) return "Not available";
                return (IsRunning) ? "Running" : "Available";               
            }
        }

        /// <summary>
        /// Color of status text
        /// </summary>
        public Brush StatusColor
        {
            get
            {
                if (!IsAvailable) return Brushes.Red;
                return (IsRunning) ? Brushes.Green : Brushes.Blue;
            }
        }

        void DisplayDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null || !IsRunning) return;
            try {
                Output.Add(e.Data);
                if (Output.Count > 300) Output.RemoveAt(0);
            }
            catch (Exception) {
                // This exception is sometimes caught when the process is killed. Ignore it.
            }
        }

        /// <summary>
        /// Stop instance
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            if (process == null) return;
            try
            {
                process.Kill();
            }
            catch (Exception e)
            {
                Logger.Log("Services Plugin","Error stopping service " + Title,e.Message, Logger.Level.Error);

            }
            finally
            {
                process.Close();
            }
        }
    }
}