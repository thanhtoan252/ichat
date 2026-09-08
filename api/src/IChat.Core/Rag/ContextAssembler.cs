namespace IChat.Core.Rag;

using System.Text;
using IChat.Core.Abstractions;

public sealed class ContextAssembler(ITokenEstimator tokenEstimator)
{
    private readonly ITokenEstimator _tokenEstimator = tokenEstimator;

    /// <summary>
    /// Cắt theo ngân sách token từ chunk rank thấp nhất trở lên, và luôn cắt TRỌN chunk —
    /// nửa chunk không có ngữ cảnh thì vô dụng với cả embedding lẫn LLM.
    /// </summary>
    public AssembledContext Assemble(IReadOnlyList<ExpandedContext> contexts, int maxTokens)
    {
        ArgumentNullException.ThrowIfNull(contexts);

        if (contexts.Count == 0)
        {
            return AssembledContext.Empty;
        }

        var ordered = contexts.OrderByDescending(context => context.Score).ToList();
        var kept = new List<ExpandedContext>();
        var usedTokens = 0;

        foreach (var context in ordered)
        {
            var rendered = RenderOne(kept.Count + 1, context);
            var cost = _tokenEstimator.Estimate(rendered);

            if (kept.Count > 0 && usedTokens + cost > maxTokens)
            {
                continue;
            }

            kept.Add(context);
            usedTokens += cost;
        }

        var sources = new List<ContextSource>(kept.Count);
        var builder = new StringBuilder();

        for (var i = 0; i < kept.Count; i++)
        {
            var context = kept[i];
            var index = i + 1;

            sources.Add(new ContextSource
            {
                Index = index,
                AnchorChunkIds = context.AnchorChunkIds,
                DocumentId = context.DocumentId,
                DocumentTitle = context.DocumentTitle,
                HeadingPath = context.HeadingPath,
                Text = context.Text,
                Score = context.Score
            });

            builder.Append(RenderOne(index, context));
        }

        return new AssembledContext
        {
            Sources = sources,
            RenderedContext = builder.ToString().TrimEnd()
        };
    }

    private static string RenderOne(int index, ExpandedContext context)
    {
        var source = string.IsNullOrWhiteSpace(context.HeadingPath)
            ? context.DocumentTitle
            : $"{context.DocumentTitle} > {context.HeadingPath}";

        return $"[{index}] (source: {source})\n{context.Text}\n\n";
    }
}
