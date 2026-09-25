namespace ProjectX.V2.DaoLayer.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Provides extension methods to synchronize an existing in-memory collection with an incoming sequence of items.
/// </summary>
public static class CollectionSynchronizerExtensions
{
    /// <summary>
    /// Synchronizes <paramref name="existingCollection"/> to match the items in <paramref name="incomingCollection"/> using a key selector.
    /// The method:
    /// - Updates existing items when their key is present in both collections by invoking <paramref name="updateExisting"/> (if provided).
    /// - Prepares and adds new items present in <paramref name="incomingCollection"/> but not in <paramref name="existingCollection"/> by invoking <paramref name="prepareNew"/> (if provided) before adding.
    /// - Removes items from <paramref name="existingCollection"/> whose keys are not present in <paramref name="incomingCollection"/> when <paramref name="removeMissing"/> is <c>true</c>, optionally invoking <paramref name="removeExisting"/> before removal.
    /// </summary>
    /// <typeparam name="TChild">Type of the items contained in the collections.</typeparam>
    /// <typeparam name="TKey">Type of the key used to identify items. Must be non-nullable.</typeparam>
    /// <param name="existingCollection">
    /// The target collection that will be synchronized. Cannot be <c>null</c>.
    /// </param>
    /// <param name="incomingCollection">
    /// The source sequence containing the desired set of items. Cannot be <c>null</c>.
    /// </param>
    /// <param name="keySelector">
    /// A function that returns a unique key for an item. Keys are used to match items between collections. Cannot be <c>null</c>.
    /// </param>
    /// <param name="updateExisting">
    /// Optional action invoked when an item with a matching key exists in <paramref name="existingCollection"/>.
    /// The action receives the existing item and the incoming item (existing, incoming) so callers can copy values or merge state.
    /// </param>
    /// <param name="prepareNew">
    /// Optional action invoked for items that are new (present in <paramref name="incomingCollection"/> but not in <paramref name="existingCollection"/>).
    /// Use this to initialize navigation properties, set identity fields, or otherwise prepare the item before it is added to <paramref name="existingCollection"/>.
    /// </param>
    /// <param name="removeExisting">
    /// Optional action invoked for items that will be removed from <paramref name="existingCollection"/> because their keys are missing from <paramref name="incomingCollection"/>.
    /// Use this to perform cleanup before removal (e.g., detach events, clear relationships).
    /// </param>
    /// <param name="removeMissing">
    /// When <c>true</c> (default), items in <paramref name="existingCollection"/> that do not appear in <paramref name="incomingCollection"/> will be removed.
    /// When <c>false</c>, missing items are left untouched.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="existingCollection"/>, <paramref name="incomingCollection"/>, or <paramref name="keySelector"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// Complexity: O(n) on average for hash-based lookups, where n is the number of items in the incoming and existing sequences.
    /// - The method uses the keys produced by <paramref name="keySelector"/> to match items; callers should ensure keys are unique within each collection.
    /// - New items from <paramref name="incomingCollection"/> are added directly to <paramref name="existingCollection"/> (no cloning).
    /// - The method iterates over a snapshot of the existing collection when removing items to avoid modifying a collection while enumerating it.
    /// - The default behavior is to remove missing items; set <paramref name="removeMissing"/> to <c>false</c> to preserve existing items that are not present in the incoming sequence.
    /// </remarks>
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
            {
                prepareNew?.Invoke(incoming);
                existingCollection.Add(incoming);
            }
        }

        if (!removeMissing)
            return;

        foreach (TChild existing in existingCollection.ToList())
        {
            TKey key = keySelector(existing);

            if (!incomingKeys.Contains(key))
            {
                removeExisting?.Invoke(existing);
                existingCollection.Remove(existing);
            }
        }
    }
}
