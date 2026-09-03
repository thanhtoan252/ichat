namespace IChat.Core.UnitTests.Conversations;

using IChat.Core.Contracts.Conversations;
using IChat.Core.Services.Chat;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Xunit;

public class AnswerBufferTests
{
    [Fact]
    public void ToAnswer_NothingHappened_ReturnsEmptyAndUninterrupted()
    {
        var answer = new AnswerBuffer().ToAnswer();

        answer.Text.Should().BeEmpty();
        answer.Interrupted.Should().BeFalse();
        answer.InputTokens.Should().BeNull();
        answer.OutputTokens.Should().BeNull();
    }

    [Fact]
    public void AppendText_ConcatenatesEveryChunkInOrder()
    {
        var buffer = new AnswerBuffer();

        buffer.AppendText("Cau ");
        buffer.AppendText("tra ");
        buffer.AppendText("loi");

        buffer.ToAnswer().Text.Should().Be("Cau tra loi");
    }

    // Có provider trả usage ở update cuối, có provider trả rải rác qua nhiều update.
    [Fact]
    public void AddUsage_SumsAcrossSeveralUpdates()
    {
        var buffer = new AnswerBuffer();

        buffer.AddUsage(UsageUpdate(inputTokens: 10, outputTokens: 3));
        buffer.AddUsage(UsageUpdate(inputTokens: 5, outputTokens: 7));

        var answer = buffer.ToAnswer();

        answer.InputTokens.Should().Be(15);
        answer.OutputTokens.Should().Be(10);
    }

    [Fact]
    public void AddUsage_UpdateWithoutUsageContent_LeavesTokensUnknown()
    {
        var buffer = new AnswerBuffer();

        buffer.AddUsage(new ChatResponseUpdate(ChatRole.Assistant, "xin chao"));

        buffer.ToAnswer().InputTokens.Should().BeNull();
    }

    [Fact]
    public void MarkInterrupted_AppendsTheMarkerOnce()
    {
        var buffer = new AnswerBuffer();
        buffer.AppendText("Mot phan cau tra loi");

        buffer.MarkInterrupted();

        buffer.ToAnswer().Text.Should().Be("Mot phan cau tra loi\n\n[interrupted]");
    }

    // ToAnswer có thể được gọi lại, và ngắt hai lần vẫn chỉ là một lần ngắt.
    [Fact]
    public void MarkInterrupted_CalledTwice_StillAppendsTheMarkerOnlyOnce()
    {
        var buffer = new AnswerBuffer();
        buffer.AppendText("Mot phan");

        buffer.MarkInterrupted();
        buffer.MarkInterrupted();
        buffer.ToAnswer();

        buffer.ToAnswer().Text.Should().Be("Mot phan\n\n[interrupted]");
    }

    [Fact]
    public void MarkFailed_SetsBothTheErrorAndTheInterruptedFlag()
    {
        var buffer = new AnswerBuffer();
        buffer.AppendText("Mot phan");

        var error = new ErrorPayload { Code = SseErrorCode.External, Message = "The AI provider failed while generating the answer." };
        buffer.MarkFailed(error);

        buffer.Error.Should().BeSameAs(error);

        var answer = buffer.ToAnswer();

        answer.Interrupted.Should().BeTrue();
        answer.Text.Should().EndWith("[interrupted]");
    }

    [Fact]
    public void Error_IsNull_WhenNothingFailed()
    {
        new AnswerBuffer().Error.Should().BeNull();
    }

    private static ChatResponseUpdate UsageUpdate(int inputTokens, int outputTokens)
    {
        var usage = new UsageContent(new UsageDetails
        {
            InputTokenCount = inputTokens,
            OutputTokenCount = outputTokens
        });

        return new ChatResponseUpdate { Contents = [usage] };
    }
}
