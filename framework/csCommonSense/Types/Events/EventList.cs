using System;
using System.Windows.Data;
using Caliburn.Micro;

namespace csEvents
{
    public class EventList : BindableCollection<IEvent> 
    {

        private readonly object listlock = new object();
        
        public string Id { get; set; }
        
        public EventList()
        {
            BindingOperations.EnableCollectionSynchronization(this, listlock);
        }

        public string Name { get; set; }

        public class EventClickedArgs : EventArgs
        {
            public IEvent e;
            public object sender;
            public string command;
        }
        
        public event EventHandler<EventClickedArgs> Clicked;

        public void TriggerClicked(object sender, IEvent e, string command)
        {
            var handler = Clicked;
            if (handler != null) handler(sender, new EventClickedArgs { command = command, e = e, sender = sender });
        }


        public System.Collections.Generic.IEnumerable<object> Where(Func<IEvent, int, bool> func)
        {
            throw new NotImplementedException();
        }
    }
}