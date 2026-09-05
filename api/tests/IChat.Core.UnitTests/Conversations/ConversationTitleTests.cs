namespace IChat.Core.UnitTests.Conversations;

using IChat.Core.Domain.Conversations;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class ConversationTitleTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("New conversation")]
    [TestCase("  new CONVERSATION  ")]
    public void IsDefault_RecognisesThePlaceholder(string? title)
    {
        // Arrange & Act
        var isDefault = ConversationTitle.IsDefault(title);

        // Assert
        isDefault.Should().BeTrue();
    }

    [Test]
    public void IsDefault_TitleChosenByTheCallerIsNotAPlaceholder()
    {
        // Arrange
        const string title = "Cấu hình hệ thống";

        // Act
        var isDefault = ConversationTitle.IsDefault(title);

        // Assert
        isDefault.Should().BeFalse();
    }

    [Test]
    public void FromQuestion_CollapsesWhitespace()
    {
        // Arrange
        const string question = "  timeout   mặc\n định là  bao nhiêu ";

        // Act
        var title = ConversationTitle.FromQuestion(question);

        // Assert
        title.Should().Be("timeout mặc định là bao nhiêu");
    }

    [Test]
    public void FromQuestion_LongQuestionIsCutToSixtyCharacters()
    {
        // Arrange
        var question = new string('a', 200);

        // Act
        var title = ConversationTitle.FromQuestion(question);

        // Assert
        title.Should().HaveLength(58);
        title.Should().EndWith("…");
    }

    [Test]
    public void FromQuestion_QuestionOfExactlySixtyCharactersIsKeptWhole()
    {
        // Arrange
        var question = new string('a', 60);

        // Act
        var title = ConversationTitle.FromQuestion(question);

        // Assert
        title.Should().Be(question);
    }

    [Test]
    public void FromQuestion_DoesNotCutASurrogatePairInHalf()
    {
        // Arrange
        // Ký tự thứ 57-58 là một emoji: cắt giữa cặp surrogate sẽ tạo ra ký tự hỏng.
        var question = $"{new string('a', 56)}😀{new string('b', 60)}";

        // Act
        var title = ConversationTitle.FromQuestion(question);

        // Assert
        char.IsSurrogate(title[^2]).Should().BeFalse();
        title.Should().Be($"{new string('a', 56)}…");
    }

    [Test]
    public void FromQuestion_WhitespaceOnlyFallsBackToThePlaceholder()
    {
        // Arrange
        const string question = "   ";

        // Act
        var title = ConversationTitle.FromQuestion(question);

        // Assert
        title.Should().Be(ConversationTitle.Default);
    }
}
