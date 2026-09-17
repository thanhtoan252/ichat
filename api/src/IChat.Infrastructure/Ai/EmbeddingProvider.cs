namespace IChat.Infrastructure.Ai;

/// <summary>
/// Deliberately separate from <see cref="ChatProvider"/>: Anthropic has NO embedding API, so merging the
/// two into one enum would break on the most common configuration of all (chat = Claude, embedding = OpenAI).
/// That is why there is no Anthropic member here.
/// </summary>
public enum EmbeddingProvider
{
    OpenAI,
    AzureOpenAI,
    Google
}
