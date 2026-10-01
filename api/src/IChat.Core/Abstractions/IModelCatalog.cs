namespace IChat.Core.Abstractions;

/// <summary>Never returns an API key, not even a masked one.</summary>
public interface IModelCatalog
{
    ModelCatalogSnapshot GetSnapshot();

    bool IsChatModelAllowed(string model);

    /// <summary>Some providers accept only one system block; when false, PromptBuilder merges them.</summary>
    bool SupportsMultipleSystemMessages { get; }

    /// <summary>Timeout for generating one answer, in seconds.</summary>
    int ChatTimeoutSeconds { get; }

    int ChatMaxOutputTokens { get; }

    /// <summary>null means do not send a temperature at all; some models only accept their default.</summary>
    double? ChatTemperature { get; }
}
