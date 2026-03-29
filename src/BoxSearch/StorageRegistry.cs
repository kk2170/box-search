using System;
using System.Collections.Generic;
using System.Linq;

namespace BoxSearch;

internal sealed class StorageRegistry
{
    private readonly object gate = new();
    private readonly Dictionary<string, StorageContainerSnapshot> snapshotsByContainerId = new(StringComparer.Ordinal);

    internal int Count
    {
        get
        {
            lock (gate)
            {
                return snapshotsByContainerId.Count;
            }
        }
    }

    internal IReadOnlyList<StorageContainerSnapshot> GetSnapshots()
    {
        lock (gate)
        {
            return snapshotsByContainerId.Values
                .OrderBy(static snapshot => snapshot.LocationHint, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    internal void Upsert(StorageContainerSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        lock (gate)
        {
            snapshotsByContainerId[snapshot.ContainerId] = snapshot;
        }
    }

    internal bool Remove(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId))
        {
            return false;
        }

        lock (gate)
        {
            return snapshotsByContainerId.Remove(containerId.Trim());
        }
    }
}
