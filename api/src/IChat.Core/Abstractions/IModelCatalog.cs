namespace IChat.Core.Abstractions;

public interface IModelCatalog
{
    ModelCatalogSnapshot GetSnapshot();

bool IsChatModelAllowed(string model);

bool SupportsMultipleSystemMessages { get; }

int ChatTimeoutSeconds { get; }

int ChatMaxOutputTokens { get; }

double? ChatTemperature { get; }
}
