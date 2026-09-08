namespace IChat.Core.Domain.Conversations;

public sealed class Conversation
{
    private readonly List<Message> _messages = [];

    private Conversation()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Khoá ngoại sang users; luôn lấy từ access token, không bao giờ từ client.</summary>
    public Guid UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<Message> Messages => _messages;

    public static Conversation Create(Guid userId, string title, DateTimeOffset createdAt)
    {
        return new Conversation
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Title = title,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void Rename(string title, DateTimeOffset updatedAt)
    {
        Title = title;
        UpdatedAt = updatedAt;
    }

    public void Touch(DateTimeOffset updatedAt)
    {
        UpdatedAt = updatedAt;
    }
}
