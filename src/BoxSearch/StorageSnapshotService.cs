using System;
using System.Collections.Generic;
using System.Linq;

namespace BoxSearch;

internal sealed class StorageSnapshotService
{
    internal StorageContainerSnapshot CreateSnapshot(StorageObservation observation)
    {
        if (observation is null)
        {
            throw new ArgumentNullException(nameof(observation));
        }

        var itemMap = new Dictionary<string, StorageItemEntry>(StringComparer.Ordinal);

        foreach (var item in observation.Items)
        {
            if (string.IsNullOrWhiteSpace(item.ItemName) || item.Count <= 0)
            {
                continue;
            }

            var normalizedItemName = SearchTextNormalizer.Normalize(item.ItemName);

            if (normalizedItemName.Length == 0)
            {
                continue;
            }

            if (itemMap.TryGetValue(normalizedItemName, out var existingItem))
            {
                itemMap[normalizedItemName] = new StorageItemEntry(
                    existingItem.ItemName,
                    normalizedItemName,
                    existingItem.Count + item.Count);
                continue;
            }

            itemMap[normalizedItemName] = new StorageItemEntry(item.ItemName.Trim(), normalizedItemName, item.Count);
        }

        var items = itemMap.Values
            .OrderBy(static item => item.ItemName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new StorageContainerSnapshot(
            observation.ContainerId,
            observation.LocationHint,
            observation.Position,
            observation.ObservedAtUtc,
            items);
    }
}
