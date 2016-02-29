using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csShared.ThirdParty
{

    public class KeyAddedEventArgs<TKey, TValue>
    {
        #region Members
        #endregion Members

        #region Constructors
        #endregion Constructors

        #region Properties
        public TValue Value { get; set; }
        public TKey Key { get; set; }
        #endregion Properties

        #region Methods
        #endregion Methods

        #region Events
        #endregion Events


    }

    public class KeyEventArgs<TKey>
    {
        #region Members
        #endregion Members

        #region Constructors
        #endregion Constructors

        #region Properties
        public TKey Key { get; set; }
        #endregion Properties

        #region Methods
        #endregion Methods

        #region Events
        #endregion Events

    }

    public class KeyModifiedEventArgs<TKey, TValue>
    {
        #region Members
        #endregion Members

        #region Constructors
        #endregion Constructors

        #region Properties
        public TValue NewValue { get; set; }
        public TValue PreviousValue { get; set; }
        public TKey Key { get; set; }
        #endregion Properties

        #region Methods
        #endregion Methods

        #region Events
        #endregion Events
    }

    public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        #region Members
        public delegate void KeyModifiedEvent(object sender, KeyModifiedEventArgs<TKey, TValue> e);
        public delegate void KeyRetrievedEvent(object sender, KeyEventArgs<TKey> e);
        public delegate void ListClearedEvent(object sender, EventArgs e);
        public delegate void KeyAddedEvent(object sender, KeyAddedEventArgs<TKey, TValue> e);
        public delegate void KeyRemovedEvent(object sender, KeyEventArgs<TKey> e);
        #endregion Members

        #region Constructors
        #endregion Constructors

        #region Properties
        public new TValue this[TKey key]
        {
            get
            {
                if (KeyRetrieved != null)
                    KeyRetrieved(this, new KeyEventArgs<TKey> { Key = key });
                return base[key];

            }
            set
            {
                //if the key is not in the list add it
                //this mocks up the default behavior, but forces it to go through
                //the new Add function which raises the event.
                if (!ContainsKey(key))
                    Add(key, value);

                TValue prevValue = base[key];
                if (!prevValue.Equals(value))
                {
                    base[key] = value;
                    if (KeyModified != null)
                        KeyModified(this, new KeyModifiedEventArgs<TKey, TValue> { Key = key, NewValue = value, PreviousValue = prevValue });
                }
            }
        }
        #endregion Properties

        #region Methods
        public new void Clear()
        {
            if (Keys.Count > 0)
            {
                base.Clear();
                if (ListCleared != null)
                    ListCleared(this, new EventArgs());
            }
        }

        public new void Add(TKey key, TValue value)
        {
            base.Add(key, value);
            if (KeyAdded != null)
                KeyAdded(this, new KeyAddedEventArgs<TKey, TValue> { Key = key, Value = value });
        }

        public new bool Remove(TKey key)
        {
            bool retValue = base.Remove(key);
            if (retValue && KeyRemoved != null)
                KeyRemoved(this, new KeyEventArgs<TKey> { Key = key });

            return retValue;

        }
        #endregion Methods

        #region Events
        public event ListClearedEvent ListCleared;
        public event KeyAddedEvent KeyAdded;
        public event KeyModifiedEvent KeyModified;
        public event KeyRetrievedEvent KeyRetrieved;
        public event KeyRemovedEvent KeyRemoved;
        #endregion Events


        
    }
}
