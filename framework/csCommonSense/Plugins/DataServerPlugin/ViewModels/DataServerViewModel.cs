using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using csShared;
using csShared.Interfaces;
using csShared.Utils;

namespace csCommon
{
    using System.ComponentModel.Composition;
    using System.Windows.Threading;



    [Export(typeof (IPluginScreen))]
    public class DataServerViewModel : Screen, IPluginScreen
    {

        private DispatcherTimer timer;

        private string hash;

        public string Hash
        {
            get { return hash; }
            set { hash = value; NotifyOfPropertyChange(()=>Hash); }
        }
        

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0,0,0,3);
            timer.Tick += timer_Tick;
            //timer.Start();

        }

        void timer_Tick(object sender, EventArgs e)
        {
            var act  = AppStateSettings.Instance.DataServer.ActiveService;
            if (act != null)
            {
                var s = string.Concat(act.PoIs.Select(k => k.ToXmlBase(act.Settings).ToString()));
                var b = Application.Current.MainWindow.Title + " - " + s;

                var c = s.Length;
                Hash = "types:" + act.PoITypes.Count + ", pois:" + act.PoIs.Count + ", labels:" +
                       c;

                //var s = AppStateSettings.Instance.DataServer.ActiveService.ToXml().ToString();
                //string hash;
                //using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                //{
                //    Hash = BitConverter.ToString(
                //      md5.ComputeHash(Encoding.UTF8.GetBytes(s))
                //    ).Replace("-", String.Empty);
                //}
            }
        }


        public string Name
        {
            get { return string.Empty; }
        }
    }
}
