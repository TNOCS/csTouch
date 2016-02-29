using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using ExcelServiceModel;

namespace ExcelServiceClient.ViewModels
{
    public class CreateSessionViewModel : Screen
    {
        protected string _sessionName;

        public string SessionName
        {
            get { return _sessionName; }
            set
            {
                if (value != _sessionName)
                {
                    _sessionName = value;
                    NotifyOfPropertyChange(() => SessionName);
                }
            }
        }

        protected string _workbookName;

        public string WorkbookName
        {
            get { return _workbookName; }
            set
            {
                if (value != _workbookName)
                {
                    _workbookName = value;
                    NotifyOfPropertyChange(() => WorkbookName);
                }
            }
        }

        protected bool _watchAllFormulaCells;

        public bool WatchAllFormulaCells
        {
            get { return _watchAllFormulaCells; }
            set
            {
                if (value != _watchAllFormulaCells)
                {
                    _watchAllFormulaCells = value;
                    NotifyOfPropertyChange(() => WatchAllFormulaCells);
                }
            }
        }

        protected bool _watchAllNames;

        public bool WatchAllNames
        {
            get { return _watchAllNames; }
            set
            {
                if (value != _watchAllNames)
                {
                    _watchAllNames = value;
                    NotifyOfPropertyChange(() => WatchAllNames);
                }
            }
        }

        protected bool _useCalculationChain;

        public bool UseCalculationChain
        {
            get { return _useCalculationChain; }
            set
            {
                if (value != _useCalculationChain)
                {
                    _useCalculationChain = value;
                    NotifyOfPropertyChange(() => UseCalculationChain);
                }
            }
        }


        protected ObservableCollection<StringEntry> _watchCellEntries = new ObservableCollection<StringEntry>();

        public ObservableCollection<StringEntry> WatchCellEntries
        {
            get { return _watchCellEntries; }
        }

        public string[] WatchCells
        {
            get { return _watchCellEntries.Select(se => se.Value).ToArray(); }
        }

        protected ObservableCollection<StringEntry> _watchNameEntries = new ObservableCollection<StringEntry>();

        public ObservableCollection<StringEntry> WatchNameEntries
        {
            get { return _watchNameEntries; }
        }

        public string[] WatchNames
        {
            get { return _watchNameEntries.Select(we => we.Value).ToArray(); }
        }

        public ExcelSession Session
        {
            get
            {
                return new ExcelSession()
                    {
                        Name = SessionName,
                        WorkbookName = WorkbookName,
                        WatchAllFormulaCells = WatchAllFormulaCells,
                        WatchAllNames = WatchAllNames,
                        WatchCells = WatchCells,
                        WatchNames = WatchNames,
                        UseCalculationChain = UseCalculationChain,
                    };
            }
        }

        public void Ok()
        {
            this.TryClose(true);
        }

        public void Cancel()
        {
            this.TryClose(false);
        }
    }

    public class StringEntry : PropertyChangedBase
    {
        protected string _value;

        public string Value
        {
            get { return _value; }
            set
            {
                if (value != _value)
                {
                    _value = value;
                    NotifyOfPropertyChange(() => Value);
                }
            }
        }
    }
}
