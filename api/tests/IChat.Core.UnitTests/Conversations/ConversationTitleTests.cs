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

var isDefault = ConversationTitle.IsDefault(title);

isDefault.Should().BeTrue();
    }

[Test]
    public void IsDefault_TitleChosenByTheCallerIsNotAPlaceholder()
    {

const string title = "Cấu hình hệ thống";

var isDefault = ConversationTitle.IsDefault(title);

isDefault.Should().BeFalse();
    }

[Test]
    public void FromQuestion_CollapsesWhitespace()
    {

const string question = "  timeout   mặc\n định là  bao nhiêu ";

var title = ConversationTitle.FromQuestion(question);

title.Should().Be("timeout mặc định là bao nhiêu");
    }

[Test]
    public void FromQuestion_LongQuestionIsCutToSixtyCharacters()
    {

var question = new string('a', 200);

var title = ConversationTitle.FromQuestion(question);

title.Should().HaveLength(58);
        title.Should().EndWith("…");
    }

[Test]
    public void FromQuestion_QuestionOfExactlySixtyCharactersIsKeptWhole()
    {

var question = new string('a', 60);

var title = ConversationTitle.FromQuestion(question);

title.Should().Be(question);
    }

[Test]
    public void FromQuestion_DoesNotCutASurrogatePairInHalf()
    {

var question = $"{new string('a', 56)}😀{new string('b', 60)}";

var title = ConversationTitle.FromQuestion(question);

char.IsSurrogate(title[^2]).Should().BeFalse();
        title.Should().Be($"{new string('a', 56)}…");
    }

[Test]
    public void FromQuestion_WhitespaceOnlyFallsBackToThePlaceholder()
    {

const string question = "   ";

var title = ConversationTitle.FromQuestion(question);

title.Should().Be(ConversationTitle.Default);
    }
}
