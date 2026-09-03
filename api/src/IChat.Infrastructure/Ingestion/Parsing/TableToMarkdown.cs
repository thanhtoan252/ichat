namespace IChat.Infrastructure.Ingestion.Parsing;

using System.Text;

/// <summary>
/// Bảng là ĐƠN VỊ NGUYÊN TỬ: nửa bảng không có header cột thì vô nghĩa với cả
/// embedding lẫn LLM. Dùng chung cho docx và pdf.
/// </summary>
public static class TableToMarkdown
{
    public static string Render(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Count == 0)
        {
            return string.Empty;
        }

        var columnCount = rows.Max(row => row.Count);

        if (columnCount == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        AppendRow(builder, rows[0], columnCount);
        builder.Append("| ").AppendJoin(" | ", Enumerable.Repeat("---", columnCount)).AppendLine(" |");

        for (var i = 1; i < rows.Count; i++)
        {
            AppendRow(builder, rows[i], columnCount);
        }

        return builder.ToString().TrimEnd();
    }

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string> row, int columnCount)
    {
        builder.Append("| ");

        for (var column = 0; column < columnCount; column++)
        {
            // Ô gộp (merged cell) làm hàng ngắn hơn số cột: đệm rỗng để bảng không lệch.
            var cell = column < row.Count ? Sanitize(row[column]) : string.Empty;
            builder.Append(cell);
            builder.Append(column == columnCount - 1 ? " |" : " | ");
        }

        builder.AppendLine();
    }

    private static string Sanitize(string? cell)
    {
        if (string.IsNullOrWhiteSpace(cell))
        {
            return string.Empty;
        }

        return cell.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ')
            .Trim();
    }
}
