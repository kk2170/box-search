namespace BoxSearch;

internal sealed class StorageItemEntry
{
    internal StorageItemEntry(string itemName, string normalizedItemName, int count)
    {
        ItemName = itemName;
        NormalizedItemName = normalizedItemName;
        Count = count;
    }

    internal string ItemName { get; }

    internal string NormalizedItemName { get; }

    internal int Count { get; }
}
