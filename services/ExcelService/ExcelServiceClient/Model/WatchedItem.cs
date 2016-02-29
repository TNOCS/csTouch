using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using ExcelServiceModel;

namespace ExcelServiceClient.Model
{
    public class WatchedItem : PropertyChangedBase
    {
        protected int _sessionId;

        public int SessionId
        {
            get { return _sessionId; }
            set
            {
                if (value != _sessionId)
                {
                    _sessionId = value;
                    NotifyOfPropertyChange(() => SessionId);
                }
            }
        }

        protected string _itemName;

        public string ItemName
        {
            get { return _itemName; }
            set
            {
                if (value != _itemName)
                {
                    _itemName = value;
                    NotifyOfPropertyChange(() => ItemName);
                }
            }
        }

        protected object _itemValue;

        public object ItemValue
        {
            get { return _itemValue; }
            set
            {
                if (value != _itemValue)
                {
                    _itemValue = value;
                    NotifyOfPropertyChange(() => ItemValue);
                }
            }
        }
    }
}
