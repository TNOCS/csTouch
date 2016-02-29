using DataServer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Xml.Linq;


namespace csGeoLayers
{

    public static class Extensions
    {
        public static void SuppressScriptErrors(this WebBrowser webBrowser, bool hide)
        {
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null)
                return;
            object objComWebBrowser = fiComWebBrowser.GetValue(webBrowser);
            if (objComWebBrowser == null)
                return;

            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { hide });
        }


        public static string SplitCamelCase(this string input)
        {
            return Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
        }

        public static void AddObjectAsElement(this XElement element, string name, object value)
        {

            if (value != null)
            {
                element.Add(new XElement(name, value));
            }
        }

        public static List<T> EnumToList<T>()
        {
            Type enumType = typeof(T);

            // Can't use type constraints on value types, so have to do check like this
            if (enumType.BaseType != typeof(Enum))
                throw new ArgumentException("T must be of type System.Enum");

            var enumValArray = Enum.GetValues(enumType);

            var enumValList = new List<T>(enumValArray.Length);

            foreach (int val in enumValArray)
            {
                enumValList.Add((T)Enum.Parse(enumType, val.ToString()));
            }

            return enumValList;
        }
    }

    // Program.cs
    public static class EmClass
    {
        public static List<XElement> GetLocalElements(this XElement s, string localname)
        {
            return s == null ? new List<XElement>() : s.Elements().Where(k => k.Name.LocalName == localname).ToList();
        }

        public static XElement GetFirstElement(this XElement s, string localname)
        {
            var r = s.Elements().Where(k => k.Name.LocalName == localname).ToList();
            return r.Count > 0 ? r.First() : null;
        }

        public static T GetElementValue<T>(this XElement s, string elementName, T defaultValue) where T : IConvertible
        {
            var e = s.GetFirstElement(elementName);
            if (e == null) return defaultValue;
            try
            {

                return GenericConverter.Parse<T>(e.Value);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static T GetValue<T>(this XElement s, T defaultValue) where T : IConvertible
        {
            if (s == null) return defaultValue;
            try
            {
                return GenericConverter.Parse<T>(s.Value);
            }
            catch
            {
                return defaultValue;
            }
        }
    }

    public class GenericConverter
    {
        public static T Parse<T>(string sourceValue) where T : IConvertible
        {
            return (T)Convert.ChangeType(sourceValue, typeof(T), CultureInfo.InvariantCulture);
        }

        public static T Parse<T>(string sourceValue, IFormatProvider provider) where T : IConvertible
        {
            return (T)Convert.ChangeType(sourceValue, typeof(T), provider);
        }
    }

    public static class CollectionExtensions
    {
        /// <summary>
        /// Calls the provided action on each item, providing the item and its index into the source.
        /// </summary>
        [Pure]
        public static void CountForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            Contract.Requires(source != null);
            Contract.Requires(action != null);

            int i = 0;
            source.ForEach(item => action(item, i++));
        }

        [Pure]
        public static IEnumerable<TTarget> CountSelect<TSource, TTarget>(this IEnumerable<TSource> source, Func<TSource, int, TTarget> func) {
            var i = 0;
            return source.Select(item => func(item, i++));
        }

        /// <summary>
        ///     Returns true if all items in the list are unique using
        ///     <see cref="EqualityComparer{T}.Default">EqualityComparer&lt;T&gt;.Default</see>.
        /// </summary>
        /// <exception cref="ArgumentNullException">if <param name="source"/> is null.</exception>
        [Pure]
        public static bool AllUnique<T>(this IList<T> source)
        {

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            return source.TrueForAllPairs((a, b) => !comparer.Equals(a, b));
        }

        public static IComparer<T> ToComparer<T>(this Func<T, T, int> compareFunction)
        {
            Contract.Requires(compareFunction != null);
            return new FuncComparer<T>(compareFunction);
        }

        #region impl
        private class FuncComparer<T> : IComparer<T>
        {
            public FuncComparer(Func<T, T, int> func)
            {
                Contract.Requires(func != null);
                m_func = func;
            }

            public int Compare(T x, T y)
            {
                return m_func(x, y);
            }

            [ContractInvariantMethod]
            void ObjectInvariant()
            {
                Contract.Invariant(m_func != null);
            }

            private readonly Func<T, T, int> m_func;
        }


        #endregion

        private class FuncEqualityComparer<T> : IEqualityComparer<T>
        {
            public FuncEqualityComparer(Func<T, T, bool> func)
            {
                Contract.Requires(func != null);
                m_func = func;
            }
            public bool Equals(T x, T y)
            {
                return m_func(x, y);
            }

            public int GetHashCode(T obj)
            {
                return 0; // this is on purpose. Should only use function...not short-cut by hashcode compare
            }

            [ContractInvariantMethod]
            void ObjectInvariant()
            {
                Contract.Invariant(m_func != null);
            }

            private readonly Func<T, T, bool> m_func;
        }

        /// <summary>
        ///     Returns true if <paramref name="compare"/> returns
        ///     true for every pair of items in <paramref name="source"/>.
        /// </summary>
        [Pure]
        public static bool TrueForAllPairs<T>(this IList<T> source, Func<T, T, bool> compare)
        {
            Contract.Requires(source != null);
            Contract.Requires(compare != null);

            for (int i = 0; i < source.Count; i++)
            {
                for (int j = i + 1; j < source.Count; j++)
                {
                    if (!compare(source[i], source[j]))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     Returns true if <paramref name="compare"/> returns true of every
        ///     adjacent pair of items in the <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     If there are n items in the collection, n-1 comparisons are done.
        /// </para>
        /// <para>
        ///     Every valid [i] and [i+1] pair are passed into <paramref name="compare"/>.
        /// </para>
        /// <para>
        ///     If <paramref name="source"/> has 0 or 1 items, true is returned.
        /// </para>
        /// </remarks>
        [Pure]
        public static bool TrueForAllAdjacentPairs<T>(this IList<T> source, Func<T, T, bool> compare)
        {
            Contract.Requires(source != null);
            Contract.Requires(compare != null);

            for (int i = 0; i < (source.Count - 1); i++)
            {
                if (!compare(source[i], source[i + 1]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Returns true if all of the items in <paramref name="source"/> are not
        ///     null or empty.
        /// </summary>
        /// <exception cref="ArgumentNullException">if <param name="source"/> is null.</exception>
        [Pure]
        public static bool AllNotNullOrEmpty(this IEnumerable<string> source)
        {
            Contract.Requires(source != null);
            return source.All(item => !string.IsNullOrEmpty(item));
        }

        /// <summary>
        ///     Returns true if all items in <paramref name="source"/> exist
        ///     in <paramref name="set"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">if <param name="source"/> or <param name="set"/> are null.</exception>
        [Pure]
        public static bool AllExistIn<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> set)
        {
            Contract.Requires(source != null);
            Contract.Requires(set != null);

            return source.All(item => set.Contains(item));
        }

        /// <summary>
        ///     Returns true if <paramref name="source"/> has no items in it; otherwise, false.
        /// </summary>
        /// <remarks>
        /// <para>
        ///     If an <see cref="ICollection{TSource}"/> is provided,
        ///     <see cref="ICollection{TSource}.Count"/> is used.
        /// </para>
        /// <para>
        ///     Yes, this does basically the same thing as the
        ///     <see cref="System.Linq.Enumerable.Any{TSource}(IEnumerable{TSource})"/>
        ///     extention. The differences: 'IsEmpty' is easier to remember and it leverages
        ///     <see cref="ICollection{TSource}.Count">ICollection.Count</see> if it exists.
        /// </para>
        /// </remarks>
        [Pure]
        public static bool IsEmpty<TSource>(this IEnumerable<TSource> source)
        {
            Contract.Requires(source != null);

            if (source is ICollection<TSource>)
            {
                return ((ICollection<TSource>)source).Count == 0;
            }
            else
            {
                using (IEnumerator<TSource> enumerator = source.GetEnumerator())
                {
                    return !enumerator.MoveNext();
                }
            }
        }

        /// <summary>
        ///     Returns the index of the first item in <paramref name="source"/>
        ///     for which <paramref name="predicate"/> returns true. If none, -1.
        /// </summary>
        /// <param name="source">The source enumerable.</param>
        /// <param name="predicate">The function to evaluate on each element.</param>
        [Pure]
        public static int IndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            Contract.Requires(source != null);
            Contract.Requires(predicate != null);

            int index = 0;
            foreach (TSource item in source)
            {
                if (predicate(item))
                {
                    return index;
                }
                index++;
            }
            return -1;
        }

        /// <summary>
        ///     Returns a new <see cref="ReadOnlyCollection{TSource}"/> using the
        ///     contents of <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        ///     The contents of <paramref name="source"/> are copied to
        ///     an array to ensure the contents of the returned value
        ///     don't mutate.
        /// </remarks>
        public static ReadOnlyCollection<TSource> ToReadOnlyCollection<TSource>(this IEnumerable<TSource> source)
        {
            Contract.Requires(source != null);
            Contract.Ensures(Contract.Result<ReadOnlyCollection<TSource>>() != null);
            return new ReadOnlyCollection<TSource>(source.ToArray());
        }

        /// <summary>
        ///     Performs the specified <paramref name="action"/>
        ///     on each element of the specified <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The sequence to which is applied the specified <paramref name="action"/>.</param>
        /// <param name="action">The action applied to each element in <paramref name="source"/>.</param>
        public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
        {
            Contract.Requires(source != null);
            Contract.Requires(action != null);

            foreach (TSource item in source)
            {
                action(item);
            }
        }

        /// <summary>
        ///     Removes the last element from <paramref name="source"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The list from which to remove the last element.</param>
        /// <returns>The last element.</returns>
        /// <remarks><paramref name="source"/> must have at least one element and allow changes.</remarks>
        public static TSource RemoveLast<TSource>(this IList<TSource> source)
        {
            Contract.Requires(source != null);
            Contract.Requires(source.Count > 0);
            TSource item = source[source.Count - 1];
            source.RemoveAt(source.Count - 1);
            return item;
        }

        /// <summary>
        ///     If <paramref name="source"/> is null, return an empty <see cref="IEnumerable{TSource}"/>;
        ///     otherwise, return <paramref name="source"/>.
        /// </summary>
        public static IEnumerable<TSource> EmptyIfNull<TSource>(this IEnumerable<TSource> source)
        {
            return source ?? Enumerable.Empty<TSource>();
        }

        /// <summary>
        ///     Recursively projects each nested element to an <see cref="IEnumerable{TSource}"/>
        ///     and flattens the resulting sequences into one sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to project.</param>
        /// <param name="recursiveSelector">A transform to apply to each element.</param>
        /// <returns>
        ///     An <see cref="IEnumerable{TSource}"/> whose elements are the
        ///     result of recursively invoking the recursive transform function
        ///     on each element and nested element of the input sequence.
        /// </returns>
        public static IEnumerable<TSource> SelectRecursive<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, IEnumerable<TSource>> recursiveSelector)
        {
            Contract.Requires(source != null);
            Contract.Requires(recursiveSelector != null);

            Stack<IEnumerator<TSource>> stack = new Stack<IEnumerator<TSource>>();
            stack.Push(source.GetEnumerator());

            try
            {
                while (stack.Count > 0)
                {
                    if (stack.Peek().MoveNext())
                    {
                        TSource current = stack.Peek().Current;

                        yield return current;

                        stack.Push(recursiveSelector(current).GetEnumerator());
                    }
                    else
                    {
                        stack.Pop().Dispose();
                    }
                }
            }
            finally
            {
                while (stack.Count > 0)
                {
                    stack.Pop().Dispose();
                }
            }
        } //*** SelectRecursive


        public static T Random<T>(this IList<T> source)
        {
            Contract.Requires(source != null);
            Contract.Requires(source.Count > 0);
            return source[Rnd.Next(source.Count)];
        }

        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, Func<T, T, bool> comparer)
        {
            Contract.Requires(source != null);
            Contract.Requires(comparer != null);
            return source.Distinct(comparer.ToEqualityComparer());
        }

        public static IEqualityComparer<T> ToEqualityComparer<T>(this Func<T, T, bool> func)
        {
            Contract.Requires(func != null);
            return new FuncEqualityComparer<T>(func);
        }

        public static Random Rnd
        {
            get
            {
                Contract.Ensures(Contract.Result<Random>() != null);
                var r = (Random)s_random.Target;
                if (r == null)
                {
                    s_random.Target = r = new Random();
                }
                return r;
            }
        }

        private static readonly WeakReference s_random = new WeakReference(null);

        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, params T[] items)
        {
            return source.Concat(items.AsEnumerable());
        }

        [Pure]
        public static bool Contains<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            return dictionary.Contains(new KeyValuePair<TKey, TValue>(key, value));
        }

        [Pure]
        public static bool CountAtLeast<T>(this IEnumerable<T> source, int count)
        {
            Contract.Requires(source != null);
            if (source is ICollection<T>)
            {
                return ((ICollection<T>)source).Count >= count;
            }
            else
            {
                using (var enumerator = source.GetEnumerator())
                {
                    while (count > 0)
                    {
                        if (enumerator.MoveNext())
                        {
                            count--;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        public static IEnumerable<TSource> Except<TSource, TOther>(this IEnumerable<TSource> source, IEnumerable<TOther> other, Func<TSource, TOther, bool> comparer)
        {
            return from item in source
                   where !other.Any(x => comparer(item, x))
                   select item;
        }

        public static IEnumerable<TSource> Intersect<TSource, TOther>(this IEnumerable<TSource> source, IEnumerable<TOther> other, Func<TSource, TOther, bool> comparer)
        {
            return from item in source
                   where other.Any(x => comparer(item, x))
                   select item;
        }

        public static INotifyCollectionChanged AsINPC<T>(this ReadOnlyObservableCollection<T> source)
        {
            Contract.Requires(source != null);
            return (INotifyCollectionChanged)source;
        }

        /// <summary>
        /// Creates an <see cref="ObservableCollection"/> from the <see cref="IEnumerable"/>.
        /// </summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <param name="source">The <see cref="IEnumerable"/> to create the <see cref="ObservableCollection"/> from.</param>
        /// <returns>An <see cref="ObservableCollection"/> that contains elements from the input sequence.</returns>
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
        {
            Contract.Requires(source != null);
#if WP7
            var result = new ObservableCollection<T>();
            foreach (var item in source)
            {
                result.Add(item);
            }
            return result;
#else
            return new ObservableCollection<T>(source);
#endif
        }

        /// <summary>
        /// Convert a label to a double: first, look for the label in the PoI, or otherwise, 
        /// look for a global variable in the service settings.
        /// Decimal notation may be with point or comma.
        /// </summary>
        /// <param name="bc">BaseContent or PoI</param>
        /// <param name="key">Label name</param>
        /// <returns>Converted result or 0 in case of failure.</returns>
        public static double LabelToDouble(this BaseContent bc, string key)
        {
            string result;
            if (bc.Labels.ContainsKey(key)) result = bc.Labels[key];
            else if (bc.Service.Settings != null && bc.Service.Settings.Labels != null && bc.Service.Settings.Labels.ContainsKey(key))
            {
                result = bc.Service.Settings.Labels[key];
            }
            else return 0;
            double fvalue;
            if (double.TryParse(result, NumberStyles.Any, CultureInfo.InvariantCulture, out fvalue)) return fvalue;
            double.TryParse(result, NumberStyles.Any, new CultureInfo("NL-nl"), out fvalue);
            return fvalue;
        }
    }





}
