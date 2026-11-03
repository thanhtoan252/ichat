namespace IChat.Core.UnitTests.Search;

using IChat.Infrastructure.Search;
using FluentAssertions;
using NUnit.Framework;

[TestFixture]
public class TsQueryBuilderTests
{
    [Test]
    public void Build_LongQuestion_ProducesOrQuery_NotAnd()
    {

const string question = "làm thế nào để tôi cấu hình biến môi trường cho ứng dụng trong file cấu hình vậy";

var tsquery = TsQueryBuilder.Build(question);

tsquery.Should().NotBeNull();
        tsquery.Should().Contain(" | ", "plainto_tsquery joins with &, so a long question would match nothing");
        tsquery.Should().NotContain("&");
    }

[Test]
    public void Build_FifteenWordQuestion_KeepsEveryContentLexeme()
    {

const string question = "cấu hình biến môi trường ứng dụng";

var tsquery = TsQueryBuilder.Build(question);

tsquery.Should().Contain("cau").And.Contain("hinh").And.Contain("bien").And.Contain("moi").And.Contain("truong");
    }

[Test]
    public void Build_FiltersStopWords()
    {

const string question = "cấu hình của hệ thống là gì và các bước cho việc này";
        string[] stopWords = ["cua", "la", "va", "cac", "cho"];

var tsquery = TsQueryBuilder.Build(question);

tsquery.Should().NotBeNull();
        foreach (var stopWord in stopWords)
        {
            tsquery.Should().NotMatchRegex($@"(^|\|\s*){stopWord}(\s*\||$)", $"'{stopWord}' is a stopword; keeping it would match every chunk");
        }
    }

[Test]
    public void Build_QueryOfOnlyStopWords_ReturnsNull()
    {

const string question = "của là và các một cho với";

var tsquery = TsQueryBuilder.Build(question);

tsquery.Should().BeNull("return null rather than forcing ''::tsquery");
    }

[Test]
    public void Build_SpecialCharacters_DoNotBreakSyntax()
    {

const string question = "cấu hình @!#$%^&*()':\" | & ! ( ) <-> biến môi trường";

var tsquery = TsQueryBuilder.Build(question);

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

var tsquery = TsQueryBuilder.Build(query);

tsquery.Should().BeNull();
    }

[Test]
    public void Build_SingleCharacterTokens_AreDropped()
    {

const string question = "a b c d e";

var tsquery = TsQueryBuilder.Build(question);

tsquery.Should().BeNull("single-character lexemes are dropped");
    }

[Test]
    public void Build_RemovesVietnameseDiacritics_MatchingUnaccent()
    {

const string question = "đặt cấu hình";

var tsquery = TsQueryBuilder.Build(question);

tsquery.Should().Contain("dat", "PostgreSQL unaccent maps đ to d");
        tsquery.Should().NotContain("đ");
    }

[Test]
    public void Build_DeduplicatesRepeatedLexemes()
    {

const string question = "cấu hình cấu hình cấu hình";

var tsquery = TsQueryBuilder.Build(question);

tsquery!.Split(" | ").Should().OnlyHaveUniqueItems();
    }

[Test]
    public void Build_EnglishStopWordsAlsoFiltered()
    {

const string question = "what is the configuration of the system";
        string[] stopWords = ["what", "is", "the", "of"];

var tsquery = TsQueryBuilder.Build(question);

tsquery.Should().Contain("configuration");
        foreach (var stopWord in stopWords)
        {
            tsquery.Should().NotMatchRegex($@"(^|\|\s*){stopWord}(\s*\||$)");
        }
    }

[Test]
    public void Build_MixedVietnameseEnglish_KeepsBothLanguages()
    {

const string question = "cách config timeout của service";

var tsquery = TsQueryBuilder.Build(question);

tsquery.Should().Contain("cach").And.Contain("config").And.Contain("timeout").And.Contain("service");
    }

[Test]
    public void Build_NumbersArePreserved()
    {

const string question = "timeout 120 giây";

var tsquery = TsQueryBuilder.Build(question);

tsquery.Should().Contain("120");
    }
}
