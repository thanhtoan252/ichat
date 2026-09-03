namespace IChat.Core.Rag;

using Microsoft.Extensions.AI;

public static class PromptBuilder
{
    public const string AnswerSystemPrompt = """
        You are an assistant that answers questions from internal documents.

        Mandatory rules:
        - Answer only from the CONTEXT provided. Do not use knowledge outside the CONTEXT.
        - Every claim must carry an [n] marker pointing at the matching source in the CONTEXT.
        - If the CONTEXT is not enough, say plainly that the information was not found in the documents. Do not invent it.
        - Answer in the same language as the user's question.
        """;

    public const string NoContextSystemPrompt = """
        You are an assistant that answers questions from internal documents.
        No document relevant to this question was found.
        Tell the user plainly that you did not find the information in the documents. Never invent an answer.
        """;

    public const string QueryRewriteSystemPrompt = """
        Rewrite the last question as a standalone, self-contained question,
        replacing every pronoun and reference with the concrete noun from the history.
        Keep the original language. Return only the question, no explanation,
        no surrounding quotes.
        """;

    /// <summary>
    /// Câu hỏi đưa vào đây phải là câu hỏi GỐC của người dùng. Bản viết lại chỉ phục vụ
    /// retrieval — dùng nhầm sẽ khiến câu trả lời lệch khỏi điều người dùng thật sự hỏi.
    /// </summary>
    public static IReadOnlyList<ChatMessage> BuildAnswerPrompt(
        AssembledContext context,
        IReadOnlyList<ChatMessage> normalizedHistory,
        string originalUserQuestion,
        bool supportsMultipleSystemMessages)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(normalizedHistory);

        var messages = new List<ChatMessage>();

        if (!context.HasContext)
        {
            messages.Add(new ChatMessage(ChatRole.System, NoContextSystemPrompt));
        }
        else if (supportsMultipleSystemMessages)
        {
            messages.Add(new ChatMessage(ChatRole.System, AnswerSystemPrompt));
            messages.Add(new ChatMessage(ChatRole.System, $"CONTEXT:\n{context.RenderedContext}"));
        }
        else
        {
            messages.Add(new ChatMessage(ChatRole.System, $"{AnswerSystemPrompt}\n\nCONTEXT:\n{context.RenderedContext}"));
        }

        messages.AddRange(normalizedHistory);
        messages.Add(new ChatMessage(ChatRole.User, originalUserQuestion));

        return messages;
    }
}
