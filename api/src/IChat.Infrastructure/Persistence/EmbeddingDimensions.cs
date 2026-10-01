namespace IChat.Infrastructure.Persistence;

/// <summary>
/// The vector dimension count is fixed at the schema level (a vector(N) column). Changing this value is a
/// breaking change in the data layer and must come with an EF migration that changes the column type plus a
/// full reindex. See the README section "Changing the embedding model".
/// </summary>
public static class EmbeddingDimensions
{
    public const int Default = 1536;

    /// <summary>Must match <see cref="Default"/>.</summary>
    public const string ColumnType = "vector(1536)";
}
