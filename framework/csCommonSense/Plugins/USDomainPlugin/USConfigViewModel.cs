using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows;
using Caliburn.Micro;
//using csSurface;
//using csSurface.Geo;
using System.Windows.Input;
using System.Windows.Controls;
//using csSurface.Utils;

namespace csUSDomainPlugin
{
    [Export(typeof(IUSConfig))]
    public class USConfigViewModel:Screen
    {
        private USConfigView fView;

        public USDomainPlugin Plugin;

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            fView = (USConfigView)view;
            fView.Plugin = Plugin;
        }

        public string Name { get { return "USConfig"; } }

        // IMB connection parameters
        //public string USClientIMBHostText { get; set; }
        //public string USClientIMBPortText { get; set; }
        //public string USClientIMBFederationText { get; set; }


    }
}
