namespace IChat.Core.Services.Chat;

using System.Text;
using IChat.Core.Contracts.Conversations;
using Microsoft.Extensions.AI;

/// <summary>
/// Trạng thái tích luỹ trong lúc stream. Tồn tại vì một iterator không trả về được
/// giá trị: caller đưa buffer vào, đọc kết quả ra sau khi vòng lặp kết thúc.
/// </summary>
public sealed class AnswerBuffer
{
    private const string InterruptedMarker = "\n\n[interrupted]";

    private readonly StringBuilder _text = new();

    public ErrorPayload? Error { get; private set; }

    private bool Interrupted { get; set; }

    private int? InputTokens { get; set; }

    private int? OutputTokens { get; set; }

    public void AppendText(string text)
    {
        _text.Append(text);
    }

    /// <summary>Usage có provider trả ở update cuối, có provider trả rải rác; cộng dồn an toàn.</summary>
    public void AddUsage(ChatResponseUpdate update)
    {
        foreach (var content in update.Contents.OfType<UsageContent>())
        {
            InputTokens = (InputTokens ?? 0) + (int)(content.Details.InputTokenCount ?? 0);
            OutputTokens = (OutputTokens ?? 0) + (int)(content.Details.OutputTokenCount ?? 0);
        }
    }

    public void MarkInterrupted()
    {
        Interrupted = true;
    }

    public void MarkFailed(ErrorPayload error)
    {
        Error = error;
        Interrupted = true;
    }

    public GeneratedAnswer ToAnswer()
    {
        var text = _text.ToString();

        return new GeneratedAnswer
        {
            Text = Interrupted ? text + InterruptedMarker : text,
            Interrupted = Interrupted,
            InputTokens = InputTokens,
            OutputTokens = OutputTokens
        };
    }
}
