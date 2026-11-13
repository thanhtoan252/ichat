namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Rag;
using FluentAssertions;
using Microsoft.Extensions.AI;
using NUnit.Framework;

[TestFixture]
public class ChatHistoryNormalizerTests
{
    [Test]
    public void Normalize_DropsEmptyMessages()
    {

        ChatMessage[] history =
                [
                    new(ChatRole.User, "  "),
            new(ChatRole.User, "câu hỏi thật")
                ];

        var normalized = ChatHistoryNormalizer.Normalize(history);

        normalized.Should().ContainSingle().Which.Text.Should().Be("câu hỏi thật");
    }

    [Test]
    public void Normalize_MergesAdjacentSameRoleMessages()
    {

        ChatMessage[] history =
                [
                    new(ChatRole.User, "phần một"),
            new(ChatRole.User, "phần hai"),
            new(ChatRole.Assistant, "trả lời")
                ];

        var normalized = ChatHistoryNormalizer.Normalize(history);

        normalized.Should().HaveCount(2);
        normalized[0].Text.Should().Be("phần một\n\nphần hai");
        normalized[1].Role.Should().Be(ChatRole.Assistant);
    }

    [Test]
    public void Normalize_FirstMessageIsAlwaysUser()
    {

        ChatMessage[] history =
                [
                    new(ChatRole.Assistant, "tôi nói trước"),
            new(ChatRole.User, "người dùng hỏi")
                ];

        var normalized = ChatHistoryNormalizer.Normalize(history);

        normalized.Should().ContainSingle();
        normalized[0].Role.Should().Be(ChatRole.User);
    }

    [Test]
    public void Normalize_StripsSystemMessages()
    {

        ChatMessage[] history =
                [
                    new(ChatRole.System, "system cũ"),
            new(ChatRole.User, "hỏi")
                ];

        var normalized = ChatHistoryNormalizer.Normalize(history);

        normalized.Should().ContainSingle();
        normalized[0].Role.Should().Be(ChatRole.User);
    }

    [Test]
    public void Normalize_ProducesStrictlyAlternatingRoles()
    {

        ChatMessage[] history =
                [
                    new(ChatRole.User, "u1"),
            new(ChatRole.Assistant, "a1"),
            new(ChatRole.Assistant, "a2"),
            new(ChatRole.User, "u2"),
            new(ChatRole.User, "u3")
                ];

        var normalized = ChatHistoryNormalizer.Normalize(history);

        for (var i = 1; i < normalized.Count; i++)
        {
            normalized[i].Role.Should().NotBe(normalized[i - 1].Role);
        }

        normalized[0].Role.Should().Be(ChatRole.User);
    }

    [Test]
    public void Normalize_AllEmpty_ReturnsEmpty()
    {

        ChatMessage[] history = [new(ChatRole.Assistant, "")];

        var normalized = ChatHistoryNormalizer.Normalize(history);

        normalized.Should().BeEmpty();
    }
}
