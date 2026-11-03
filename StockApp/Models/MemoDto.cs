using System.Text.Json.Serialization;

namespace StockApp.Models;

public sealed class MemoDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("memo")]
    public string Text { get; set; } = string.Empty;
}
