using System.Text.Json.Serialization;

internal sealed class GoldenItem
{
    public required string Tag { get; init; }

    public required string Question { get; init; }

    [JsonPropertyName("expectedAnswerContains")]
    public required List<string> ExpectedAnswerContains { get; init; }

    public List<HistoryTurn>? History { get; init; }
}
