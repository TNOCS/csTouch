using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Diagnostics;
using IMB3;
using IMB3.ByteBuffers;
using SessionsEvents;
using Microsoft.Surface.Presentation.Controls;
//using csSurface;
using csShared;

namespace csUSDomainPlugin
{
    /// <summary>
    /// Interaction logic for USConfigView.xaml
    /// </summary>
    public partial class USConfigView : UserControl
    {
        
        
        public USConfigView()
        {
            InitializeComponent();

            USClientIMBHostText.Text = AppStateSettings.Instance.Config.Get("IMB.Host", USDomainPlugin.DefaultIMBRemoteHost);
            USClientIMBPortText.Text = AppStateSettings.Instance.Config.Get("IMB.Port", USDomainPlugin.DefaultIMBRemotePort.ToString());
            USClientIMBFederationText.Text = AppStateSettings.Instance.Config.Get("IMB.IdleFederation", USDomainPlugin.DefaultIMBFederation);
        }

        private USDomainPlugin fPlugin;
        public USDomainPlugin Plugin
        {
            get { return fPlugin; }
            set
            {
                fPlugin = value;
                fPlugin.Connection.OnDisconnect += new TConnection.TOnDisconnect(connection_UpdateConnectionStatus);
                UpdateConnectionStatus();
            }
        }

        public int IMBRemotePort { get { try { return Convert.ToInt32(USClientIMBPortText.Text); } catch { return 4000; } } }
        public string IMBRemoteHost { get { return USClientIMBHostText.Text; } }
        public string IMBFederation { get { return USClientIMBFederationText.Text; } }

        private void connection_UpdateConnectionStatus(TConnection aConnection)
        {
            Dispatcher.BeginInvoke(new USDomainPlugin.TUpdateConnectionStatus(UpdateConnectionStatus), null);
        }

        void UpdateConnectionStatus()
        {
            if (Plugin.Connection.Connected)
            {
                IMBConnectionStatusLabel.Content = "connected";
                IMBConnectionStatusLabel.Foreground = IMBConnectionLabel.Foreground.Clone();
            }
            else
            {
                IMBConnectionStatusLabel.Content = "NOT connected";
                IMBConnectionStatusLabel.Foreground = Brushes.DarkRed;
            }
        }
        
        private void USClientIMBConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!Plugin.IMBConnected)
            {
                if (Plugin.IMBOpen(IMBRemoteHost, IMBRemotePort, IMBFederation))
                {
                    Plugin.Connection.OnDisconnect += new TConnection.TOnDisconnect(connection_UpdateConnectionStatus);
                    // store settings
                    AppStateSettings.Instance.Config.SetLocalConfig("IMB.Host", USClientIMBHostText.Text); 
                    AppStateSettings.Instance.Config.SetLocalConfig("IMB.Port", USClientIMBPortText.Text);
                    AppStateSettings.Instance.Config.SetLocalConfig("IMB.IdleFederation", USClientIMBFederationText.Text);
                }
                UpdateConnectionStatus();
            }
        }

        private void USClientIMBDisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin.IMBConnected)
            {
                Plugin.IMBClose();
                UpdateConnectionStatus();
            }
        }
    }
}
