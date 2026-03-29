using System;
using System.Collections.Generic;
using System.Linq;

namespace BoxSearch;

internal sealed class StorageObservation
{
    internal StorageObservation(
        string containerId,
        string locationHint,
        WorldPosition position,
        DateTimeOffset observedAtUtc,
        IEnumerable<ObservedItemStack> items)
    {
        ContainerId = string.IsNullOrWhiteSpace(containerId)
            ? throw new ArgumentException("A container identifier is required.", nameof(containerId))
            : containerId.Trim();
        LocationHint = string.IsNullOrWhiteSpace(locationHint) ? ContainerId : locationHint.Trim();
        Position = position;
        ObservedAtUtc = observedAtUtc == default ? DateTimeOffset.UtcNow : observedAtUtc;
        Items = items?.Where(static item => item.Count > 0).ToArray()
            ?? throw new ArgumentNullException(nameof(items));
    }

    internal string ContainerId { get; }

    internal string LocationHint { get; }

    internal WorldPosition Position { get; }

    internal DateTimeOffset ObservedAtUtc { get; }

    internal IReadOnlyList<ObservedItemStack> Items { get; }
}
