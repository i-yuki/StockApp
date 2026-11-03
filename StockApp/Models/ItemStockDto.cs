using System.Text.Json.Serialization;

namespace StockApp.Models;

public sealed class ItemStockDto
{
    [JsonPropertyName("group_id")]
    public long GroupId { get; set; }

    [JsonPropertyName("item_id")]
    public long ItemId { get; set; }

    [JsonPropertyName("item_name")]
    public string ItemName { get; set; } = string.Empty;

    [JsonPropertyName("qty")]
    public int Qty { get; set; }
}
