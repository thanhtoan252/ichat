namespace IChat.Core.UnitTests.Conversations;

using IChat.Core.Contracts.Conversations;
using IChat.Core.Services.Chat;
using FluentAssertions;
using Microsoft.Extensions.AI;
using NUnit.Framework;

[TestFixture]
public class AnswerBufferTests
{
    private AnswerBuffer _buffer = null!;

    [SetUp]
    public void SetUp()
    {
        _buffer = new AnswerBuffer();
    }

    [Test]
    public void ToAnswer_NothingHappened_ReturnsEmptyAndUninterrupted()
    {
        // Arrange — a buffer nothing was written to

        // Act
        var answer = _buffer.ToAnswer();

        // Assert
        answer.Text.Should().BeEmpty();
        answer.Interrupted.Should().BeFalse();
        answer.InputTokens.Should().BeNull();
        answer.OutputTokens.Should().BeNull();
    }

    [Test]
    public void AppendText_ConcatenatesEveryChunkInOrder()
    {
        // Arrange
        string[] chunks = ["Cau ", "tra ", "loi"];

        // Act
        foreach (var chunk in chunks)
        {
            _buffer.AppendText(chunk);
        }

        // Assert
        _buffer.ToAnswer().Text.Should().Be("Cau tra loi");
    }

    // Có provider trả usage ở update cuối, có provider trả rải rác qua nhiều update.
    [Test]
    public void AddUsage_SumsAcrossSeveralUpdates()
    {
        // Arrange
        var first = UsageUpdate(inputTokens: 10, outputTokens: 3);
        var second = UsageUpdate(inputTokens: 5, outputTokens: 7);

        // Act
        _buffer.AddUsage(first);
        _buffer.AddUsage(second);

        // Assert
        var answer = _buffer.ToAnswer();
        answer.InputTokens.Should().Be(15);
        answer.OutputTokens.Should().Be(10);
    }

    [Test]
    public void AddUsage_UpdateWithoutUsageContent_LeavesTokensUnknown()
    {
        // Arrange
        var update = new ChatResponseUpdate(ChatRole.Assistant, "xin chao");

        // Act
        _buffer.AddUsage(update);

        // Assert
        _buffer.ToAnswer().InputTokens.Should().BeNull();
    }

    [Test]
    public void MarkInterrupted_AppendsTheMarkerOnce()
    {
        // Arrange
        _buffer.AppendText("Mot phan cau tra loi");

        // Act
        _buffer.MarkInterrupted();

        // Assert
        _buffer.ToAnswer().Text.Should().Be("Mot phan cau tra loi\n\n[interrupted]");
    }

    // ToAnswer có thể được gọi lại, và ngắt hai lần vẫn chỉ là một lần ngắt.
    [Test]
    public void MarkInterrupted_CalledTwice_StillAppendsTheMarkerOnlyOnce()
    {
        // Arrange
        _buffer.AppendText("Mot phan");

        // Act
        _buffer.MarkInterrupted();
        _buffer.MarkInterrupted();
        _buffer.ToAnswer();

        // Assert
        _buffer.ToAnswer().Text.Should().Be("Mot phan\n\n[interrupted]");
    }

    [Test]
    public void MarkFailed_SetsBothTheErrorAndTheInterruptedFlag()
    {
        // Arrange
        _buffer.AppendText("Mot phan");
        var error = new ErrorPayload { Code = SseErrorCode.External, Message = "The AI provider failed while generating the answer." };

        // Act
        _buffer.MarkFailed(error);

        // Assert
        _buffer.Error.Should().BeSameAs(error);

        var answer = _buffer.ToAnswer();
        answer.Interrupted.Should().BeTrue();
        answer.Text.Should().EndWith("[interrupted]");
    }

    [Test]
    public void Error_IsNull_WhenNothingFailed()
    {
        // Arrange — a buffer nothing failed on

        // Act
        var error = _buffer.Error;

        // Assert
        error.Should().BeNull();
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
