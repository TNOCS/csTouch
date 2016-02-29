using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using csShared.Controls.Popups.ColorInputPopup;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using csAppraisalPlugin.Classes;
using csAppraisalPlugin.Views;
using csShared;
using csShared.Controls.Popups.InputPopup;
using csShared.Controls.Popups.MenuPopup;
using csShared.Utils;
using PowerPointGenerator;

namespace csAppraisalPlugin.ViewModels
{
    [Export(typeof (IAppraisalTab)), PartCreationPolicy(CreationPolicy.NonShared)]
    public class AppraisalTabViewModel : Screen, IAppraisalTab
    {
        private readonly List<InputDevice> ignoredDeviceList = new List<InputDevice>();
        private string bfile;

        private AppraisalPlugin plugin;
        private AppraisalTabView view;

        public string BFile
        {
            get { return bfile; }
            set
            {
                bfile = value;
                NotifyOfPropertyChange(() => BFile);
            }
        }

        public AppraisalList Appraisals
        {
            get
            {
                if (Plugin == null || Plugin.Appraisals == null) return null;
                return Plugin.Appraisals;
            }
        }

        #region IAppraisalTab Members

        public AppraisalPlugin Plugin
        {
            get { return plugin; }
            set
            {
                plugin = value;
                NotifyOfPropertyChange(() => Plugin);
                NotifyOfPropertyChange(() => Appraisals);
            }
        }

        #endregion

        protected override void OnViewLoaded(object loadedView)
        {
            base.OnViewLoaded(loadedView);
            view = (AppraisalTabView) loadedView;
        }

        public void Compare()
        {
            Plugin.Active = !Plugin.Active;
        }

        public void Export()
        {
            Plugin.Export();
        }

        public void StartDrag(object sender, MouseEventArgs e)
        {
            if (e == null) return;
            StartDragDrop(sender, e.Device, e.OriginalSource);
            e.Handled = true;
        }

        public void StartTouchDrag(object sender, TouchEventArgs e)
        {
            if (e == null) return;
            StartDragDrop(sender, e.Device, e.OriginalSource);
            e.Handled = true;
        }


        public void Selected()
        {
            foreach (var a in Plugin.Appraisals.Where(k => k.IsSelected))
            {
                if (!Plugin.SelectedAppraisals.Contains(a)) Plugin.SelectedAppraisals.Add(a);
            }
            for (var i=Plugin.SelectedAppraisals.Count - 1; i>=0; i--)
            {
                if (Plugin.SelectedAppraisals[i].IsSelected) continue;
                Plugin.SelectedAppraisals.RemoveAt(i);
            }
            //var tbr = Plugin.Appraisals.Where(k => !k.IsSelected && Plugin.SelectedAppraisals.Contains(k));
            //foreach (var a in tbr) Plugin.SelectedAppraisals.Remove(a);
        }

