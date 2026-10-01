namespace IChat.Core.Common;

public enum ErrorType
{
    None = 0,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    External,
    Unexpected
}
