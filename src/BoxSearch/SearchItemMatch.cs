namespace BoxSearch;

internal sealed class SearchItemMatch
{
    internal SearchItemMatch(string itemName, int count)
    {
        ItemName = itemName;
        Count = count;
    }

    internal string ItemName { get; }

    internal int Count { get; }
}
