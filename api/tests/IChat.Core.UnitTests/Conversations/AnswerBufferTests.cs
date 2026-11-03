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

var answer = _buffer.ToAnswer();

answer.Text.Should().BeEmpty();
        answer.Interrupted.Should().BeFalse();
        answer.InputTokens.Should().BeNull();
        answer.OutputTokens.Should().BeNull();
    }

[Test]
    public void AppendText_ConcatenatesEveryChunkInOrder()
    {

string[] chunks = ["Cau ", "tra ", "loi"];

foreach (var chunk in chunks)
        {
            _buffer.AppendText(chunk);
        }

_buffer.ToAnswer().Text.Should().Be("Cau tra loi");
    }

[Test]
    public void AddUsage_SumsAcrossSeveralUpdates()
    {

var first = UsageUpdate(inputTokens: 10, outputTokens: 3);
        var second = UsageUpdate(inputTokens: 5, outputTokens: 7);

_buffer.AddUsage(first);
        _buffer.AddUsage(second);

var answer = _buffer.ToAnswer();
        answer.InputTokens.Should().Be(15);
        answer.OutputTokens.Should().Be(10);
    }

[Test]
    public void AddUsage_UpdateWithoutUsageContent_LeavesTokensUnknown()
    {

var update = new ChatResponseUpdate(ChatRole.Assistant, "xin chao");

_buffer.AddUsage(update);

_buffer.ToAnswer().InputTokens.Should().BeNull();
    }

[Test]
    public void MarkInterrupted_AppendsTheMarkerOnce()
    {

_buffer.AppendText("Mot phan cau tra loi");

_buffer.MarkInterrupted();

_buffer.ToAnswer().Text.Should().Be("Mot phan cau tra loi\n\n[interrupted]");
    }

[Test]
    public void MarkInterrupted_CalledTwice_StillAppendsTheMarkerOnlyOnce()
    {

_buffer.AppendText("Mot phan");

_buffer.MarkInterrupted();
        _buffer.MarkInterrupted();
        _buffer.ToAnswer();

_buffer.ToAnswer().Text.Should().Be("Mot phan\n\n[interrupted]");
    }

[Test]
    public void MarkFailed_SetsBothTheErrorAndTheInterruptedFlag()
    {

_buffer.AppendText("Mot phan");
        var error = new ErrorPayload { Code = SseErrorCode.External, Message = "The AI provider failed while generating the answer." };

_buffer.MarkFailed(error);

_buffer.Error.Should().BeSameAs(error);

var answer = _buffer.ToAnswer();
        answer.Interrupted.Should().BeTrue();
        answer.Text.Should().EndWith("[interrupted]");
    }

[Test]
    public void Error_IsNull_WhenNothingFailed()
    {

var error = _buffer.Error;

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
