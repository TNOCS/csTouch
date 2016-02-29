using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using Caliburn.Micro;
using csShared;
using csShared.Documents;
using csShared.FloatingElements;
using csShared.Interfaces;
using csShared.TabItems;
using csShared.Utils;
using S22.Imap;

namespace csPresenterPlugin
{
    [Export(typeof (IPlugin))]
    public class ReceiveMailPlugin : PropertyChangedBase, IPlugin
    {
        private bool hideFromSettings;
        private IPluginScreen screen;
        private ISettingsScreen settings;

        public bool CanStop
        {
            get { return true; }
        }

        public ISettingsScreen Settings
        {
            get { return settings; }
            set
            {
                settings = value;
                NotifyOfPropertyChange(() => Settings);
            }
        }


        public IPluginScreen Screen
        {
            get { return screen; }
            set
            {
                screen = value;
                NotifyOfPropertyChange(() => Screen);
            }
        }

        public bool HideFromSettings
        {
            get { return hideFromSettings; }
            set
            {
                hideFromSettings = value;
                NotifyOfPropertyChange(() => HideFromSettings);
            }
        }

        public int Priority
        {
            get { return 6; }
        }

        public string Icon
        {
            get { return @"icons\filewatcher.png"; }
        }

        #region IPlugin Members

        private ImapClient Client;
        private bool isRunning;
        public StartPanelTabItem st { get; set; }

        public string Name
        {
            get { return "ReceiveMailPlugin"; }
        }

        public bool IsRunning
        {
            get { return isRunning; }
            set
            {
                isRunning = value;
                NotifyOfPropertyChange(() => IsRunning);
            }
        }

        private Thread mailThread;

        public AppStateSettings AppState { get; set; }

        public void Init()
        {
        }

        public void ReceiveMail()
        {
            try
            {
                string username = AppState.Config.Get("ReceiveMailPlugin.Username", "presenter@arnoud.org");
                string password = AppState.Config.Get("ReceiveMailPlugin.Password", "tnopresenter");
                string server = AppState.Config.Get("ReceiveMailPlugin.Server", "imap.gmail.com");
                int port = AppState.Config.GetInt("ReceiveMailPlugin.Port", 993);
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) return;
                Client = new ImapClient(server, port, username, password, AuthMethod.Login, true);
                
                // Should ensure IDLE is actually supported by the server
                if (Client.Supports("IDLE") == false)
                {
                    Logger.Log("ReceiveMailPlugin", "Server does not support IMAP IDLE", "", Logger.Level.Info);
                }
                else
                {
                    // We want to be informed when new messages arrive
                    Client.NewMessage += OnNewMessage;
                }
            }
            catch (Exception exception)
            {
                Logger.Log("ReceiveMailPlugin", "Error initializing imap server, check settings", exception.Message,
                    Logger.Level.Error);
            }
        }

        public void Start()
        {
            mailThread = new Thread(ReceiveMail);
            mailThread.Start();
            
            

            IsRunning = true;
        }


        public void Pause()
        {
        }

        public void Stop()
        {
            try
            {
                if (Client != null)
                {
                    Client.NewMessage -= OnNewMessage;
                    Client.Logout();
                }

                mailThread.Abort();
            }
            catch (Exception exception)
            {
                Logger.Log("ReceiveMailPlugin","Error stopping plugin", exception.Message,Logger.Level.Error);
            }
            finally
            {
                IsRunning = false;
            }
           
            
        }

        private void OnNewMessage(object sender, IdleMessageEventArgs e)
        {
            string mailFolder = Directory.GetCurrentDirectory() + "\\" +
                                AppStateSettings.Instance.Config.Get("ReceiveMailPlugin.Folder", "\\Presenter\\");
            if (!mailFolder.EndsWith("\\")) mailFolder += "\\";
            if (!Directory.Exists(mailFolder)) Directory.CreateDirectory(mailFolder);
            if (!Directory.Exists(mailFolder)) return;

            // Fetch the new message's headers and print the subject line
            MailMessage m = e.Client.GetMessage(e.MessageUID, FetchOptions.Normal);
            if (m.Attachments.Any())
            {
                foreach (Attachment a in m.Attachments)
                {
                    try
                    {
                        byte[] bytes = default(byte[]);
                        using (var memstream = new MemoryStream())
                        {
                            a.ContentStream.CopyTo(memstream);
                            bytes = memstream.ToArray();
                        }
                        string f = mailFolder + DateTime.Now.Ticks + "-" + a.Name;
                        File.WriteAllBytes(f, bytes);

                        if (AppState.Config.GetBool("ReceiveMailPlugin.ShowDirectly", false))
                        {
                            var fe = FloatingHelpers.CreateFloatingElement(new Document() {Location = f, OriginalUrl = f});
                            AppState.FloatingItems.AddFloatingElement(fe);
                        }

                        Execute.OnUIThread(
                            () =>
                            {
                                NotificationEventArgs nea =
                                    AppState.TriggerNotification("New file received from " + m.From.Address);
                            });
                    }
                    catch (Exception exception)
                    {                        
                        Logger.Log("ReceiveMailPlugin","Error save incoming message", exception.Message,Logger.Level.Error);
                    }
                    
                }
            }
        }

        #endregion
    }
}