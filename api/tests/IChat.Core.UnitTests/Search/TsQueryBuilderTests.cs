namespace IChat.Core.UnitTests.Search;

using IChat.Infrastructure.Search;
using FluentAssertions;
using Xunit;

/// <summary>
/// Bốn test cuối là bắt buộc theo spec: chúng chặn đúng lỗi khiến nhánh full-text
/// im lặng trả rỗng gần như mọi lúc.
/// </summary>
public class TsQueryBuilderTests
{
    [Fact]
    public void Build_LongQuestion_ProducesOrQuery_NotAnd()
    {
        var tsquery = TsQueryBuilder.Build(
            "làm thế nào để tôi cấu hình biến môi trường cho ứng dụng trong file cấu hình vậy");

        tsquery.Should().NotBeNull();
        tsquery.Should().Contain(" | ", "plainto_tsquery joins with &, so a long question would match nothing");
        tsquery.Should().NotContain("&");
    }

    [Fact]
    public void Build_FifteenWordQuestion_KeepsEveryContentLexeme()
    {
        var tsquery = TsQueryBuilder.Build("cấu hình biến môi trường ứng dụng");

        tsquery.Should().Contain("cau").And.Contain("hinh").And.Contain("bien").And.Contain("moi").And.Contain("truong");
    }

    [Fact]
    public void Build_FiltersStopWords()
    {
        var tsquery = TsQueryBuilder.Build("cấu hình của hệ thống là gì và các bước cho việc này");

        tsquery.Should().NotBeNull();
        foreach (var stopWord in new[] { "cua", "la", "va", "cac", "cho" })
        {
            tsquery.Should().NotMatchRegex($@"(^|\|\s*){stopWord}(\s*\||$)", $"'{stopWord}' is a stopword; keeping it would match every chunk");
        }
    }

    [Fact]
    public void Build_QueryOfOnlyStopWords_ReturnsNull()
    {
        var tsquery = TsQueryBuilder.Build("của là và các một cho với");

        tsquery.Should().BeNull("return null rather than forcing ''::tsquery");
    }

    [Fact]
    public void Build_SpecialCharacters_DoNotBreakSyntax()
    {
        var tsquery = TsQueryBuilder.Build("cấu hình @!#$%^&*()':\" | & ! ( ) <-> biến môi trường");

        tsquery.Should().NotBeNull();
        tsquery.Should().NotContain("'");
        tsquery.Should().NotContain("!");
        tsquery.Should().NotContain("(");
        tsquery.Should().NotContain(")");
        tsquery.Should().Contain("cau");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_EmptyQuery_ReturnsNull(string? query)
    {
        TsQueryBuilder.Build(query).Should().BeNull();
    }

    [Fact]
    public void Build_SingleCharacterTokens_AreDropped()
    {
        TsQueryBuilder.Build("a b c d e").Should().BeNull("single-character lexemes are dropped");
    }

    [Fact]
    public void Build_RemovesVietnameseDiacritics_MatchingUnaccent()
    {
        var tsquery = TsQueryBuilder.Build("đặt cấu hình");

        tsquery.Should().Contain("dat", "PostgreSQL unaccent maps đ to d");
        tsquery.Should().NotContain("đ");
    }

    [Fact]
    public void Build_DeduplicatesRepeatedLexemes()
    {
        var tsquery = TsQueryBuilder.Build("cấu hình cấu hình cấu hình");

        tsquery!.Split(" | ").Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Build_EnglishStopWordsAlsoFiltered()
    {
        var tsquery = TsQueryBuilder.Build("what is the configuration of the system");

        tsquery.Should().Contain("configuration");
        foreach (var stopWord in new[] { "what", "is", "the", "of" })
        {
            tsquery.Should().NotMatchRegex($@"(^|\|\s*){stopWord}(\s*\||$)");
        }
    }

    [Fact]
    public void Build_MixedVietnameseEnglish_KeepsBothLanguages()
    {
        var tsquery = TsQueryBuilder.Build("cách config timeout của service");

        tsquery.Should().Contain("cach").And.Contain("config").And.Contain("timeout").And.Contain("service");
    }

    [Fact]
    public void Build_NumbersArePreserved()
    {
        var tsquery = TsQueryBuilder.Build("timeout 120 giây");

        tsquery.Should().Contain("120");
    }
}
