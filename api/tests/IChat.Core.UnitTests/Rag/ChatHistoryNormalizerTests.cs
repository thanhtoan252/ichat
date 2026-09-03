namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Rag;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Xunit;

public class ChatHistoryNormalizerTests
{
    [Fact]
    public void Normalize_DropsEmptyMessages()
    {
        var normalized = ChatHistoryNormalizer.Normalize(
        [
            new ChatMessage(ChatRole.User, "  "),
            new ChatMessage(ChatRole.User, "câu hỏi thật")
        ]);

        normalized.Should().ContainSingle().Which.Text.Should().Be("câu hỏi thật");
    }

    [Fact]
    public void Normalize_MergesAdjacentSameRoleMessages()
    {
        var normalized = ChatHistoryNormalizer.Normalize(
        [
            new ChatMessage(ChatRole.User, "phần một"),
            new ChatMessage(ChatRole.User, "phần hai"),
            new ChatMessage(ChatRole.Assistant, "trả lời")
        ]);

        normalized.Should().HaveCount(2);
        normalized[0].Text.Should().Be("phần một\n\nphần hai");
        normalized[1].Role.Should().Be(ChatRole.Assistant);
    }

    [Fact]
    public void Normalize_FirstMessageIsAlwaysUser()
    {
        var normalized = ChatHistoryNormalizer.Normalize(
        [
            new ChatMessage(ChatRole.Assistant, "tôi nói trước"),
            new ChatMessage(ChatRole.User, "người dùng hỏi")
        ]);

        normalized.Should().ContainSingle();
        normalized[0].Role.Should().Be(ChatRole.User);
    }

    [Fact]
    public void Normalize_StripsSystemMessages()
    {
        var normalized = ChatHistoryNormalizer.Normalize(
        [
            new ChatMessage(ChatRole.System, "system cũ"),
            new ChatMessage(ChatRole.User, "hỏi")
        ]);

        normalized.Should().ContainSingle();
        normalized[0].Role.Should().Be(ChatRole.User);
    }

    [Fact]
    public void Normalize_ProducesStrictlyAlternatingRoles()
    {
        var normalized = ChatHistoryNormalizer.Normalize(
        [
            new ChatMessage(ChatRole.User, "u1"),
            new ChatMessage(ChatRole.Assistant, "a1"),
            new ChatMessage(ChatRole.Assistant, "a2"),
            new ChatMessage(ChatRole.User, "u2"),
            new ChatMessage(ChatRole.User, "u3")
        ]);

        for (var i = 1; i < normalized.Count; i++)
        {
            normalized[i].Role.Should().NotBe(normalized[i - 1].Role);
        }

        normalized[0].Role.Should().Be(ChatRole.User);
    }

    [Fact]
    public void Normalize_AllEmpty_ReturnsEmpty()
    {
        ChatHistoryNormalizer.Normalize([new ChatMessage(ChatRole.Assistant, "")]).Should().BeEmpty();
    }
}
