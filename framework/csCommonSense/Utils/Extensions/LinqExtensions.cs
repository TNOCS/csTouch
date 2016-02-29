using System;
using System.Collections.Generic;
using csCommon.Utils.Collections;

namespace csShared.Utils
{
  public static class LinqExtensions
  {

      //
      public static ConcurrentObservableSortedDictionary<TKey, TValue> ToConcurrentObservableSortedDictionary<TSource, TKey, TValue>
      (this IEnumerable<TSource> source,
       Func<TSource, TKey> keySelector,
       Func<TSource, TValue> valueSelector)
      {
          // TODO: Argument validation
          var ret = new ConcurrentObservableSortedDictionary<TKey, TValue>();
          foreach (var element in source)
          {
              ret.Add(keySelector(element), valueSelector(element));
          }
          return ret;
      }

      public static SortedDictionary<TKey, TValue> ToSortedDictionary<TSource, TKey, TValue>
      (this IEnumerable<TSource> source,
       Func<TSource, TKey> keySelector,
       Func<TSource, TValue> valueSelector)
      {
          // TODO: Argument validation
          var ret = new SortedDictionary<TKey, TValue>();
          foreach (var element in source)
          {
              ret.Add(keySelector(element), valueSelector(element));
          }
          return ret;
      }

    public static SortedList<TKey, TValue> ToSortedList<TSource, TKey, TValue>
      (this IEnumerable<TSource> source,
       Func<TSource, TKey> keySelector,
       Func<TSource, TValue> valueSelector)
    {
      // TODO: Argument validation
      var ret = new SortedList<TKey, TValue>();
      foreach (var element in source)
      {
        ret.Add(keySelector(element), valueSelector(element));
      }
      return ret;
    }
  }
}
