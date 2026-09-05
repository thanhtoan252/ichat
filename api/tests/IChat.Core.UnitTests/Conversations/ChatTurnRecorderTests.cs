namespace IChat.Core.UnitTests.Conversations;

using IChat.Core.Abstractions;
using IChat.Core.Domain.Conversations;
using IChat.Core.Rag;
using IChat.Core.Services.Chat;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

public class ChatTurnRecorderTests
{
    private static readonly Guid ChunkA = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
    private static readonly Guid ChunkB = Guid.Parse("00000000-0000-0000-0000-0000000000b2");
    private static readonly Guid ChunkC = Guid.Parse("00000000-0000-0000-0000-0000000000c3");

    [Fact]
    public async Task RecordAnswer_WritesOneCitationPerCitedSource()
    {
        var recorder = Recorder(out var messages, out var citations);

        var done = await recorder.RecordAnswerAsync(
            Conversation(),
            Answer("Theo [1] va [2] thi dung."),
            Assembled(Source(1, ChunkA), Source(2, ChunkB)),
            Metadata(),
            CancellationToken.None);

        done.Citations.Select(citation => citation.ChunkId).Should().Equal(ChunkA, ChunkB);
        citations.Should().HaveCount(2);
        messages.Should().ContainSingle();
    }

    // Không được tin marker model sinh ra: [5] khi context chỉ có 2 nguồn là marker bịa.
    [Fact]
    public async Task RecordAnswer_IgnoresMarkersOutsideTheContextRange()
    {
        var recorder = Recorder(out _, out var citations);

        var done = await recorder.RecordAnswerAsync(
            Conversation(),
            Answer("Xem [1] va [5] va [0]."),
            Assembled(Source(1, ChunkA), Source(2, ChunkB)),
            Metadata(),
            CancellationToken.None);

        done.Citations.Should().ContainSingle();
        done.Citations[0].MarkerIndex.Should().Be(1);
        citations.Should().ContainSingle();
    }

    // Một khối context gộp nhiều chunk neo; hai nguồn có thể chia nhau một chunk.
    [Fact]
    public async Task RecordAnswer_DeduplicatesAnchorChunksSharedBetweenSources()
    {
        var recorder = Recorder(out _, out var citations);

        var done = await recorder.RecordAnswerAsync(
            Conversation(),
            Answer("Ca [1] lan [2]."),
            Assembled(Source(1, ChunkA, ChunkB), Source(2, ChunkB, ChunkC)),
            Metadata(),
            CancellationToken.None);

        done.Citations.Select(citation => citation.ChunkId).Should().Equal(ChunkA, ChunkB, ChunkC);
        citations.Should().HaveCount(3);
    }

    [Fact]
    public async Task RecordAnswer_MultipleAnchorsInOneSource_AllBecomeCitations()
    {
        var recorder = Recorder(out _, out _);

        var done = await recorder.RecordAnswerAsync(
            Conversation(),
            Answer("Chi mot nguon [1]."),
            Assembled(Source(1, ChunkA, ChunkB)),
            Metadata(),
            CancellationToken.None);

        done.Citations.Should().HaveCount(2);
        done.Citations.Should().OnlyContain(citation => citation.MarkerIndex == 1);
    }

    [Fact]
    public async Task RecordAnswer_AnswerWithoutMarkers_WritesNoCitation()
    {
        var recorder = Recorder(out var messages, out var citations);

        var done = await recorder.RecordAnswerAsync(
            Conversation(),
            Answer("Toi khong biet."),
            Assembled(Source(1, ChunkA)),
            Metadata(),
            CancellationToken.None);

        done.Citations.Should().BeEmpty();
        citations.Should().BeEmpty();
        messages.Should().ContainSingle("câu trả lời vẫn phải được lưu dù không trích dẫn gì");
    }

    [Fact]
    public async Task RecordAnswer_FillsEveryFieldOfTheDonePayload()
    {
        var recorder = Recorder(out var messages, out _);

        var done = await recorder.RecordAnswerAsync(
            Conversation(),
            new GeneratedAnswer
            {
                Text = "Theo [1].",
                Interrupted = true,
                InputTokens = 120,
                OutputTokens = 45
            },
            Assembled(Source(1, ChunkA)),
            new GenerationMetadata
            {
                Provider = "OpenAI",
                Model = "gpt-4o-mini",
                LatencyMs = 1234,
                RetrievalMs = 56,
                Degraded = true
            },
            CancellationToken.None);

        done.MessageId.Should().Be(messages.Single().Id);
        done.Provider.Should().Be("OpenAI");
        done.Model.Should().Be("gpt-4o-mini");
        done.InputTokens.Should().Be(120);
        done.OutputTokens.Should().Be(45);
        done.LatencyMs.Should().Be(1234);
        done.RetrievalMs.Should().Be(56);
        done.Degraded.Should().BeTrue();
        done.Interrupted.Should().BeTrue();
    }

    // Hội thoại tạo từ nút "New chat" chưa có tên; câu hỏi đầu tiên phải đặt tên cho nó.
    [Fact]
    public async Task RecordQuestion_NamesADefaultTitledConversation()
    {
        var recorder = Recorder(out var messages, out _);
        var conversation = Conversation();

        await recorder.RecordQuestionAsync(conversation, "Timeout mac dinh la bao nhieu?", "timeout mac dinh", 42, CancellationToken.None);

        conversation.Title.Should().NotBe(ConversationTitle.Default);
        messages.Single().RewrittenQuery.Should().Be("timeout mac dinh");
        messages.Single().RetrievalMs.Should().Be(42);
    }

    [Fact]
    public async Task RecordQuestion_KeepsAnAlreadyNamedConversation()
    {
        var recorder = Recorder(out _, out _);
        var conversation = Conversation();
        conversation.Rename("Ten da dat", DateTimeOffset.UnixEpoch);

        await recorder.RecordQuestionAsync(conversation, "Cau hoi moi", "cau hoi moi", 1, CancellationToken.None);

        conversation.Title.Should().Be("Ten da dat");
    }

    private static ChatTurnRecorder Recorder(out List<Message> messages, out List<MessageCitation> citations)
    {
        var savedMessages = new List<Message>();
        var savedCitations = new List<MessageCitation>();

        var messageSet = Substitute.For<DbSet<Message>>();
        messageSet.When(set => set.Add(Arg.Any<Message>())).Do(call => savedMessages.Add(call.Arg<Message>()));

        var citationSet = Substitute.For<DbSet<MessageCitation>>();
        citationSet.When(set => set.Add(Arg.Any<MessageCitation>())).Do(call => savedCitations.Add(call.Arg<MessageCitation>()));

        var dbContext = Substitute.For<IApplicationDbContext>();
        dbContext.Messages.Returns(messageSet);
        dbContext.MessageCitations.Returns(citationSet);
        dbContext.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        messages = savedMessages;
        citations = savedCitations;

        return new ChatTurnRecorder(dbContext, TimeProvider.System, NullLogger<ChatTurnRecorder>.Instance);
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
            AnchorChunkIds = anchorChunkIds,
            DocumentId = Guid.Parse("00000000-0000-0000-0000-0000000000d4"),
            DocumentTitle = "Tai lieu",
            HeadingPath = "Muc > Tieu muc",
            Text = "noi dung",
            Score = 0.5
        };
    }
}
