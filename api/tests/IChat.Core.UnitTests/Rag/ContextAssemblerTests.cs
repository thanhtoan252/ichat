namespace IChat.Core.UnitTests.Rag;

using IChat.Core.Abstractions;
using IChat.Core.Rag;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Xunit;

public class ContextAssemblerTests
{
    private sealed class CharTokenEstimator : ITokenEstimator
    {
        public int Estimate(string? text) => text?.Length ?? 0;
    }

    private static readonly ContextAssembler Assembler = new(new CharTokenEstimator());

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

    [Fact]
    public void Assemble_EmptyInput_ReturnsEmptyContext()
    {
        var assembled = Assembler.Assemble([], 6000);

        assembled.HasContext.Should().BeFalse();
        assembled.Sources.Should().BeEmpty();
    }

    [Fact]
    public void Assemble_NumbersSourcesFromOne()
    {
        var assembled = Assembler.Assemble([Context("1", 0.9, 10), Context("2", 0.8, 10)], 6000);

        assembled.Sources.Select(source => source.Index).Should().Equal(1, 2);
    }

    [Fact]
    public void Assemble_OrdersByScoreDescending()
    {
        var assembled = Assembler.Assemble([Context("1", 0.2, 10), Context("2", 0.9, 10)], 6000);

        assembled.Sources[0].Score.Should().Be(0.9);
    }

    [Fact]
    public void Assemble_DropsWholeChunksWhenOverBudget_NeverTruncatesMidChunk()
    {
        var assembled = Assembler.Assemble(
            [Context("1", 0.9, 100), Context("2", 0.8, 100), Context("3", 0.7, 100)],
            maxTokens: 300);

        assembled.Sources.Should().HaveCount(2, "the third chunk exceeds the budget, so it must be dropped whole");
        assembled.Sources.Should().OnlyContain(source => source.Text.Length == 100);
    }

    [Fact]
    public void Assemble_AlwaysKeepsTopChunkEvenIfItAloneExceedsBudget()
    {
        var assembled = Assembler.Assemble([Context("1", 0.9, 5000)], maxTokens: 10);

        assembled.Sources.Should().ContainSingle();
    }

    [Fact]
    public void Assemble_RenderedContextContainsMarkersAndSourceLabel()
    {
        var assembled = Assembler.Assemble([Context("1", 0.9, 5)], 6000);

        assembled.RenderedContext.Should().Contain("[1]").And.Contain("Tài liệu > Mục");
    }

    [Fact]
    public void BuildAnswerPrompt_WithoutContext_UsesNoContextPrompt()
    {
        var prompt = PromptBuilder.BuildAnswerPrompt(AssembledContext.Empty, [], "câu hỏi", supportsMultipleSystemMessages: true);

        prompt[0].Text.Should().Be(PromptBuilder.NoContextSystemPrompt);
        prompt[^1].Role.Should().Be(ChatRole.User);
    }

    [Fact]
    public void BuildAnswerPrompt_MultipleSystemSupported_EmitsTwoSystemBlocks()
    {
        var assembled = Assembler.Assemble([Context("1", 0.9, 5)], 6000);

        var prompt = PromptBuilder.BuildAnswerPrompt(assembled, [], "câu hỏi", supportsMultipleSystemMessages: true);

        prompt.Count(message => message.Role == ChatRole.System).Should().Be(2);
    }

    [Fact]
    public void BuildAnswerPrompt_MultipleSystemNotSupported_MergesIntoOneBlock()
    {
        var assembled = Assembler.Assemble([Context("1", 0.9, 5)], 6000);

        var prompt = PromptBuilder.BuildAnswerPrompt(assembled, [], "câu hỏi", supportsMultipleSystemMessages: false);

        prompt.Count(message => message.Role == ChatRole.System).Should().Be(1);
        prompt[0].Text.Should().Contain("CONTEXT:").And.Contain(PromptBuilder.AnswerSystemPrompt);
    }

    [Fact]
    public void BuildAnswerPrompt_UsesOriginalQuestionNotRewritten()
    {
        var assembled = Assembler.Assemble([Context("1", 0.9, 5)], 6000);

        var prompt = PromptBuilder.BuildAnswerPrompt(assembled, [], "vậy cấu hình nó ở đâu?", supportsMultipleSystemMessages: true);

        prompt[^1].Text.Should().Be("vậy cấu hình nó ở đâu?");
    }
}
