using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using ExcelServiceClient.Model;
using ExcelServiceClient.Properties;
using ExcelServiceModel;
using Microsoft.AspNet.SignalR.Client;

namespace ExcelServiceClient.ViewModels
{
    public enum ConnectionType{Rest, SignalR};

    public class ShellViewModel : Screen
    {
        private readonly Regex _cellNameRegex = new Regex(@"^('?[^']+'?|[^!]+)![A-Z]+[0-9]+$");

        private HttpClient _client;
        private HubConnection _hubConnection;
        private IHubProxy _hubProxy;

        protected BindableCollection<WatchedItem> _watchedItems = new BindableCollection<WatchedItem>();

        public BindableCollection<WatchedItem> WatchedItems
        {
            get { return _watchedItems; }
        } 

        public IEnumerable<WatchedItem> SessionWatchedItems
        {
            get
            {
                if (SelectedSession == null) return null;
                return _watchedItems.Where(wi => wi.SessionId == SelectedSession.Id);
;            }
        } 

        protected void SetWatchedItemValue(object item)
        {
            int sessionId = -1;
            string itemName = null, itemValue = null;

            if (item is CellValue)
            {
                var cellValue = (CellValue) item;
                sessionId = cellValue.SessionId;
                itemName = string.Format("{0}!{1}", cellValue.Worksheet, cellValue.CellName);
                itemValue = cellValue.Value.ToString();
            }
            else if (item is NameValue)
            {
                var nameValue = (NameValue) item;
                sessionId = nameValue.SessionId;
                itemName = nameValue.Name;
                itemValue = nameValue.Values != null ? string.Join(", ", nameValue.Values.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))) : "";
            }


