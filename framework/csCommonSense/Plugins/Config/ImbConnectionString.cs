using System;
using System.Linq;
using System.Net.Sockets;
using Caliburn.Micro;
using csShared.Utils;

namespace csCommon.Plugins.AppStatus
{
    public class ImbConnectionString : PropertyChangedBase
    {
        private string _server;

        public string Server
        {
            get { return _server; }
            set { _server = value; NotifyOfPropertyChange(()=>Server);  }
        }

        private int _port;

        public int Port
        {
            get { return _port; }
            set { _port = value; NotifyOfPropertyChange(()=>Port); }
        }

        public override string ToString()
        {
            return Server + "|" + Port;
        }

        public void FromString(string s)
        {
            try
            {
                string[] ss = s.Split('|');
                if (ss.Count() == 2)
                {
                    Server = ss[0];
                    Port = int.Parse(ss[1]);
                }
            }
            catch (Exception)
            {
                Logger.Log("Connection Definition","Error parsing connection string",s,Logger.Level.Error);                
            }
            
        }

        public void CheckAvailable(int timeout)
        {
            TcpClient FClient = new TcpClient();
            var result = FClient.BeginConnect(Server, Port, null, null);

            bool success = result.AsyncWaitHandle.WaitOne(timeout, true);
            if (success)
            {
                FClient.EndConnect(result);
                FClient.Close();
                Available = true;
            }
            else
            {
                FClient.Close();
                Available = false;
            }
        }

        private bool _available;

        public bool Available
        {
            get { return _available; }
            set { _available = value; NotifyOfPropertyChange(()=>Available); }
        }

        private bool _current;

        public bool Current
        {
            get { return _current; }
            set { _current = value; NotifyOfPropertyChange(()=>Current); }
        }
        
        public bool CanDelete
        {
            get { return !Current; }
        }
        
        
    }
}