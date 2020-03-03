using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicPlayer.Models.EventArgs
{
    public class ChangeCollectionItem<T>
    {
        public int Index { get; set; }

        public T Item { get; set; }

        public ChangeCollectionItem()
        {
        }

        public ChangeCollectionItem(int index, T item)
        {
            Index = index;
            Item = item;
        }

        public static List<ChangeCollectionItem<T>> GetAddedChanged(IEnumerable<T> adds, IEnumerable<T> src)
        {
            T[] array = src.ToArray();
            List<ChangeCollectionItem<T>> changes = new List<ChangeCollectionItem<T>>();

            foreach (T addItem in adds?.ToArray() ?? Enumerable.Empty<T>())
            {
                if (array.Contains(addItem)) continue;

                changes.Add(new ChangeCollectionItem<T>(array.Length + changes.Count, addItem));
            }

            return changes;
        }

        public static List<ChangeCollectionItem<T>> GetRemovedChanged(IEnumerable<T> removes, IEnumerable<T> src)
        {
            T[] array = src.ToArray();
            List<ChangeCollectionItem<T>> changes = new List<ChangeCollectionItem<T>>();

            foreach (T removeItem in removes?.ToArray() ?? Enumerable.Empty<T>())
            {
                int index = Array.IndexOf(array, removeItem);

                if (index == -1) continue;

                index -= changes.Count(c => c.Index < index);
                changes.Add(new ChangeCollectionItem<T>(index, removeItem));
            }

            return changes;
        }
    }
}
