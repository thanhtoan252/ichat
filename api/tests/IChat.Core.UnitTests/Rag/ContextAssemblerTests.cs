namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Moq;
using NUnit.Framework;

[TestFixture]
public class ContextAssemblerTests
{
    private ContextAssembler _assembler = null!;

    [SetUp]
    public void SetUp()
    {
        // Một ký tự = một token: budget trong test đọc thẳng ra độ dài chuỗi.
        var tokenEstimator = new Mock<ITokenEstimator>();
        tokenEstimator.Setup(estimator => estimator.Estimate(It.IsAny<string?>())).Returns((string? text) => text?.Length ?? 0);

        _assembler = new ContextAssembler(tokenEstimator.Object);
    }

    [Test]
    public void Assemble_EmptyInput_ReturnsEmptyContext()
    {
        // Arrange — no expanded context at all

        // Act
        var assembled = _assembler.Assemble([], 6000);

        // Assert
        assembled.HasContext.Should().BeFalse();
        assembled.Sources.Should().BeEmpty();
    }

    [Test]
    public void Assemble_NumbersSourcesFromOne()
    {
        // Arrange
        var contexts = new[] { Context("1", 0.9, 10), Context("2", 0.8, 10) };

        // Act
        var assembled = _assembler.Assemble(contexts, 6000);

        // Assert
        assembled.Sources.Select(source => source.Index).Should().Equal(1, 2);
    }

    [Test]
    public void Assemble_OrdersByScoreDescending()
    {
        // Arrange
        var contexts = new[] { Context("1", 0.2, 10), Context("2", 0.9, 10) };

        // Act
        var assembled = _assembler.Assemble(contexts, 6000);

        // Assert
        assembled.Sources[0].Score.Should().Be(0.9);
    }

    [Test]
    public void Assemble_DropsWholeChunksWhenOverBudget_NeverTruncatesMidChunk()
    {
        // Arrange
        var contexts = new[] { Context("1", 0.9, 100), Context("2", 0.8, 100), Context("3", 0.7, 100) };

        // Act
        var assembled = _assembler.Assemble(contexts, maxTokens: 300);

        // Assert
        assembled.Sources.Should().HaveCount(2, "the third chunk exceeds the budget, so it must be dropped whole");
        assembled.Sources.Should().OnlyContain(source => source.Text.Length == 100);
    }

    [Test]
    public void Assemble_AlwaysKeepsTopChunkEvenIfItAloneExceedsBudget()
    {
        // Arrange
        var contexts = new[] { Context("1", 0.9, 5000) };

        // Act
        var assembled = _assembler.Assemble(contexts, maxTokens: 10);

        // Assert
        assembled.Sources.Should().ContainSingle();
    }

    [Test]
    public void Assemble_RenderedContextContainsMarkersAndSourceLabel()
    {
        // Arrange
        var contexts = new[] { Context("1", 0.9, 5) };

        // Act
        var assembled = _assembler.Assemble(contexts, 6000);

        // Assert
        assembled.RenderedContext.Should().Contain("[1]").And.Contain("Tài liệu > Mục");
    }

    [Test]
    public void BuildAnswerPrompt_WithoutContext_UsesNoContextPrompt()
    {
        // Arrange
        var assembled = AssembledContext.Empty;

        // Act
        var prompt = PromptBuilder.BuildAnswerPrompt(assembled, [], "câu hỏi", supportsMultipleSystemMessages: true);

        // Assert
        prompt[0].Text.Should().Be(PromptBuilder.NoContextSystemPrompt);
        prompt[^1].Role.Should().Be(ChatRole.User);
    }

    [Test]
    public void BuildAnswerPrompt_MultipleSystemSupported_EmitsTwoSystemBlocks()
    {
        // Arrange
        var assembled = _assembler.Assemble([Context("1", 0.9, 5)], 6000);

        // Act
        var prompt = PromptBuilder.BuildAnswerPrompt(assembled, [], "câu hỏi", supportsMultipleSystemMessages: true);

        // Assert
        prompt.Count(message => message.Role == ChatRole.System).Should().Be(2);
    }

    [Test]
    public void BuildAnswerPrompt_MultipleSystemNotSupported_MergesIntoOneBlock()
    {
        // Arrange
        var assembled = _assembler.Assemble([Context("1", 0.9, 5)], 6000);

        // Act
        var prompt = PromptBuilder.BuildAnswerPrompt(assembled, [], "câu hỏi", supportsMultipleSystemMessages: false);

        // Assert
        prompt.Count(message => message.Role == ChatRole.System).Should().Be(1);
        prompt[0].Text.Should().Contain("CONTEXT:").And.Contain(PromptBuilder.AnswerSystemPrompt);
    }

    [Test]
    public void BuildAnswerPrompt_UsesOriginalQuestionNotRewritten()
    {
        // Arrange
        var assembled = _assembler.Assemble([Context("1", 0.9, 5)], 6000);
        const string originalQuestion = "vậy cấu hình nó ở đâu?";

        // Act
        var prompt = PromptBuilder.BuildAnswerPrompt(assembled, [], originalQuestion, supportsMultipleSystemMessages: true);

        // Assert
        prompt[^1].Text.Should().Be(originalQuestion);
    }

    private static ExpandedContext Context(string id, double score, int textLength)
    {
        var guid = Guid.Parse($"00000000-0000-0000-0000-{id.PadLeft(12, '0')}");

        return new ExpandedContext
        {
            AnchorChunkIds = [guid],
            DocumentId = guid,
            DocumentTitle = "Tài liệu",
            HeadingPath = "Mục",
            Text = new string('x', textLength),
            Score = score,
            StartChunkIndex = 0,
            EndChunkIndex = 0
        };
    }
}
