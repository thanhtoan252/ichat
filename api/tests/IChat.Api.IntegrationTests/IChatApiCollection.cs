namespace IChat.Api.IntegrationTests;

using Xunit;

[CollectionDefinition(nameof(IChatApiCollection))]
public sealed class IChatApiCollection : ICollectionFixture<IChatApiFactory>;
