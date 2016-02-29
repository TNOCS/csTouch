using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using csCommon.Annotations;
using csEvents.Sensors;

namespace DataServer
{
    public class SensorSet : ConcurrentDictionary<string, DataSet>, INotifyPropertyChanged
    {
        private string title;

        public string Title
        {
            get { return title; }
            set
            {
                if (string.Equals(title, value)) return;
                title = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([NotNull, CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}