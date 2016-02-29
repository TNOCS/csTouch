using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using csAppraisalPlugin.Classes;
using csAppraisalPlugin.Views;
using csShared;
using csShared.Controls.Popups.InputPopup;
using csShared.Controls.Popups.MenuPopup;
using System.Windows.Input;
using System.Windows.Controls;
using csShared.Utils;

namespace csAppraisalPlugin.ViewModels
{
    [Export(typeof(IFunctionsTab)), PartCreationPolicy(CreationPolicy.NonShared)]
    public class FunctionsTabViewModel : Screen, IFunctionsTab
    {
        private FunctionsTabView view;

        protected override void OnViewLoaded(object loadedView)
        {
            base.OnViewLoaded(loadedView);
            view = (FunctionsTabView)loadedView;
            Plugin.PropertyChanged += PluginPropertyChanged;
        }

        void PluginPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedAppraisal")
                UpdateFunctions();
        }

        private void UpdateFunctions()
        {
            if (Plugin == null || Plugin.SelectedAppraisal == null) return;
            var l = Plugin.SelectedAppraisal.Criteria.Select(k => k.Id).ToList();
            foreach (var f in Plugin.Functions)
                f.IsSelected = l.Contains(f.Id);
        }

        private AppraisalPlugin plugin;

        public AppraisalPlugin Plugin
        {
            get { return plugin; }
            set
            {
                plugin = value;
                NotifyOfPropertyChange(() => Plugin);
                NotifyOfPropertyChange(() => Functions);
            }
        }
        
        private string bfile;
        public string BFile
        {
            get { return bfile; }
            set { bfile = value; NotifyOfPropertyChange(()=>BFile);}
        }

        public FunctionList Functions
        {
            get
            {
                if (Plugin == null || Plugin.Functions==null) return null;
                return Plugin.Functions;
            }
        }

        public void Compare()
        {
            Plugin.Active = !Plugin.Active;
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

        private readonly List<InputDevice> ignoredDeviceList = new List<InputDevice>();

        public void Selected(SelectionChangedEventArgs e)
        {
            if (Plugin.SelectedAppraisals == null) return;
            if (e.AddedItems != null)
            {
                foreach (var a in Plugin.SelectedAppraisals)
                {
                    foreach (Function f in e.AddedItems)
                    {
                        if (a.Criteria.All(k => k.Id != f.Id))
                            a.Criteria.Add(new Criterion {
                                                             Id = f.Id,
                                                             Title = f.Name,
                                                             AssignedValue = (a.Criteria.Maximum - a.Criteria.Minimum)/2D,
                                                             Weight = 1
                                                         });
                    }
                }
            }
            if (e.RemovedItems == null) return;
            foreach (var a in Plugin.SelectedAppraisals)
            {
                foreach (Function f in e.RemovedItems)
                {
                    var tbr = a.Criteria.FirstOrDefault(k => k.Id == f.Id);
                    if (tbr != null) a.Criteria.Remove(tbr);
                }
            }
        }

        public void Add()
        {
            Functions.Add(new Function("New Function") { IsDefault = true });
        }

        public void AppraisalMenu(ActionExecutionContext context)
        {
            var sb = context.Source as SurfaceButton;
            if (sb == null) return;
            var a = sb.DataContext as Function;
            if (a == null) return;
            var menu = new MenuPopupViewModel
            {
                RelativeElement = sb,
                RelativePosition = new Point(-35, -5),
                TimeOut = new TimeSpan(0, 0, 0, 5),
                VerticalAlignment = VerticalAlignment.Bottom,
                DisplayProperty = "",
                AutoClose = true
            };
            //menu.Point = _view.CreateLayer.TranslatePoint(new Point(0,0),Application.Current.MainWindow);
            
            menu.Objects.Add((a.IsDefault) ? "Remove default" : "Make default");
            menu.Objects.Add("Remove");
            menu.Objects.Add("Duplicate");
            menu.Objects.Add("Rename");
            menu.Selected += (e, s) =>
                                 {
                                     switch (s.Object.ToString())
                                     {
                                         case "Remove default":
                                             a.IsDefault = false;
                                             break;
                                         case "Make default":
                                             a.IsDefault = true;
                                             break;
                                         case "Remove":
                                             Plugin.Functions.Remove(a);
                                             break;
                                         case "Duplicate":
                                             Plugin.Functions.Add(a.Clone());
                                             break;
                                         case "Rename":
                                             var input = new InputPopupViewModel {
                                                      RelativeElement = sb,
                                                      RelativePosition = new Point(-20, -15),
                                                      TimeOut = new TimeSpan(0, 0, 2, 5),
                                                      VerticalAlignment = VerticalAlignment.Bottom,
                                                      Title = "Function Name",
                                                      Width = 250.0,
                                                      DefaultValue = a.Name
                                                  };
                                             input.Saved += (st, ea) =>
                                                 {
                                                     a.Name = ea.Result;
                                                 };
                                             AppStateSettings.Instance.Popups.Add(input);
                                             break;

                                     }
                                 };

            AppStateSettings.Instance.Popups.Add(menu);
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

            IEnumerable<InputDevice> devices = new List<InputDevice>(new[] { device });

            SurfaceDragDrop.BeginDragDrop(view, findSource, cursorVisual, mf, devices, DragDropEffects.Copy);

            // Reset the input device's state.
            InputDeviceHelper.ClearDeviceState(device);
            ignoredDeviceList.Remove(device);
        }

    }
}
