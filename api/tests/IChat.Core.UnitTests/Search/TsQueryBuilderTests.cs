namespace IChat.Core.UnitTests.Search;

using IChat.Infrastructure.Search;
using FluentAssertions;
using NUnit.Framework;

/// <summary>
/// Bốn test cuối là bắt buộc theo spec: chúng chặn đúng lỗi khiến nhánh full-text
/// im lặng trả rỗng gần như mọi lúc.
/// </summary>
[TestFixture]
public class TsQueryBuilderTests
{
    [Test]
    public void Build_LongQuestion_ProducesOrQuery_NotAnd()
    {
        // Arrange
        const string question = "làm thế nào để tôi cấu hình biến môi trường cho ứng dụng trong file cấu hình vậy";

        // Act
        var tsquery = TsQueryBuilder.Build(question);

        // Assert
        tsquery.Should().NotBeNull();
        tsquery.Should().Contain(" | ", "plainto_tsquery joins with &, so a long question would match nothing");
        tsquery.Should().NotContain("&");
    }

    [Test]
    public void Build_FifteenWordQuestion_KeepsEveryContentLexeme()
    {
        // Arrange
        const string question = "cấu hình biến môi trường ứng dụng";

        // Act
        var tsquery = TsQueryBuilder.Build(question);

        // Assert
        tsquery.Should().Contain("cau").And.Contain("hinh").And.Contain("bien").And.Contain("moi").And.Contain("truong");
    }

    [Test]
    public void Build_FiltersStopWords()
    {
        // Arrange
        const string question = "cấu hình của hệ thống là gì và các bước cho việc này";
        string[] stopWords = ["cua", "la", "va", "cac", "cho"];

        // Act
        var tsquery = TsQueryBuilder.Build(question);

        // Assert
        tsquery.Should().NotBeNull();
        foreach (var stopWord in stopWords)
        {
            tsquery.Should().NotMatchRegex($@"(^|\|\s*){stopWord}(\s*\||$)", $"'{stopWord}' is a stopword; keeping it would match every chunk");
        }
    }

    [Test]
    public void Build_QueryOfOnlyStopWords_ReturnsNull()
    {
        // Arrange
        const string question = "của là và các một cho với";

        // Act
        var tsquery = TsQueryBuilder.Build(question);

        // Assert
        tsquery.Should().BeNull("return null rather than forcing ''::tsquery");
    }

    [Test]
    public void Build_SpecialCharacters_DoNotBreakSyntax()
    {
        // Arrange
        const string question = "cấu hình @!#$%^&*()':\" | & ! ( ) <-> biến môi trường";

        // Act
        var tsquery = TsQueryBuilder.Build(question);

        // Assert
        tsquery.Should().NotBeNull();
        tsquery.Should().NotContain("'");
        tsquery.Should().NotContain("!");
        tsquery.Should().NotContain("(");
        tsquery.Should().NotContain(")");
        tsquery.Should().Contain("cau");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Build_EmptyQuery_ReturnsNull(string? query)
    {
        // Arrange & Act
        var tsquery = TsQueryBuilder.Build(query);

        // Assert
        tsquery.Should().BeNull();
    }

    [Test]
    public void Build_SingleCharacterTokens_AreDropped()
    {
        // Arrange
        const string question = "a b c d e";

        // Act
        var tsquery = TsQueryBuilder.Build(question);

        // Assert
        tsquery.Should().BeNull("single-character lexemes are dropped");
    }

    [Test]
    public void Build_RemovesVietnameseDiacritics_MatchingUnaccent()
    {
        // Arrange
        const string question = "đặt cấu hình";

        // Act
        var tsquery = TsQueryBuilder.Build(question);

        // Assert
        tsquery.Should().Contain("dat", "PostgreSQL unaccent maps đ to d");
        tsquery.Should().NotContain("đ");
    }

    [Test]
    public void Build_DeduplicatesRepeatedLexemes()
    {
        // Arrange
        const string question = "cấu hình cấu hình cấu hình";

        // Act
        var tsquery = TsQueryBuilder.Build(question);

        // Assert
        tsquery!.Split(" | ").Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void Build_EnglishStopWordsAlsoFiltered()
    {
        // Arrange
        const string question = "what is the configuration of the system";
        string[] stopWords = ["what", "is", "the", "of"];

        // Act
        var tsquery = TsQueryBuilder.Build(question);

        // Assert
        tsquery.Should().Contain("configuration");
        foreach (var stopWord in stopWords)
        {
            tsquery.Should().NotMatchRegex($@"(^|\|\s*){stopWord}(\s*\||$)");
        }
    }

    [Test]
    public void Build_MixedVietnameseEnglish_KeepsBothLanguages()
    {
        // Arrange
        const string question = "cách config timeout của service";

        // Act
        var tsquery = TsQueryBuilder.Build(question);

        // Assert
        tsquery.Should().Contain("cach").And.Contain("config").And.Contain("timeout").And.Contain("service");
    }

    [Test]
    public void Build_NumbersArePreserved()
    {
        // Arrange
        const string question = "timeout 120 giây";

        // Act
        var tsquery = TsQueryBuilder.Build(question);

        // Assert
        tsquery.Should().Contain("120");
    }
}
