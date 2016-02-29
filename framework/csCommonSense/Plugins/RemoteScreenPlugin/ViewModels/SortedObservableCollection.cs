using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Caliburn.Micro;

namespace csRemoteScreenPlugin
{
    public class SortedObservableCollection<T> : ObservableCollection<T>
    {
        private readonly Func<T, long> func;

        public SortedObservableCollection(Func<T, long> func)
        {
            this.func = func;
        }

        public SortedObservableCollection(Func<T, long> func, IEnumerable<T> collection)
            : base(collection)
        {
            this.func = func;
        }

        public SortedObservableCollection(Func<T, long> func, List<T> list)
            : base(list)
        {
            this.func = func;
        }

        protected override void InsertItem(int index, T item)
        {
            Execute.OnUIThread(() =>
                {
                    bool added = false;
                    for (int idx = 0; idx < Count; idx++)
                    {
                        if (func(item) > func(Items[idx]))
                        {
                            base.InsertItem(idx, item);
                            added = true;
                            break;
                        }
                    }

                    if (!added)
                    {
                        base.InsertItem(index, item);
                    }
                });

        }
    }
}