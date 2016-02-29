using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MenuPopup;
using csShared.Utils;
using Microsoft.Surface.Presentation.Controls;
using PowerPointGenerator;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;

namespace csCommon.Plugins.NotebookPlugin
{
    public class NoteTabViewModel : Screen
    {
        public ScreenshotPlugin Plugin { get; set; }

        public Notebook ActiveNotebook
        {
            get
            {
                if (Plugin.Notebooks.ActiveNotebook==null) Plugin.Notebooks.ActiveNotebook = new Notebook();
                var activeNotebook = Plugin.Notebooks.ActiveNotebook;
                activeNotebook.Items.CollectionChanged -= ActiveNotebookChanged;
                activeNotebook.Items.CollectionChanged += ActiveNotebookChanged;
                return activeNotebook;
            }
        }

        public string AvailableNotebooks
        {
            get { return string.Join(",", Plugin.Notebooks.Select(k => k.Name)); }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            Plugin.Notebooks.CollectionChanged += (e, f) => NotifyOfPropertyChange(() => AvailableNotebooks);
            NotifyOfPropertyChange(() => CanCreatePowerPoint);
        }

        public void ItemMenu(ActionExecutionContext context)
        {
            var sb = context.Source as SurfaceButton;
            if (sb == null) return;
            var a = sb.DataContext as NotebookItem;
            if (a == null) return;
            var menu = new MenuPopupViewModel
            {
                RelativeElement = sb,
                RelativePosition = new Point(-35, -5),
                TimeOut = new TimeSpan(0, 0, 0, 5),
                VerticalAlignment = VerticalAlignment.Bottom,
                DisplayProperty = string.Empty,
                AutoClose = true
            };
            //menu.Point = _view.CreateLayer.TranslatePoint(new Point(0,0),Application.Current.MainWindow);

            menu.Objects.Add("Remove");

            menu.Selected += (e, s) =>
            {
                switch (s.Object.ToString())
                {
                    case "Remove":
                        try
                        {
                            if (ActiveNotebook.Items.Contains(a)) ActiveNotebook.Items.Remove(a);
                            if (File.Exists(a.FileName))
                            {
                                File.Delete(a.FileName);
                                //ThreadPool.QueueUserWorkItem(delegate {
                                //    Thread.Sleep(1000); File.Delete(a.FileName);                                    
                                //});
                            }
                        }
                        catch (Exception ex)
                        {
                            // Added ex.Message to logger.
                            Logger.Log("Notes", "Try to delete it next time (after restarting).", "Error deleting notebook item from disk!\n" + ex.Message, Logger.Level.Error, true);
                        }

                        NotifyOfPropertyChange(() => CanCreatePowerPoint);
                        break;
                }
            };

            AppStateSettings.Instance.Popups.Add(menu);
        }

        public void NotebookChanged()
        {
            Plugin.Notebooks.ActiveNotebook = Plugin.Notebooks.FirstOrDefault(k => k.Name == Plugin.Notebooks.ActiveNotebookConfig);
            NotifyOfPropertyChange(() => CanCreatePowerPoint);
        }

        private void ActiveNotebookChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyOfPropertyChange(() => CanCreatePowerPoint);
        }

        public bool CanCreatePowerPoint { get { return (ActiveNotebook != null && ActiveNotebook.Items != null && ActiveNotebook.Items.Any(n => n.IsSelected)); } }

        public void CreatePowerPoint()
        {
            if (!CanCreatePowerPoint)
            {
                Logger.Log("NotebookPlugin", string.Format("Error creating PowerPoint of {0} Notebook", ActiveNotebook.Name), "Please select some images!", Logger.Level.Error, true);
                return;
            }
            var imagePaths = ActiveNotebook.Items.Where(notebookItem => notebookItem.IsSelected).Select(notebookItem => notebookItem.FileName).ToList();
            var pptFactory = new PowerPointFactory(Path.ChangeExtension(Path.Combine(Path.GetDirectoryName(imagePaths[0]), ActiveNotebook.Name), "pptx"));
            pptFactory.CreateTitleAndImageSlides(imagePaths);
            AppStateSettings.Instance.TriggerNotification(string.Format("Finished creating PowerPoint of {0} Notebook", ActiveNotebook.Name));
        }
    }
}