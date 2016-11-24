using System;
using System.Collections.Generic;

namespace NuGit.Infrastructure
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

    }

}
