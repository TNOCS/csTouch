using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using csGeoLayers;
using csShared;

namespace csCommon
{
    public partial class ShellView
    {
        private readonly AppStateSettings appState = AppStateSettings.Instance;

        public ShellView()
        {
            InitializeComponent();
            Loaded += MainView_Loaded;
        }

        void MainView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
        //    RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            appState.ScriptCommand += Instance_ScriptCommand;
            InterceptAltF4();
            browser.SuppressScriptErrors(true);
        }

        /// <summary>
        /// Close the application nicely when pressing ALT-F4
        /// </summary>
        [DebuggerStepThrough]        
        private void InterceptAltF4() {
            Application.Current.MainWindow.KeyDown += (o, e) => {
                if (e.Key != Key.System || e.SystemKey != Key.F4) return;
                e.Handled = true;
                appState.CloseApplication();
            };
        }

        void Instance_ScriptCommand(object sender, string command)
        {
            if (!command.StartsWith("web:")) return;
            var web = command.Replace("web:", "");
            gBrowser.Visibility = Visibility.Visible;
            browser.Navigated += browser_Navigated;
            browser.Navigate(new Uri(web));
//            qrCodeGeoControl1.Text = web;
            sbShare.Visibility = Visibility.Visible;
            tbBack.Text = "Close Browser";
        }

        void browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            sbNavigateBack.Visibility = (browser.CanGoBack) ? Visibility.Visible : Visibility.Collapsed;
//            qrCodeGeoControl1.Text = e.Uri.AbsoluteUri;
        }

        private void sbGoBack_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (sbShare.Visibility == Visibility.Collapsed)
            {
                browser.Visibility = Visibility.Visible;
                sbShare.Visibility = Visibility.Visible;
                tbBack.Text = "Close Browser";
            }
            else
            {
                gBrowser.Visibility = Visibility.Collapsed;
            }

        }



        private void sbShare_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            browser.Visibility = Visibility.Collapsed;
            sbShare.Visibility = Visibility.Collapsed;
            tbBack.Text = "Close QR Code";
        }

        private void sbNavigateBack_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (browser.CanGoBack) browser.GoBack();
        }
    }
}
