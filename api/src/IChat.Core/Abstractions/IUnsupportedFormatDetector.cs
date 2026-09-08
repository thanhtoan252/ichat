namespace IChat.Core.Abstractions;

using IChat.Core.Common;

/// <summary>
/// Chạy trước mọi bước nhận diện: một định dạng bị từ chối CÓ CHỦ ĐÍCH phải trả về thông
/// điệp hướng dẫn, chứ không rơi vào parser rồi nổ thành 500.
/// </summary>
public interface IUnsupportedFormatDetector
{
    /// <summary>Trả về Error khi định dạng bị từ chối có chủ đích, null khi không có ý kiến.</summary>
    Error? Detect(string fileName, ReadOnlySpan<byte> header);
}
