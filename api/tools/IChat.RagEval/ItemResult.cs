internal sealed class ItemResult
{
    public required string Tag { get; init; }

    public required bool Found { get; init; }

    public required int Rank { get; init; }

    public required bool FullTextEmpty { get; init; }
}
