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
using SessionsEvents;

namespace csUSDomainPlugin
{
    [Export(typeof(IUSLegenda))]
    public class USLegendaViewModel : Screen
    {
        private USLegendaView fView;

        public USDomainPlugin Plugin;

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            fView = (USLegendaView)view;
            fView.Plugin = Plugin;
        }

        public string Name { get { return "USLegenda"; } }

        public string Title
        {
            get
            {
                if (fView != null)
                    return fView.Title;
                else
                    return "";
            }
            set
            {
                if (fView != null)
                    fView.Title = value;
            }
        }

        public SessionPalette Palette
        {
            get
            {
                if (fView != null)
                    return fView.Palette;
                else
                    return null;
            }
            set
            {
                if (fView != null)
                    fView.Palette = value;
            }
        }
    }
}
