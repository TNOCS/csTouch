using System.ComponentModel.Composition;
using System.Linq;
using Caliburn.Micro;
using csImb;
using csShared;


namespace csCommon.RemoteScreenPlugin
{
    [Export(typeof (IScreen))]
    public class ContactsViewModel : Screen
    {
        public static AppStateSettings AppState { get { return AppStateSettings.Instance; } }

        private ContactsView cv;

        private csRemoteScreenPlugin.RemoteScreenPlugin plugin;

        public csRemoteScreenPlugin.RemoteScreenPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }

        private string newGroupName = string.Empty;

        public string NewGroupName
        {
            get { return newGroupName; }
            set { newGroupName = value; NotifyOfPropertyChange(()=>NewGroupName); NotifyOfPropertyChange(()=>CanCreateGroup); }
        }

        public bool CanCreateGroup
        {
            get
            {
                return (!string.IsNullOrEmpty(NewGroupName.Trim()) 
                    && AppState.Imb.Groups.All(k => k.Name.ToLower() != NewGroupName.ToLower().Trim()));
            }
        }

        protected override void OnViewLoaded(object view)
        {
            cv = (ContactsView) view;
            base.OnViewLoaded(view);
            AppState.Imb.ClientChanged                -= Imb_ClientChanged;
            AppState.Imb.ClientChanged                += Imb_ClientChanged;
            AppState.Imb.Groups.CollectionChanged     -= Groups_CollectionChanged;
            AppState.Imb.Groups.CollectionChanged     += Groups_CollectionChanged;
            AppState.Imb.AllClients.CollectionChanged -= Clients_CollectionChanged;
            AppState.Imb.AllClients.CollectionChanged += Clients_CollectionChanged;
            AppState.Imb.PropertyChanged              -= Imb_PropertyChanged;
            AppState.Imb.PropertyChanged              += Imb_PropertyChanged;
            UpdateClients();
        }

        void Imb_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActiveGroup") UpdateClients();
        }

        void Groups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateClients();
        }

        void Clients_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateClients();
        }

        public void CreateGroup()
        {
            if (!CanCreateGroup) return;
            var ng = new csGroup {Name = NewGroupName.Trim()};
            AppState.Imb.CreateGroup(ng);
            //AppState.Imb.JoinGroup(ng);
            NewGroupName = string.Empty;
            SelectedTab = 0;
        }
       
        public void UpdateClients()
        {
            var r = new BindableCollection<IScreen>();
            foreach (var c in AppState.Imb.Groups) r.Add(new GroupViewModel { Group = c, Plugin = plugin });

            foreach (var c in AppState.Imb.AllClients) r.Add(new ContactViewModel { Client = c, Plugin = plugin});

            cv.Clients.ItemsSource = Clients = r;
        }

        private int selectedTab;

        public int SelectedTab
        {
            get { return selectedTab; }
            set { selectedTab = value; NotifyOfPropertyChange(()=>SelectedTab); }
        }
        
        void Imb_ClientChanged(object sender, ImbClientStatus e)
        {
            Execute.OnUIThread(UpdateClients);
        }

        private BindableCollection<IScreen> clients;

        public BindableCollection<IScreen> Clients
        {
            get { return clients; }
            set { clients = value; NotifyOfPropertyChange(()=>Clients); }
        }
    }
}