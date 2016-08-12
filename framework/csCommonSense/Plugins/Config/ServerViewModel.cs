﻿using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using Caliburn.Micro;
using System;
using csCommon.Plugins.AppStatus;
using csShared;
using csShared.Utils;
using IMB3;
using csCommon.Imb;

namespace csCommon.Plugins.Config
{

    

    [Export(typeof(IScreen))]
    public class ServerViewModel : Conductor<Screen>.Collection.OneActive
    {

        private string port;

        private string server;
        private string name;
        
        public AppStateSettings AppState
        {
            get { return AppStateSettings.GetInstance(); }
            set { }
        }


        private BindableCollection<ImbConnectionString> connections = new BindableCollection<ImbConnectionString>();


        public string Name
        {
            get { return AppState.Config.Get("FullName", ""); }
            set
            {
                if (value == null || value == AppState.Config.Get("FullName", "")) return;
                name = value;
                AppState.Config.SetLocalConfig("FullName", value, false);
            }
        }

        public BindableCollection<ImbConnectionString> Connections
        {
            get { return connections; }
            set
            {
                connections = value;
                NotifyOfPropertyChange(() => Connections);
            }
        }

        public string Server
        {
            get { return server; }
            set
            {
                server = value;
                NotifyOfPropertyChange(() => Server);
            }
        }

        public string Port
        {
            get { return port; }
            set
            {
                port = value;
                NotifyOfPropertyChange(() => Port);
            }
        }

        public void SaveUser()
        {
            //AppState.Config.SetLocalConfig("FullName", AppState.Imb.Status.n, false);
            AppState.Config.SetLocalConfig("FullName", name, true);
            AppState.InitImb();
        }

        public void Connect(ImbConnectionString ics)
        {
            AppState.Config.SetLocalConfig(csImbConfig.CfgNameImbHost, ics.Server, false);
            AppState.Config.SetLocalConfig(csImbConfig.CfgNameImbPort, ics.Port.ToString(CultureInfo.InvariantCulture), true);
            AppState.InitImb();
            UpdateStatus();
        }

        public void Remove(ImbConnectionString ics)
        {
            if (ics.Current) return;
            if (!Connections.Contains(ics)) return;
            Connections.Remove(ics);
            SaveConnections();
            UpdateStatus();
        }

        public void SaveConnections()
        {
            var result = Connections.Aggregate(string.Empty, (current, c) => current + (c + ","));
            result = result.Trim(',');
            AppState.Config.SetLocalConfig("ImbHistory", result, true);
        }

        public void UpdateStatus()
        {
            
            if (AppState.Imb.Imb == null) return;
            if (!AppState.Config.GetBool(csImbConfig.CfgNameImbIsEnabled, true)) return;
            Connections = new BindableCollection<ImbConnectionString>();
            var hs = AppState.Config.Get("ImbHistory", "");
            var hh = hs.Split(',');

            foreach (var h in hh)
            {
                if (string.IsNullOrEmpty(h)) continue;
                var ics = new ImbConnectionString();
                ics.FromString(h);
                ics.Current = (AppState.Imb.Imb.RemoteHost == ics.Server && AppState.Imb.Imb.RemotePort == ics.Port);
                Connections.Add(ics);
            }
            if (!Connections.Any(k => k.Current))
            {
                if (AppState.Imb != null && AppState.Imb.IsConnected)
                {
                    var ics = new ImbConnectionString
                    {
                        Server = AppState.Imb.Imb.RemoteHost,
                        Port = AppState.Imb.Imb.RemotePort,
                        Current = true
                    };
                    Connections.Add(ics);
                    SaveConnections();
                }
            }
            //if (AppState.Imb != null && !AppState.Imb.IsConnected)
            {
                var aServer = string.Empty;
                var aPort = 4000;
                if (IMBlocator.DecodeServerURI(IMBlocator.LocateServerURI(), ref aServer, ref aPort))
                {
                    var ics =
                        new ImbConnectionString
                        {
                            Server = aServer,
                            Port = aPort,
                            Available = true
                        };
                    if (!Connections.Any(k => k.Server == ics.Server && k.Port == ics.Port))
                        Connections.Add(ics);
                }
            }
            ThreadPool.QueueUserWorkItem(delegate
            {
                lock (Connections)
                {
                    try
                    {
                        foreach (var c in Connections) c.CheckAvailable(500);
                    }
                    catch (Exception e)
                    {
                        Logger.Log("AppStatusViewModel", "UpdateStatus", e.Message, Logger.Level.Error);
                    }
                }
            });

            //ConnectionStatus+="\nMy IP:" + AppState.Imb.Imb.
        }

        private string username;

        public string Username
        {
            get { return username; }
            set { username = value; NotifyOfPropertyChange(()=>Username); }
        }
        

        public void ChangeUsername()
        {
            Name = Username;
            AppState.TriggerNotification("Username changed. You might need to restart the application");
        }


        public void AddServer()
        {
            if (string.IsNullOrEmpty(Server) || string.IsNullOrEmpty(Port)) return;
            if (Connections.Any(k => k.Server.ToLower() == Server.ToLower() && k.Port == int.Parse(port)))
            {
                AppState.TriggerNotification("Server configuration already exists");
                return;
            }
            try
            {
                var ics = new ImbConnectionString { Server = Server, Port = int.Parse(Port) };
                Connections.Add(ics);
                SaveConnections();
                UpdateStatus();
            }
            catch (Exception e)
            {
                Logger.Log("AppStatusViewModel", "AddServer", e.Message, Logger.Level.Error);
            }
        }

        private readonly BackgroundWorker bw = new BackgroundWorker();

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            bw.DoWork += bw_DoWork;
            bw.RunWorkerAsync();
            Username = Name;

        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {            
            UpdateStatus();
        }
        


    }


}

