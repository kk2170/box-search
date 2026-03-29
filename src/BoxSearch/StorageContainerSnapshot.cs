using System;
using System.Collections.Generic;

namespace BoxSearch;

internal sealed class StorageContainerSnapshot
{
    internal StorageContainerSnapshot(
        string containerId,
        string locationHint,
        WorldPosition position,
        DateTimeOffset observedAtUtc,
        IReadOnlyList<StorageItemEntry> items)
    {
        ContainerId = containerId;
        LocationHint = locationHint;
        Position = position;
        ObservedAtUtc = observedAtUtc;
        Items = items;
    }

    internal string ContainerId { get; }

    internal string LocationHint { get; }

    internal WorldPosition Position { get; }

    internal DateTimeOffset ObservedAtUtc { get; }

    internal IReadOnlyList<StorageItemEntry> Items { get; }
}
