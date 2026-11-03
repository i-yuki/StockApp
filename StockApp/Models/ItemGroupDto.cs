using System.Text.Json.Serialization;

namespace StockApp.Models;

public sealed class ItemGroupDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
