using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using csShared;
using csShared.Controls.Popups.MapCallOut;
using csShared.Controls.Popups.MenuPopup;
using csShared.Documents;
using DataServer;

namespace csDataServerPlugin.ViewModels
{
    [Export(typeof(IEditableScreen))]
    public class MediaViewModel : Screen, IEditableScreen
    {
        public MetaLabel Label;

        private BindableCollection<Document> documents = new BindableCollection<Document>();

        public BindableCollection<Document> Documents
        {
            get { return documents; }
            set { documents = value; }
        }
        
        
        private bool canEdit;

        public bool CanEdit {
            get { return false; }
            set {
                
                if (canEdit == value) return;
                canEdit = value;
                NotifyOfPropertyChange(() => CanEdit);
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            if (!string.IsNullOrEmpty(Label.Data))
            {
                var p = Path.Combine(Label.PoI.Service.MediaFolder, Label.Data);
                if (Directory.Exists(p))
                {
                    var ff = Directory.GetFiles(p);
                    foreach (var f in ff) Documents.Add(new Document() { Location = f});
                }
            }
        }

        public MapCallOutViewModel CallOut { get; set; }

        
       

    }

    
}
