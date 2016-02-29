using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows;
using Caliburn.Micro;
using csShared;
using csShared.Geo;
using System.Windows.Input;
using System.Windows.Controls;
using csShared.Utils;

namespace csUSDomainPlugin
{
    [Export(typeof(IUSDomain))]
    public class USDomainViewModel : Screen
    {
        private USDomainView fView;

        public USDomainPlugin Plugin;

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            fView = (USDomainView)view;
            Plugin.DomainView = fView; // make global to be make image sources available
            fView.Plugin = Plugin;
        }

        public USDomainView View { get { return fView; } }

        public string Name
        {
            get { return "USDomain"; }
        }

    }
}
