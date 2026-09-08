namespace IChat.Api.IntegrationTests.Fakes;

using Microsoft.Extensions.AI;

/// <summary>
/// Embedding tất định sinh từ nội dung text: cùng text luôn ra cùng vector, và
/// text giống nhau về từ vựng thì gần nhau. Đủ để test retrieval mà không cần API key.
/// </summary>
public sealed class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public const int Dimensions = 1536;

    public static volatile bool ShouldFail;

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (ShouldFail)
        {
            throw new InvalidOperationException("Fake embedding provider failing on purpose.");
        }

        var embeddings = values.Select(value => new Embedding<float>(Vectorise(value))).ToList();

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    public static float[] Vectorise(string text)
    {
        var vector = new float[Dimensions];

        foreach (var token in text.ToLowerInvariant().Split([' ', '\n', '\t', '>', ',', '.'], StringSplitOptions.RemoveEmptyEntries))
        {
            var slot = (int)((uint)token.GetHashCode(StringComparison.Ordinal) % Dimensions);
            vector[slot] += 1f;
        }

        var norm = MathF.Sqrt(vector.Sum(value => value * value));

        if (norm > 0)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }
        else
        {
            vector[0] = 1f;
        }

        return vector;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
