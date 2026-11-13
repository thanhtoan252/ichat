namespace IChat.Core.UnitTests.Conversations;

using IChat.Core.Abstractions;
using IChat.Core.Domain.Conversations;
using IChat.Core.Rag;
using IChat.Core.Services.Chat;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

[TestFixture]
public class ChatTurnRecorderTests
{
    private static readonly Guid ChunkA = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid ChunkB = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
    private static readonly Guid ChunkC = Guid.Parse("00000000-0000-0000-0000-0000000000c3");

    private List<Message> _savedMessages = null!;
    private List<MessageCitation> _savedCitations = null!;
    private Mock<IApplicationDbContext> _dbContext = null!;
    private ChatTurnRecorder _recorder = null!;

    [SetUp]
    public void SetUp()
    {
        _savedMessages = [];
        _savedCitations = [];

        var messageSet = new Mock<DbSet<Message>>();
        messageSet.Setup(set => set.Add(It.IsAny<Message>())).Callback<Message>(_savedMessages.Add);

        var citationSet = new Mock<DbSet<MessageCitation>>();
        citationSet.Setup(set => set.Add(It.IsAny<MessageCitation>())).Callback<MessageCitation>(_savedCitations.Add);

        _dbContext = new Mock<IApplicationDbContext>();
        _dbContext.SetupGet(context => context.Messages).Returns(messageSet.Object);
        _dbContext.SetupGet(context => context.MessageCitations).Returns(citationSet.Object);
        _dbContext.Setup(context => context.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _recorder = new ChatTurnRecorder(_dbContext.Object, TimeProvider.System, NullLogger<ChatTurnRecorder>.Instance);
    }

    [Test]
    public async Task RecordAnswer_WritesOneCitationPerCitedSource()
    {

        var answer = Answer("Theo [1] va [2] thi dung.");
        var context = Assembled(Source(1, ChunkA), Source(2, ChunkB));

        var done = await _recorder.RecordAnswerAsync(Conversation(), answer, context, Metadata(), CancellationToken.None);

        done.Citations.Select(citation => citation.ChunkId).Should().Equal(ChunkA, ChunkB);
        _savedCitations.Should().HaveCount(2);
        _savedMessages.Should().ContainSingle();
        _dbContext.Verify(dbContext => dbContext.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task RecordAnswer_IgnoresMarkersOutsideTheContextRange()
    {

        var answer = Answer("Xem [1] va [5] va [0].");
        var context = Assembled(Source(1, ChunkA), Source(2, ChunkB));

        var done = await _recorder.RecordAnswerAsync(Conversation(), answer, context, Metadata(), CancellationToken.None);

        done.Citations.Should().ContainSingle();
        done.Citations[0].MarkerIndex.Should().Be(1);
        _savedCitations.Should().ContainSingle();
    }

    [Test]
    public async Task RecordAnswer_WritesOneRepresentativeAnchorPerSource()
    {

        var answer = Answer("Ca [1] lan [2].");
        var context = Assembled(Source(1, ChunkA, ChunkB), Source(2, ChunkB, ChunkC));

        var done = await _recorder.RecordAnswerAsync(Conversation(), answer, context, Metadata(), CancellationToken.None);

        done.Citations.Select(citation => citation.ChunkId).Should().Equal(ChunkA, ChunkB);
        _savedCitations.Should().HaveCount(2);
    }

    [Test]
    public async Task RecordAnswer_MultipleAnchorsInOneSource_UsesOneRepresentativeCitation()
    {

        var answer = Answer("Chi mot nguon [1].");
        var context = Assembled(Source(1, ChunkA, ChunkB));

        var done = await _recorder.RecordAnswerAsync(Conversation(), answer, context, Metadata(), CancellationToken.None);

        done.Citations.Should().ContainSingle();
        done.Citations[0].ChunkId.Should().Be(ChunkA);
        done.Citations.Should().OnlyContain(citation => citation.MarkerIndex == 1);
        _savedCitations.Should().ContainSingle();
    }

    [Test]
    public async Task RecordAnswer_AnswerWithoutMarkers_WritesNoCitation()
    {

        var answer = Answer("Toi khong biet.");
        var context = Assembled(Source(1, ChunkA));

        var done = await _recorder.RecordAnswerAsync(Conversation(), answer, context, Metadata(), CancellationToken.None);

        done.Citations.Should().BeEmpty();
        _savedCitations.Should().BeEmpty();
        _savedMessages.Should().ContainSingle("the answer must still be persisted even when it cites nothing");
    }

    [Test]
    public async Task RecordAnswer_FillsEveryFieldOfTheDonePayload()
    {

        var answer = new GeneratedAnswer
        {
            Text = "Theo [1].",
            Interrupted = true,
            InputTokens = 120,
            OutputTokens = 45
        };
        var metadata = new GenerationMetadata
        {
            Provider = "OpenAI",
            Model = "gpt-4o-mini",
            LatencyMs = 1234,
            RetrievalMs = 56,
            Degraded = true
        };

        var done = await _recorder.RecordAnswerAsync(
                    Conversation(),
                    answer,
                    Assembled(Source(1, ChunkA)),
                    metadata,
                    CancellationToken.None);

        done.MessageId.Should().Be(_savedMessages.Single().Id);
        done.Provider.Should().Be("OpenAI");
        done.Model.Should().Be("gpt-4o-mini");
        done.InputTokens.Should().Be(120);
        done.OutputTokens.Should().Be(45);
        done.LatencyMs.Should().Be(1234);
        done.RetrievalMs.Should().Be(56);
        done.Degraded.Should().BeTrue();
        done.Interrupted.Should().BeTrue();
    }

    [Test]
    public async Task RecordQuestion_NamesADefaultTitledConversation()
    {

        var conversation = Conversation();

        await _recorder.RecordQuestionAsync(conversation, "Timeout mac dinh la bao nhieu?", "timeout mac dinh", 42, CancellationToken.None);

        conversation.Title.Should().NotBe(ConversationTitle.Default);
        _savedMessages.Single().RewrittenQuery.Should().Be("timeout mac dinh");
        _savedMessages.Single().RetrievalMs.Should().Be(42);
    }

    [Test]
    public async Task RecordQuestion_KeepsAnAlreadyNamedConversation()
    {

        var conversation = Conversation();
        conversation.Rename("Ten da dat", DateTimeOffset.UnixEpoch);

        await _recorder.RecordQuestionAsync(conversation, "Cau hoi moi", "cau hoi moi", 1, CancellationToken.None);

        conversation.Title.Should().Be("Ten da dat");
    }

    private static Conversation Conversation()
    {
        return Domain.Conversations.Conversation.Create(Guid.CreateVersion7(), ConversationTitle.Default, DateTimeOffset.UnixEpoch);
    }

    private static GeneratedAnswer Answer(string text)
    {
        return new GeneratedAnswer { Text = text, Interrupted = false };
    }

    private static GenerationMetadata Metadata()
    {
        return new GenerationMetadata
        {
            Provider = "OpenAI",
            Model = "gpt-4o-mini",
            LatencyMs = 10,
            RetrievalMs = 5,
            Degraded = false
        };
    }

    private static AssembledContext Assembled(params ContextSource[] sources)
    {
        return new AssembledContext { Sources = sources, RenderedContext = "context" };
    }

    private static ContextSource Source(int index, params Guid[] anchorChunkIds)
    {
        return new ContextSource
        {
            Index = index,
            CitationChunkId = anchorChunkIds[0],
            DocumentId = Guid.Parse("00000000-0000-0000-0000-0000000000d4"),
            DocumentTitle = "Tai lieu",
            HeadingPath = "Muc > Tieu muc",
            Text = "noi dung",
            Score = 0.5
        };
    }
}
