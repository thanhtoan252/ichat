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
        // Arrange
        ChatMessage[] history =
        [
            new(ChatRole.User, "  "),
            new(ChatRole.User, "câu hỏi thật")
        ];

        // Act
        var normalized = ChatHistoryNormalizer.Normalize(history);

        // Assert
        normalized.Should().ContainSingle().Which.Text.Should().Be("câu hỏi thật");
    }

    [Test]
    public void Normalize_MergesAdjacentSameRoleMessages()
    {
        // Arrange
        ChatMessage[] history =
        [
            new(ChatRole.User, "phần một"),
            new(ChatRole.User, "phần hai"),
            new(ChatRole.Assistant, "trả lời")
        ];

        // Act
        var normalized = ChatHistoryNormalizer.Normalize(history);

        // Assert
        normalized.Should().HaveCount(2);
        normalized[0].Text.Should().Be("phần một\n\nphần hai");
        normalized[1].Role.Should().Be(ChatRole.Assistant);
    }

    [Test]
    public void Normalize_FirstMessageIsAlwaysUser()
    {
        // Arrange
        ChatMessage[] history =
        [
            new(ChatRole.Assistant, "tôi nói trước"),
            new(ChatRole.User, "người dùng hỏi")
        ];

        // Act
        var normalized = ChatHistoryNormalizer.Normalize(history);

        // Assert
        normalized.Should().ContainSingle();
        normalized[0].Role.Should().Be(ChatRole.User);
    }

    [Test]
    public void Normalize_StripsSystemMessages()
    {
        // Arrange
        ChatMessage[] history =
        [
            new(ChatRole.System, "system cũ"),
            new(ChatRole.User, "hỏi")
        ];

        // Act
        var normalized = ChatHistoryNormalizer.Normalize(history);

        // Assert
        normalized.Should().ContainSingle();
        normalized[0].Role.Should().Be(ChatRole.User);
    }

    [Test]
    public void Normalize_ProducesStrictlyAlternatingRoles()
    {
        // Arrange
        ChatMessage[] history =
        [
            new(ChatRole.User, "u1"),
            new(ChatRole.Assistant, "a1"),
            new(ChatRole.Assistant, "a2"),
            new(ChatRole.User, "u2"),
            new(ChatRole.User, "u3")
        ];

        // Act
        var normalized = ChatHistoryNormalizer.Normalize(history);

        // Assert
        for (var i = 1; i < normalized.Count; i++)
        {
            normalized[i].Role.Should().NotBe(normalized[i - 1].Role);
        }

        normalized[0].Role.Should().Be(ChatRole.User);
    }

    [Test]
    public void Normalize_AllEmpty_ReturnsEmpty()
    {
        // Arrange
        ChatMessage[] history = [new(ChatRole.Assistant, "")];

        // Act
        var normalized = ChatHistoryNormalizer.Normalize(history);

        // Assert
        normalized.Should().BeEmpty();
    }
}
