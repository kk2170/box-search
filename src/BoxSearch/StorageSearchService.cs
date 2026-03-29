using System;
using System.Collections.Generic;
using System.Linq;

namespace BoxSearch;

internal sealed class StorageSearchService
{
    private readonly StorageRegistry registry;

    internal StorageSearchService(StorageRegistry registry)
    {
        this.registry = registry;
    }

    internal IReadOnlyList<SearchResult> Search(string query)
    {
        var normalizedQuery = SearchTextNormalizer.Normalize(query);

        if (normalizedQuery.Length == 0)
        {
            return Array.Empty<SearchResult>();
        }

        var results = new List<SearchResult>();

        foreach (var snapshot in registry.GetSnapshots())
        {
            var matches = snapshot.Items
                .Where(item => item.NormalizedItemName.Contains(normalizedQuery, StringComparison.Ordinal))
                .Select(static item => new SearchItemMatch(item.ItemName, item.Count))
                .ToArray();

            if (matches.Length == 0)
            {
                continue;
            }

            results.Add(new SearchResult(
                snapshot.ContainerId,
                snapshot.LocationHint,
                snapshot.Position,
                snapshot.ObservedAtUtc,
                matches));
        }

        return results
            .OrderByDescending(static result => result.TotalMatchingItemCount)
            .ThenBy(static result => result.LocationHint, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
