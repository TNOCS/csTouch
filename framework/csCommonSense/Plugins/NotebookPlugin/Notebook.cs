using Caliburn.Micro;

namespace csCommon.Plugins.NotebookPlugin
{
    public class NotebookItem : PropertyChangedBase
    {
        private string fileName;

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; NotifyOfPropertyChange(()=>FileName); }
        }

        private bool isSelected = true;

        public bool IsSelected
        {
            get { return isSelected; }
            set { isSelected = value; NotifyOfPropertyChange(()=>IsSelected); }
        }
    }

    public class Notebook : PropertyChangedBase
    {
        private bool isActive;

        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; NotifyOfPropertyChange(()=>IsActive); }
        }

        private bool available;

        public bool Available
        {
            get { return available; }
            set { available = value; NotifyOfPropertyChange(()=>Available); }
        }

        private string name;

        public string Name
        {
            get { return name; }
            set { name = value; NotifyOfPropertyChange(()=>Name);  }
        }

        private string folder;

        public string Folder
        {
            get { return folder; }
            set { folder = value; NotifyOfPropertyChange(()=>Folder); }
        }

        private readonly BindableCollection<NotebookItem> items = new BindableCollection<NotebookItem>();

        public BindableCollection<NotebookItem> Items
        {
            get { return items; }
        }

        public void LoadItems()
        {
            Items.Clear();
            if (!System.IO.Directory.Exists(Folder)) return;
            var ff = System.IO.Directory.GetFiles(Folder, "*.png");
            foreach (var f in ff)
            {
                var nbi = new NotebookItem
                {
                    FileName = f
                };
                Items.Add(nbi);
            }
        }
    }
}