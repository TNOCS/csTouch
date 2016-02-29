using System.Text;
using Caliburn.Micro;
using csCommon.Types.TextAnalysis;
using csCommon.Types.TextAnalysis.TextFinder;
using csShared.Utils;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DataServer
{
    [ProtoContract]
    [Serializable]
    public class ContentList : BindableCollection<BaseContent>, ITextSearchableCollection<BaseContent>, ITextSearchable
    {
        [NonSerialized]
        private readonly object queueLock = new object();
        [NonSerialized]
        private readonly List<BaseContent> sendQueue = new List<BaseContent>();
        private bool isRessetable;
        private DateTime lastSend = DateTime.Now;
        [NonSerialized]
        private Timer sendTimer;
       
        [NonSerialized]
        private bool sendtimerRunning;

        private int isBatchLoading;

        public int IsBatchLoading
        {
            get { return isBatchLoading; }
            private set
            {
                isBatchLoading = value;
            }
        }
        
        public event EventHandler BatchFinished;
        public event EventHandler BatchStarted;

        public void StartBatch()
        {
            IsBatchLoading += 1;
            if (BatchStarted != null) BatchStarted(this, null);

            /*if (isBatchLoading == 1)
            {
                IsNotifying = false;
            }*/
        }

        public void FinishBatch()
        {
            IsBatchLoading -= 1;

            /*if (isBatchLoading == 0)
            {
                IsNotifying = true;
            }*/
            if (BatchFinished != null) BatchFinished(this, null);
            
        }
        //{
        //    //Enable the cross acces to this collection elsewhere
        //    //BindingOperations.EnableCollectionSynchronization(this, _lock);
        //}

        [ProtoMember(1)]
        public string Id { get; set; }

        [ProtoMember(2)]
        public Type ContentType { get; set; }

        public Service Service { get; set; }

        public bool IsRessetable
        {
            get { return isRessetable; }
            set
            {
                isRessetable = value;
                NotifyOfPropertyChange();
            }
        }

        protected override void InsertItemBase(int index, BaseContent item)
        {
            try
            {
                if (item == null) return;
                
                if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
                base.InsertItemBase(index, item);
                if (Service == null) return;
                RegisterContent(item, Service.IsInitialized);
                Service.TriggerContentChanged(item);
            }
            catch (Exception e)
            {
                Logger.Log("Data Server","Error adding item " + item.ContentId,e.Message,Logger.Level.Error);
            }            
        }

        protected override void RemoveItemBase(int index)
        {
            if (Count <= index) return;
            var p = this[index];
            RemoveContent(p);
            base.RemoveItemBase(index);
            Service.TriggerContentChanged(p);
        }

        public void UnRegisterContent(BaseContent item)
        {
            item.Changed -= item_Changed;            
        }

        public void RegisterContent(BaseContent item, bool send = true)
        {
            if (Service.IsLocal && Service.Mode == Mode.client) return;

            foreach (var m in item.AllMedia)
            {
                m.Content = item;
            }
            //read image
            if (Service.IsInitialized) item.CheckIcon();

            if (item.UserId == "" && Service.client != null) item.UserId = Service.client.Status.Name;
            if (send) SendContent(item);
            item.Changed += item_Changed;

            //var timeChanged = Observable.FromEventPattern<ChangedEventArgs>(ev => item.Changed += ev, ev => item.Changed -= ev);
            //timeChanged.Sample(TimeSpan.FromMilliseconds(333)).Subscribe(k => SendContent(item));

            //item.PropertyChanged += (cs, ce) => 
            //    SendContent(item);
        }

        void item_Changed(object sender, ChangedEventArgs e)
        {
            SendContent((BaseContent)sender);
        }

        private void RemoveContent(IContent c)
        {
            if (c.IsInTransit) return;
            if (Service.IsLocal && Service.Mode == Mode.client) return;
            var dcm = new ContentMessage
            {
                Id = c.Id,
                ContentList = Id,
                Action = ContentMessageActions.Remove,
                Sender = Service.client.Id
            };
            SendContentMessage(dcm);
        }

        private void SendContent(BaseContent c) {
            lock (queueLock) {
                if (!sendQueue.Contains(c)) sendQueue.Add(c);
            }
            if (c.IsInTransit || !c.IsNotifying) return;
            if (lastSend.AddMilliseconds(300) < DateTime.Now) {
                SendQueue();
            }
            else {
                if (!sendtimerRunning) {
                    sendtimerRunning = true;
                    sendTimer = new Timer(delegate {
                        SendQueue();
                        sendtimerRunning = false;
                    }, null, 400, Timeout.Infinite);
                }
            }
            Service.TriggerContentChanged(c);
        }

        private void SendQueue()
        {
            if (Service.client == null) return;
            
            lock (queueLock)
            {
                foreach (var c in sendQueue)
                {
                    var prevNotifying = c.IsNotifying;
                    c.IsNotifying = false;
                    c.RevisionId++;

                    using (var msc = new MemoryStream())
                    {
                        try
                        {
                            Service.Model.SerializeWithLengthPrefix(msc, c, ContentType, PrefixStyle.Base128, 0);
                        }
                        catch (Exception e)
                        {
                            Logger.Log("Poi Service","Poi Sync Error",e.Message,Logger.Level.Error);
                        }
                        
                        var cm = new ContentMessage
                        {
                            Id = c.Id,
                            Content = msc.ToArray(),
                            ContentList = Id,
                            Action = ContentMessageActions.Add,
                            Sender = Service.client.Id,
                            ContentType = ContentType
                        };
                        SendContentMessage(cm);
                        c.IsNotifying = prevNotifying;
                    }
                }
                
                lastSend = DateTime.Now;
                sendQueue.Clear();
            }
        }

        public void SendContentMessage(ContentMessage cm)
        {
            if (Service == null || Service.serviceChannel == null) return;
            var content = cm.ConvertToStream().ToArray();
            Service.serviceChannel.SignalBuffer(0, content);
        }

        internal void RegisterAllContent()
        {
            foreach (var c in this)
            {
                RegisterContent(c, false); //Service.IsInitialized);
            }
        }

        internal void UnRegisterAllCountent()
        {
            foreach (var c in this)
            {
                UnRegisterContent(c);
            }
        }

        public long IndexId
        {
            get { return FullText.GetHashCode(); } // TODO This can be cached.
        }

        public string FullText
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (BaseContent baseContent in Items)
                {
                    sb.Append(baseContent.FullText ?? "").Append(" ");                
                }
                return sb.ToString();                
            }
        }

        public WordHistogram Keywords
        {
            get // TODO Slow because it does not cache.
            {
                var fullText = FullText;
                var wordHistogram = new WordHistogram(fullText.Language(), fullText);
                return wordHistogram;
            }
        }
    }
}