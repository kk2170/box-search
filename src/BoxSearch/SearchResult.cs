using System;
using System.Collections.Generic;
using System.Linq;

namespace BoxSearch;

internal sealed class SearchResult
{
    internal SearchResult(
        string containerId,
        string locationHint,
        WorldPosition position,
        DateTimeOffset lastObservedUtc,
        IReadOnlyList<SearchItemMatch> matches)
    {
        ContainerId = containerId;
        LocationHint = locationHint;
        Position = position;
        LastObservedUtc = lastObservedUtc;
        Matches = matches;
        TotalMatchingItemCount = matches.Sum(static match => match.Count);
        SummaryText = string.Join(
            ", ",
            matches.Select(static match => $"{match.ItemName} x{match.Count}"));
    }

    internal string ContainerId { get; }

    internal string LocationHint { get; }

    internal WorldPosition Position { get; }

    internal DateTimeOffset LastObservedUtc { get; }

    internal IReadOnlyList<SearchItemMatch> Matches { get; }

    internal int TotalMatchingItemCount { get; }

    internal string SummaryText { get; }
}
