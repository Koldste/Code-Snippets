namespace ProjectX.V2.DaoLayer.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;


public static class CollectionSynchronizerExtensions
{

    public static void SyncCollection<TChild, TKey>(this ICollection<TChild> existingCollection,
                                                    IEnumerable<TChild> incomingCollection,
                                                    Func<TChild, TKey> keySelector,
                                                    Action<TChild, TChild>? updateExisting = null,
                                                    Action<TChild>? prepareNew = null,
                                                    Action<TChild>? removeExisting = null,
                                                    bool removeMissing = true) where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(existingCollection);
        ArgumentNullException.ThrowIfNull(incomingCollection);
        ArgumentNullException.ThrowIfNull(keySelector);

        Dictionary<TKey, TChild> existingByKey = existingCollection.ToDictionary(keySelector);
        HashSet<TKey> incomingKeys = [];

        foreach (TChild incoming in incomingCollection)
        {
            TKey key = keySelector(incoming);

            incomingKeys.Add(key);

            if (existingByKey.TryGetValue(key, out TChild? existing))
                updateExisting?.Invoke(existing, incoming);
            else
                prepareNew?.Invoke(incoming);
                existingCollection.Add(incoming);
        }

        if (!removeMissing)
            return;

        foreach (TChild existing in existingCollection.ToList())
        {
            TKey key = keySelector(existing);

            if (!incomingKeys.Contains(key))
                removeExisting?.Invoke(existing);
                existingCollection.Remove(existing);
        }
    }
}
