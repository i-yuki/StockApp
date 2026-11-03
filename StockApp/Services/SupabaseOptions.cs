namespace StockApp.Services;

public sealed class SupabaseOptions
{
    public string Url { get; set; } = string.Empty;

    /// <summary>公開可能なキー(publishable key)。service_role キーは絶対に設定しない。</summary>
    public string PublishableKey { get; set; } = string.Empty;
}
