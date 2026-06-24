using System.Collections.ObjectModel;

namespace SmoothScrollModern.Core.Presentation;

public static class CollectionSync
{
    public static void MatchOrder<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        var itemSet = items.ToHashSet();
        for (var index = collection.Count - 1; index >= 0; index--)
        {
            if (!itemSet.Contains(collection[index]))
            {
                collection.RemoveAt(index);
            }
        }

        for (var targetIndex = 0; targetIndex < items.Count; targetIndex++)
        {
            var item = items[targetIndex];
            var currentIndex = collection.IndexOf(item);
            if (currentIndex < 0)
            {
                collection.Insert(targetIndex, item);
                continue;
            }

            if (currentIndex != targetIndex)
            {
                collection.Move(currentIndex, targetIndex);
            }
        }
    }
}
