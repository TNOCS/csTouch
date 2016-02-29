
using System;
using System.Linq;
using System.Windows;
using csShared.Controls.Popups.MenuPopup;
using csShared.Utils;
using VlcVideoPlayer.ViewModels;
using csShared;

namespace csCommon.Plugins.StreamingVideoPlugin.ViewModels
{
    public class CsVideoStreamPlayerThumbnailViewModel : VideoStreamPlayerThumbnailViewModel
    {
        private class SecondScreenMenuItem
        {
            public SecondScreenMenuItem(string pMenuItem)
            {
                if (!String.IsNullOrWhiteSpace(pMenuItem))
                {
                    var split = pMenuItem.Split(';');

                    if (split.Length == 2)
                    {
                        MenuText = split[1];
                        int screenId = 1;
                        int.TryParse(split[0], out screenId);
                        SecondScreenId = screenId;

                    }
                    else
                    {
                        MenuText = pMenuItem;
                        SecondScreenId = 1;
                    }
                }
                else
                {
                    MenuText = "-";
                    SecondScreenId = 1;
                }
            }

            public string MenuText { get; set; }
            public int SecondScreenId { get; set; }

            public override string ToString()
            {
                return MenuText; // Quick hack for GUI
            }
        }


        public CsVideoStreamPlayerThumbnailViewModel(CsVideoStreamViewModel owner)
            : base(owner)
        {

        }

        public void OpenWindowForVideoStream()
        {
            if (Owner is CsVideoStreamViewModel)
                (Owner as CsVideoStreamViewModel).AvaliableVideoStreamsVM.OpenWindowForVideoStream(this);
        }

        public void SecondScreenMenuOptions(FrameworkElement pSource)
        {
            try
            {



                var menu = new MenuPopupViewModel
                {
                    RelativeElement = pSource,
                    RelativePosition = new System.Windows.Point(35, -5),
                    TimeOut = new System.TimeSpan(0, 0, 0, 5),
                    VerticalAlignment = System.Windows.VerticalAlignment.Bottom,
                    DisplayProperty = string.Empty,
                    AutoClose = true
                };
                var menuItemString = AppStateSettings.Instance.Config.Get("SecondScreens", "");
                if (String.IsNullOrEmpty(menuItemString))
                {
                    (Owner as CsVideoStreamViewModel).AvaliableVideoStreamsVM.OpenInSecondScreen(this, 1);
                }
                else
                {
                    var items = menuItemString.Split('|');
                    if (items.Count() > 1) // Show popup menu so user can select an second screen
                    {
                        foreach (var menuItem in items.Select(x => new SecondScreenMenuItem(x)))
                        {
                            menu.Objects.Add(menuItem);
                        }

                        menu.Selected += (o, args) =>
                        {
                            if (Owner is CsVideoStreamViewModel)
                                (Owner as CsVideoStreamViewModel).AvaliableVideoStreamsVM.OpenInSecondScreen(this,
                                    (args.Object as SecondScreenMenuItem).SecondScreenId);
                        };
                        AppStateSettings.Instance.Popups.Add(menu);
                        // Memory leak?! Should dispose popup menu?
                    }
                    else
                    {
                        if (items.Count() == 1)
                        {
                            var menuitem = new SecondScreenMenuItem(items[0]);
                            if (Owner is CsVideoStreamViewModel)
                                (Owner as CsVideoStreamViewModel).AvaliableVideoStreamsVM.OpenInSecondScreen(this,
                                    menuitem.SecondScreenId);
                        }
                    }
                }
            }
            catch (Exception)
            {


            }
        }


        public bool ShowHeader { get { return true; } }
        public bool CanExit { get { return false; } }
        public bool CanOpenWindow { get { return true; } }
        public bool CanOpenSecondScreen { get { return true; } }

    }
}
