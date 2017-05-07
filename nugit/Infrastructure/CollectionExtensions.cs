using System;
using System.Collections.Generic;
using System.Linq;

namespace nugit.Infrastructure
{

    public static class CollectionExtensions
    {
        
        /// <summary>
        /// Remove the specified number of items from the specified position in a list
        /// </summary>
        ///
        public static void RemoveAt<T>(this IList<T> list, int index, int count)
        {
            if (list == null) throw new ArgumentNullException("list");
            if (count < 0 || count > (list.Count - index)) throw new ArgumentOutOfRangeException("count");
            for (int i = 0; i < count; i++) list.RemoveAt(index);
        }


        /// <summary>
        /// Insert item(s) into the list at the specified position
        /// </summary>
        ///
        public static void Insert<T>(this IList<T> list, int index, params T[] items)
        {
            items = items ?? new T[0];
            if (list == null) throw new ArgumentNullException("list");
            if (index < 0 || list.Count < index) throw new ArgumentOutOfRangeException("index");
            foreach (T item in items.Reverse()) list.Insert(index, item);
        }


        /// <summary>
        /// Add item(s) to a collection
        /// </summary>
        ///
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (items == null) throw new ArgumentNullException("items");
            foreach (T item in items) collection.Add(item);
        }

    }

}
