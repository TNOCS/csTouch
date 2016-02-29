using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Caliburn.Micro;

namespace csCommon.csMapCustomControls.CircularMenu
{

    public enum MenuType
    {
        Button,
        Color

    }

    public class MenuItemEventArgs : EventArgs
    {
        public CircularMenuItem SelectedItem;
        public CircularMenu Menu;
    }

    public class CircularControlMenuItem : Control, INotifyPropertyChanged
    {

        private static readonly Dictionary<string, PropertyChangedEventArgs> ArgumentInstances = new Dictionary<string, PropertyChangedEventArgs>();


        /// <summary>
        /// The property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notify any listeners that the property value has changed.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("PropertyName cannot be empty or null.");
            }

            var handler = PropertyChanged;
            if (handler == null) return;
            PropertyChangedEventArgs args;
            if (!ArgumentInstances.TryGetValue(propertyName, out args))
            {
                args = new PropertyChangedEventArgs(propertyName);
                ArgumentInstances[propertyName] = args;
            }

            // Fire the change event. The smart dispatcher will directly
            // invoke the handler if this change happened on the UI thread,
            // otherwise it is sent to the proper dispatcher.
            Execute.OnUIThread(() => handler(this, args));

        }
    }

   
    public class CircularMenuItem : Control, INotifyPropertyChanged
    {
        public int AutoCloseTimeout { get; set; }

        public Point? StartPosition { get; set; }

        private static readonly Dictionary<string, PropertyChangedEventArgs> ArgumentInstances = new Dictionary<string, PropertyChangedEventArgs>();

        /// <summary>
        /// The property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler OpenChanged;

        private bool canCheck;

        public bool CanCheck
        {
            get { return canCheck; }
            set { 
                canCheck = value;
                NotifyPropertyChanged("CanCheck");
            }
        }

        private bool isChecked;

        public bool IsChecked
        {
            get { return isChecked; }
            set { isChecked = value; NotifyPropertyChanged("IsChecked"); }
        }
        
        private bool isOpen;

        public bool IsOpen
        {
            get { return isOpen; }
            set
            {
                if (isOpen == value) return;
                isOpen = value; NotifyPropertyChanged("IsOpen");
                if (OpenChanged != null) OpenChanged(this, null);
            }
        }
        

        /// <summary>
        /// Notify any listeners that the property value has changed.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                throw new ArgumentException("PropertyName cannot be empty or null.");
            }

            var handler = PropertyChanged;
            if (handler == null) return;
            PropertyChangedEventArgs args;
            if (!ArgumentInstances.TryGetValue(propertyName, out args))
            {
                args = new PropertyChangedEventArgs(propertyName);
                ArgumentInstances[propertyName] = args;
            }

            // Fire the change event. The smart dispatcher will directly
            // invoke the handler if this change happened on the UI thread,
            // otherwise it is sent to the proper dispatcher.
            Execute.OnUIThread(()=>handler(this,args));
            
        }

        public string Id { get; set; }

        public new CircularMenuItem Parent { get; set; }

        public new string Tag { get; set; }

        public event EventHandler<MenuItemEventArgs> ItemSelected;
        public event EventHandler<MenuItemEventArgs> Selected;

        public void TriggerSelected(CircularMenu m, FrameworkElement item = null)
        {
            var handler = Selected;
            if (handler != null) handler(item, new MenuItemEventArgs { SelectedItem = this, Menu = m });
        }

        public void TriggerItemSelected(CircularMenuItem item, CircularMenu m)
        {
            var handler = ItemSelected;
            if (handler != null) handler(this, new MenuItemEventArgs { SelectedItem = item, Menu = m });
        }

        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; NotifyPropertyChanged("Title"); }
        }

        private List<CircularMenuItem> items = new List<CircularMenuItem>();

        public List<CircularMenuItem> Items
        {
            get { return items; }
            set { items = value; NotifyPropertyChanged("Items"); }
        }

        private string icon;

        public string Icon
        {
            get { return icon; }
            set
            {
                icon = value;
                NotifyPropertyChanged("Icon");
            }
        }

        public string Element { get; set; }

        public MenuType Type { get; set; }

        private Brush fill;

        public Brush Fill
        {
            get { return fill; }
            set { fill = value; NotifyPropertyChanged("Fill"); }
        }

        public int Position { get; set; }

        public CircularMenu Menu { get; set; }
    }
}