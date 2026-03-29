namespace BoxSearch;

internal sealed class ObservedItemStack
{
    internal ObservedItemStack(string itemName, int count)
    {
        ItemName = itemName ?? string.Empty;
        Count = count;
    }

    internal string ItemName { get; }

    internal int Count { get; }
}
