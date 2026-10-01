namespace IChat.Api.IntegrationTests;

using IChat.Api.Endpoints.Conversations.V1.DTOs;
using IChat.Api.Endpoints.Documents.V1.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// [AsParameters] requires every constructor parameter to have a public property of the same name and
/// <b>the same type</b>; a type mismatch only breaks when the endpoint is built, and the compiler cannot
/// catch it. These tests bind for real from a query string to hold that constraint in place, together with
/// each endpoint's default values and limit ceiling.
/// </summary>
public class QueryBindingTests
{
    private static async Task<TQuery> BindAsync<TQuery>(string queryString)
        where TQuery : class
    {
        TQuery? bound = null;
        var factoryResult = RequestDelegateFactory.Create(([AsParameters] TQuery query) => bound = query);

        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider()
        };
        context.Request.QueryString = new QueryString(queryString);
        context.Response.Body = Stream.Null;

        await factoryResult.RequestDelegate(context);

        context.Response.StatusCode.Should().Be(
            StatusCodes.Status200OK,
            $"{typeof(TQuery).Name} must bind from '{queryString}'");

        return bound!;
    }

    [Fact]
    public async Task GetDocumentsQuery_OmittedValues_UseDefaults()
    {
        var query = await BindAsync<GetDocumentsQuery>(string.Empty);

        query.Status.Should().BeNull();
        query.Offset.Should().Be(0);
        query.Limit.Should().Be(20);
    }

    [Fact]
    public async Task GetDocumentsQuery_ReadsQueryString()
    {
        var query = await BindAsync<GetDocumentsQuery>("?status=Indexed&offset=40&limit=5");

        query.Status.Should().Be(DocumentStatusFilter.Indexed);
        query.Offset.Should().Be(40);
        query.Limit.Should().Be(5);
    }

    [Fact]
    public async Task GetDocumentChunksQuery_OmittedValues_UseDefaults()
    {
        var query = await BindAsync<GetDocumentChunksQuery>(string.Empty);

        query.Offset.Should().Be(0);
        query.Limit.Should().Be(50);
    }

    [Fact]
    public async Task GetConversationsQuery_OmittedValues_UseDefaults()
    {
        var query = await BindAsync<GetConversationsQuery>(string.Empty);

        query.Offset.Should().Be(0);
        query.Limit.Should().Be(20);
    }

    [Fact]
    public async Task GetMessagesQuery_OmittedValues_UseDefaults()
    {
        var query = await BindAsync<GetMessagesQuery>(string.Empty);

        query.Offset.Should().Be(0);
        query.Limit.Should().Be(50);
    }

    [Fact]
    public async Task GetDocumentsQuery_LimitAboveTheCap_IsClampedToTheCap()
    {
        var query = await BindAsync<GetDocumentsQuery>("?limit=500");

        query.Limit.Should().Be(100);
    }

    [Fact]
    public async Task GetDocumentChunksQuery_LimitAboveTheCap_IsClampedToTheCap()
    {
        var query = await BindAsync<GetDocumentChunksQuery>("?limit=999");

        query.Limit.Should().Be(200);
    }

    [Fact]
    public async Task NonsensicalPaging_FallsBackToDefaults()
    {
        var query = await BindAsync<GetMessagesQuery>("?offset=-5&limit=0");

        query.Offset.Should().Be(0);
        query.Limit.Should().Be(50);
    }

    [Fact]
    public async Task GetMessagesQuery_ReadsQueryString()
    {
        var query = await BindAsync<GetMessagesQuery>("?offset=100&limit=25");

        query.Offset.Should().Be(100);
        query.Limit.Should().Be(25);
    }
}
