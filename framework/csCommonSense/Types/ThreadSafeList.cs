using System.Collections;
using System.Collections.Generic;

namespace csCommon.Utils
{
    public class ThreadSafeList<T> : List<T>, IEnumerable
    {
        protected List<T> InteralList = new List<T>();

        // Other Elements of IList implementation

        public new IEnumerator<T> GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Clone().GetEnumerator();
        }

        protected static object Lock = new object();

        public List<T> Clone()
        {
            var newList = new List<T>();

            lock (Lock)
            {
                InteralList.ForEach(newList.Add);
            }

            return newList;
        }
    }
}
