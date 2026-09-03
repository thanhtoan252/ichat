namespace IChat.Core.UnitTests.Conversations;

using IChat.Core.Domain.Conversations;
using FluentAssertions;
using Xunit;

public class ConversationTitleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("New conversation")]
    [InlineData("  new CONVERSATION  ")]
    public void IsDefault_RecognisesThePlaceholder(string? title)
    {
        ConversationTitle.IsDefault(title).Should().BeTrue();
    }

    [Fact]
    public void IsDefault_TitleChosenByTheCallerIsNotAPlaceholder()
    {
        ConversationTitle.IsDefault("Cấu hình hệ thống").Should().BeFalse();
    }

    [Fact]
    public void FromQuestion_CollapsesWhitespace()
    {
        ConversationTitle.FromQuestion("  timeout   mặc\n định là  bao nhiêu ")
            .Should().Be("timeout mặc định là bao nhiêu");
    }

    [Fact]
    public void FromQuestion_LongQuestionIsCutToSixtyCharacters()
    {
        var title = ConversationTitle.FromQuestion(new string('a', 200));

        title.Should().HaveLength(58);
        title.Should().EndWith("…");
    }

    [Fact]
    public void FromQuestion_QuestionOfExactlySixtyCharactersIsKeptWhole()
    {
        var question = new string('a', 60);

        ConversationTitle.FromQuestion(question).Should().Be(question);
    }

    [Fact]
    public void FromQuestion_DoesNotCutASurrogatePairInHalf()
    {
        // Ký tự thứ 57-58 là một emoji: cắt giữa cặp surrogate sẽ tạo ra ký tự hỏng.
        var title = ConversationTitle.FromQuestion($"{new string('a', 56)}😀{new string('b', 60)}");

        char.IsSurrogate(title[^2]).Should().BeFalse();
        title.Should().Be($"{new string('a', 56)}…");
    }

    [Fact]
    public void FromQuestion_WhitespaceOnlyFallsBackToThePlaceholder()
    {
        ConversationTitle.FromQuestion("   ").Should().Be(ConversationTitle.Default);
    }
}
