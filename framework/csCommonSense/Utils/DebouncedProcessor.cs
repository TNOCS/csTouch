using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using DataServer;

namespace csCommon.Utils
{
    public class DebouncedProcessor<T> where T: BaseContent
    {
        private readonly List<T> _buffer = new List<T>();
        private readonly Timer _debounceTimer = null;

        public long DebounceMillis { get; private set; }
        public long DebounceEntries { get; private set; }
        private DateTime? _start = null;

        public EventHandler<T[]> EntriesAdded = delegate { };

        public DebouncedProcessor(long debounceMillis, long debounceEntries)
        {
            this.DebounceMillis = debounceMillis;
            this.DebounceEntries = debounceEntries;

            _debounceTimer = new Timer(DebounceMillis);
            _debounceTimer.Elapsed += DebounceTimerElapsed;
        }

        public bool IsCollecting => _start.HasValue;

        protected void DebounceTimerElapsed(object sender, EventArgs args)
        {
            Flush();
        }

        protected bool ShouldFlush
        {
            get
            {
                if (!IsCollecting) return false;
                return (this._start.HasValue && (DateTime.Now - this._start.Value).TotalMilliseconds > this.DebounceMillis) || _buffer.Count > this.DebounceEntries;
            }
        }

        protected void Flush()
        {
            if (ShouldFlush)
            {
                if (_buffer.Count == 0)
                {
                    StopCollecting();
                    return;
                }

                var entries = _buffer.ToArray();
                _buffer.Clear();

                EntriesAdded(this, entries);
            }
        }

        protected void StopCollecting()
        {
            if (!IsCollecting) return;

            _debounceTimer?.Stop();
            _start = null;

        }

        protected void StartCollecting()
        {
            if (IsCollecting) return;

            _start = DateTime.Now;
            _debounceTimer.Start();
        }
        public void Add(T entry)
        {
            ///TODO: check if received entry is recent or may be ignored
            var existing = _buffer.FirstOrDefault(bc => bc.ContentId == entry.ContentId);
            if (existing != null)
            {
                _buffer.Remove(existing);
            }

            _buffer.Add(entry);

            StartCollecting();

            this.Flush();
        }
    }
}
