using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows;
using Caliburn.Micro;
//using csSurface;
using csShared.Geo;
using System.Windows.Input;
using System.Windows.Controls;
using csShared.Utils;
using SessionsEvents;

namespace csUSDomainPlugin
{
    [Export(typeof(IUSDrawingMode))]
    public class USDrawingModeViewModel : Screen
    {
        private USDrawingModeView fView;

        public USDomainPlugin Plugin;

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            fView = (USDrawingModeView)view;
            fView.Plugin = Plugin;
        }

        public string Name { get { return "USDrawingMode"; } }

        
    }
}
