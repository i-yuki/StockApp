using System.Text.Json.Serialization;

namespace StockApp.Models;

public sealed class ItemHistoryDto
{
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("memo")]
    public string? Memo { get; set; }

    /// <summary>timestamptz をそのまま保持する。表示時にローカル時刻へ変換する。</summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
