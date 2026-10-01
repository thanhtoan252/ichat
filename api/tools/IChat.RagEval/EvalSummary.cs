internal sealed class EvalSummary
{
    public required double Recall { get; init; }

    public required double Mrr { get; init; }

    public required double FullTextEmptyRate { get; init; }

    public required Dictionary<string, (double Recall, double Mrr, int Count)> ByTag { get; init; }
}
