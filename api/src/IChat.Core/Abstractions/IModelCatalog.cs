namespace IChat.Core.Abstractions;

/// <summary>Không bao giờ trả API key, kể cả đã mask.</summary>
public interface IModelCatalog
{
    ModelCatalogSnapshot GetSnapshot();

    bool IsChatModelAllowed(string model);

    /// <summary>Một số provider chỉ nhận một khối system; khi false thì PromptBuilder gộp lại.</summary>
    bool SupportsMultipleSystemMessages { get; }

    /// <summary>Timeout cho một lượt sinh câu trả lời, tính bằng giây.</summary>
    int ChatTimeoutSeconds { get; }

    int ChatMaxOutputTokens { get; }

    /// <summary>null nghĩa là không gửi temperature đi; một số model chỉ nhận giá trị mặc định.</summary>
    double? ChatTemperature { get; }
}