            var existingItem = _watchedItems.FirstOrDefault(wi => wi.SessionId == sessionId && wi.ItemName == itemName);
            if (existingItem != null)
            {
                existingItem.ItemValue = itemValue;
            }
            else
            {
                var newItem = new WatchedItem() {SessionId = sessionId, ItemName = itemName, ItemValue = itemValue};
                _watchedItems.Add(newItem);
                NotifyOfPropertyChange(()=>SessionWatchedItems);
            }
        }

        protected override void OnActivate()
        {
            base.OnInitialize();

            _client = new HttpClient();
            _client.BaseAddress = new Uri("http://localhost:8080");
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Add(Settings.Default.UsernameHeader, Settings.Default.Username);

            _hubConnection = new HubConnection("http://localhost:8080/");
            _hubConnection.Headers.Add(Settings.Default.UsernameHeader, Settings.Default.Username);

            _hubProxy = _hubConnection.CreateHubProxy("ExcelHub");
            _hubProxy.On<CellValue>("CellValueUpdated", (x) =>
                {
                    Console.WriteLine("Workbook {0}, sheet {1}, cell {2} value modified: {3}", x.Workbook, x.Worksheet, x.CellName, x.Value);
                    Execute.OnUIThread(() => SetWatchedItemValue(x));
                });
            _hubProxy.On<NameValue>("NameValueUpdated", x =>
                {
                    Console.WriteLine("Workbook {0}, name {1} value modified: {2}", x.Workbook, x.Name, x.Value);
                    Execute.OnUIThread(() => SetWatchedItemValue(x));
                });

            try
            {
                _hubConnection.Start().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error setting up SignalR hub connection: {0}", ex.Message);
            }
        }

        protected override void OnDeactivate(bool close)
        {
            _client.Dispose();
            _client = null;

            _hubConnection.Stop();
            _hubConnection.Dispose();
            _hubConnection = null;

 	         base.OnDeactivate(close);
        }

        public async Task GetSessions()
        {
            SelectedSession = null;
            Sessions.Clear();

            var response = await _client.GetAsync("excel/sessions");
            if (response.IsSuccessStatusCode)
            {
                var sessions = await response.Content.ReadAsAsync<IEnumerable<ExcelSession>>();
                Console.WriteLine("{0} sessions:", sessions.Count());
                foreach (var excelSession in sessions)
                {
                    Sessions.Add(excelSession);
                    Console.WriteLine("{0}: {1} created on {2}, currently owned by {3}", excelSession.Id,
                                        excelSession.Name, excelSession.Created, excelSession.Owner);
                }
            }
        }

        public async Task JoinSession(int id)
        {
            Console.WriteLine("Joining session {0}", id);
            var response = await _client.GetAsync(string.Format("excel/sessions/{0}/join", id));
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Session {0} joined", id);
            }
            else
            {
                Console.WriteLine("Could not join session {0}", id);
            }
        }

        public async Task GetWorksheets()
        {
            _worksheets.Clear();
            if (SelectedSession == null) return;

            var response = await _client.GetAsync(string.Format("excel/sessions/{0}/worksheets", SelectedSession.Id));
            if (response.IsSuccessStatusCode)
            {
                var worksheetNames = await response.Content.ReadAsAsync<IEnumerable<string>>();
                foreach (var worksheetName in worksheetNames)
                {
                    _worksheets.Add(worksheetName);
                }
            }
            
        }

        public async Task GetNames()
        {
            _names.Clear();
            if (SelectedSession == null) return;

            var response = await _client.GetAsync(string.Format("excel/sessions/{0}/names", SelectedSession.Id));
            if (response.IsSuccessStatusCode)
            {
                var names = await response.Content.ReadAsAsync<IEnumerable<string>>();
                foreach (var name in names)
                {
                    _names.Add(name);
                }
            }

            NotifyOfPropertyChange(() => Names);
        }

        public async Task CreateSession()
        {
            var createSessionVm = new CreateSessionViewModel();

            var wm = IoC.Get<IWindowManager>();
            var dialogResult = wm.ShowDialog(createSessionVm, null, null);
            if (!dialogResult.HasValue || !dialogResult.Value) return;

            var session = createSessionVm.Session; //new ExcelSession() {Name = "CareToolSession", WorkbookName = "CareToolBoekwaarde.xlsx", WatchAllFormulaCells = false, WatchAllNames = true};
            var response = await _client.PostAsJsonAsync("excel/sessions", session);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Session created");
                GetSessions();
            }
            else
            {
                Console.WriteLine("Error creating session");
            }
        }

        public async Task GetCellValue()
        {
            if (_selectedSession == null) return;

            string strValue = null;
            switch (ConnectionType)
            {
                case ConnectionType.Rest:
                    var response = await _client.GetAsync(string.Format("excel/sessions/{0}/{1}/{2}", _selectedSession.Id, Worksheet, Cell));
                    if (response.IsSuccessStatusCode)
                    {
                        var value = await response.Content.ReadAsAsync<object>();
                        strValue = Convert.ToString(value, CultureInfo.InvariantCulture);
                    }
                    break;
                case ConnectionType.SignalR:
                    var cellValue = new CellValue(_selectedSession.Id, null, Worksheet, Cell, null);
                    cellValue = await _hubProxy.Invoke<CellValue>("GetCellValue", cellValue);
                    strValue = Convert.ToString(cellValue.Value, CultureInfo.InvariantCulture);
                    break;
            }

            CellValue = strValue;
        }

        public async Task SetCellValue()
        {
            if (_selectedSession == null) return;

            double dValue;
            if (!double.TryParse(CellValue, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out dValue)) return;

            switch (ConnectionType)
            {
                case ConnectionType.Rest:
                    var response =
                        await
                        _client.PutAsJsonAsync(
                            string.Format("excel/sessions/{0}/{1}/{2}", _selectedSession.Id, Worksheet, Cell), dValue);
                    break;
                case ConnectionType.SignalR:
                    await _hubProxy.Invoke("PutCellValue", new CellValue(_selectedSession.Id, null, Worksheet, Cell, dValue));
                    break;
            }
        }

        public async Task GetNameValue()
        {
            if (_selectedSession == null || string.IsNullOrWhiteSpace(_name)) return;

            object[] values = new object[0];
            switch (ConnectionType)
            {
                case ConnectionType.Rest:
                    var response = await _client.GetAsync(string.Format("excel/sessions/{0}/names/{1}", _selectedSession.Id, Name));
                    if (response.IsSuccessStatusCode)
                    {
                        values = await response.Content.ReadAsAsync<object[]>();
                    }
                    break;
                case ConnectionType.SignalR:
            NameValue nameValue = null;
                    var nameRequest = new NameValue(_selectedSession.Id, null, Name, null);
                    nameValue = await _hubProxy.Invoke<NameValue>("GetNameValue", nameRequest);
                    values = nameValue.Values;
                    break;
            }

            this.NameValue = values != null ? string.Join(", ", values.Select(v=>Convert.ToString(v, CultureInfo.InvariantCulture))) : "";
        }

        public async Task SetNameValue()
        {
            if (_selectedSession == null) return;

            if (string.IsNullOrWhiteSpace(NameValue)) return;

            var values = NameValue.Split(',').Select(strValue =>
                {
                    double dvalue;
                    return Convert.ToDouble(strValue, CultureInfo.InvariantCulture.NumberFormat);

                    return double.NaN;
                }).Where(dValue => !double.IsNaN(dValue)).Cast<object>().ToArray();

            switch (ConnectionType)
            {
                case ConnectionType.Rest:
                    await _client.PutAsJsonAsync(string.Format("excel/sessions/{0}/names/{1}", _selectedSession.Id, Name), values);
                    break;
                case ConnectionType.SignalR:
                    var nameValue = new NameValue(_selectedSession.Id, null, Name, values);
                    await _hubProxy.Invoke("PutNameValue", nameValue);
                    break;
            }

        }

        protected string _worksheet;

        public string Worksheet
        {
            get { return _worksheet; }
            set
            {
                if (value != _worksheet)
                {
                    _worksheet = value;
                    NotifyOfPropertyChange(() => Worksheet);

                    if (_cellNameRegex.IsMatch(string.Format("{0}!{1}", _worksheet, _cell)))
                    {
                        GetCellValue();
                    }
                }
            }
        }

        protected ObservableCollection<string> _worksheets = new ObservableCollection<string>();

        public ObservableCollection<string> Worksheets
        {
            get { return _worksheets; }
        } 

        protected ObservableCollection<string> _names = new ObservableCollection<string>();

        public ObservableCollection<string> Names
        {
            get { return _names; }
        } 

        protected string _cell;

        public string Cell
        {
            get { return _cell; }
            set
            {
                if (value != _cell)
                {
                    _cell = value;
                    NotifyOfPropertyChange(() => Cell);

                    
                    if (_cellNameRegex.IsMatch(string.Format("{0}!{1}",_worksheet, _cell)))
                    {
                        GetCellValue();
                    }
                }
            }
        }

        protected string _cellValue;

        public string CellValue
        {
            get { return _cellValue; }
            set
            {
                if (value != _cellValue)
                {
                    _cellValue = value;
                    NotifyOfPropertyChange(() => CellValue);
                }
            }
        }

        protected string _name;

        public string Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    NotifyOfPropertyChange(() => Name);

                    if (Names.Contains(_name))
                    {
                        GetNameValue();
                    }
                }
            }
        }

        protected string _nameValue;

        public string NameValue
        {
            get { return _nameValue; }
            set
            {
                if (value != _nameValue)
                {
                    _nameValue = value;
                    NotifyOfPropertyChange(() => NameValue);
                }
            }
        }

        protected ConnectionType _connectionType;

        public ConnectionType ConnectionType
        {
            get { return _connectionType; }
            set
            {
                if (value != _connectionType)
                {
                    _connectionType = value;
                    NotifyOfPropertyChange(() => ConnectionType);
                    NotifyOfPropertyChange(() => UseRest);
                    NotifyOfPropertyChange(() => UseSignalR);
                }
            }
        }

        public bool UseRest
        {
            get
            {
                return ConnectionType == ConnectionType.Rest;
            }
            set
            {
                ConnectionType = ConnectionType.Rest;
            }
        }
        
        public bool UseSignalR
        {
            get
            {
                return ConnectionType == ConnectionType.SignalR;
            }
            set
            {
                ConnectionType = ConnectionType.SignalR;
            }
        }

        protected ObservableCollection<ExcelSession> _sessions = new ObservableCollection<ExcelSession>();

        public ObservableCollection<ExcelSession> Sessions
        {
            get { return _sessions; }
        }

        protected ExcelSession _selectedSession;

        public ExcelSession SelectedSession
        {
            get { return _selectedSession; }
            set
            {
                if (value != _selectedSession)
                {
                    _selectedSession = value;

                    if (_selectedSession != null && !_selectedSession.Users.Contains(Settings.Default.Username))
                    {
                        var task = Task.Run(()=> JoinSession(_selectedSession.Id));
                        task.Wait();
                    }

                    GetWorksheets();
                    GetNames();

                    NotifyOfPropertyChange(() => SelectedSession);
                    NotifyOfPropertyChange(()=> SessionWatchedItems);

                    NotifyOfPropertyChange(()=>SessionId);
                    NotifyOfPropertyChange(() => SessionName);
                    NotifyOfPropertyChange(() => SessionOwner);
                    NotifyOfPropertyChange(() => SessionCreated);
                    NotifyOfPropertyChange(() => SessionUsers);
                    NotifyOfPropertyChange(() => SessionWatches);
                }
            }
        }

        public int? SessionId { get { return _selectedSession!=null?(int?)_selectedSession.Id:null; } }
        public string SessionName { get { return _selectedSession!=null?_selectedSession.Name:null; } }
        public string SessionOwner { get { return _selectedSession!=null?_selectedSession.Owner:null; } }
        public DateTime? SessionCreated { get { return _selectedSession!=null?(DateTime?)_selectedSession.Created:null; } }
        public string SessionUsers { get { return _selectedSession!=null?string.Join(", ", _selectedSession.Users):null; } }
        public string SessionWatches
        {
            get
            {
                if (_selectedSession == null) return null;

                var strWatches = new List<string>();
                if (_selectedSession.WatchAllFormulaCells)
                {
                    strWatches.Add("All formula cells");
                }

                if (_selectedSession.WatchAllNames)
                {
                    strWatches.Add("All names");
                }

                strWatches.AddRange(_selectedSession.WatchCells);
                strWatches.AddRange(_selectedSession.WatchNames);

                return string.Join(", ", strWatches);
            }
        }

    }
}