        public void AppraisalMenu(ActionExecutionContext context)
        {
            var sb = context.Source as SurfaceButton;
            if (sb == null) return;
            var appraisal = sb.DataContext as Appraisal;
            if (appraisal == null) return;
            var menu = new MenuPopupViewModel {
                                                  RelativeElement = sb,
                                                  RelativePosition = new Point(-35, -5),
                                                  TimeOut = new TimeSpan(0, 0, 0, 5),
                                                  VerticalAlignment = VerticalAlignment.Bottom,
                                                  DisplayProperty = ""
                
                              };
            
            //menu.Point = _view.CreateLayer.TranslatePoint(new Point(0,0),Application.Current.MainWindow);
            menu.Objects.Add((appraisal.IsCompare) ? "Stop Compare" : "Compare");
            menu.Objects.Add("Remove");
            menu.Objects.Add("Duplicate");
            menu.Objects.Add("Rename");
            menu.Objects.Add("Select Color");
            menu.Selected += (e, s) =>
                                 {
                                     switch (s.Object.ToString())
                                     {
                                         case "Stop Compare":
                                             appraisal.IsCompare = false;
                                             Plugin.TriggerAppraisalsUpdated();
                                             break;
                                         case "Compare":
                                             appraisal.IsCompare = true;
                                             Plugin.TriggerAppraisalsUpdated();
                                             break;
                                         case "Remove":
                                             Plugin.Appraisals.Remove(appraisal);
                                             if (Plugin.SelectedAppraisals.Contains(appraisal)) Plugin.SelectedAppraisals.Remove(appraisal);
                                             break;
                                         case "Duplicate":
                                             Plugin.Appraisals.Add(appraisal.Clone() as Appraisal);
                                             break;
                                         case "Select Color":
                                             var cinput = new ColorInputPopupViewModel
                                             {
                                                 RelativeElement = sb,
                                                 RelativePosition = new Point(-20, -15),
                                                 TimeOut = new TimeSpan(0, 0, 2, 5),
                                                 VerticalAlignment = VerticalAlignment.Bottom,
                                                 Title = "Appraisal Title",
                                                 Width = 250.0,
                                                 DefaultValueB = appraisal.Color.B.ToString(),
                                                 DefaultValueR = appraisal.Color.R.ToString(),
                                                 DefaultValueG = appraisal.Color.G.ToString()

                                             };
                                             cinput.Saved += (st, ea) =>
                                             {
                                                 appraisal.Color = ea.Result.Color;
                                                 Plugin.TriggerAppraisalsUpdated();
                                                 //appraisal.Title = ea.Result;
                                                 // TODO Should we change the filename too (so the copied screenshot filenames make sense to humans)
                                             };
                                             AppStateSettings.Instance.Popups.Add(cinput);
                                             break;
                                         case "Rename":
                                             var input = new InputPopupViewModel
                                             {
                                                 RelativeElement = sb,
                                                 RelativePosition = new Point(-20, -15),
                                                 TimeOut = new TimeSpan(0, 0, 2, 5),
                                                 VerticalAlignment = VerticalAlignment.Bottom,
                                                 Title = "Appraisal Title",
                                                 Width = 250.0,
                                                 DefaultValue = appraisal.Title
                                             };
                                             input.Saved += (st, ea) =>
                                             {
                                                 appraisal.Title = ea.Result;
                                                 // TODO Should we change the filename too (so the copied screenshot filenames make sense to humans)
                                             };
                                             AppStateSettings.Instance.Popups.Add(input);
                                             break;
                                     }
                                 };

            AppStateSettings.Instance.Popups.Add(menu);
        }

        public void NewBlankAppraisal()
        {
            var appraisal = new Appraisal("New appraisal", string.Empty);
            if (Plugin != null && Plugin.Appraisals != null) Plugin.Appraisals.Add(appraisal);
        }

       

        public void StartDragDrop(object sender, InputDevice device, object src)
        {
            var mf = sender;

            // Check whether the input device is in the ignore list.
            if (ignoredDeviceList.Contains(device)) return;

            InputDeviceHelper.InitializeDeviceState(device);

            // try to start drag-and-drop,
            // verify that the cursor the contact was placed at is a ListBoxItem
            //                DependencyObject downSource = sender as DependencyObject;
            //                FrameworkElement parrent = mf.Parent as FrameworkElement;
            //                var source = GetVisualAncestor<FrameworkElement>(downSource);

            var findSource = src as FrameworkElement;

            //var parrent = GetVisualAncestor<FrameworkElement>(downSource);

            //                var cursorVisual = new ucDocument(){Document = new Document(){Location = mf.Location}};
            //                var cursorVisual = new ListFeatureView() { CanBeDragged = false };
            var cursorVisual = new Border {
                                              Background = Brushes.Black,
                                              DataContext = mf,
                                              Width = 30,
                                              Height = 30
                                          };


            IEnumerable<InputDevice> devices = new List<InputDevice>(new[] {device});

            SurfaceDragDrop.BeginDragDrop(view, findSource, cursorVisual, mf, devices, DragDropEffects.Copy);

            // Reset the input device's state.
            InputDeviceHelper.ClearDeviceState(device);
            ignoredDeviceList.Remove(device);
        }
    }
}