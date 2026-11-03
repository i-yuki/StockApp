namespace StockApp.Services;

/// <summary>在庫一覧で選択中の商品グループを、他画面から戻ったときのために保持する。</summary>
public sealed class StockListState
{
    public long? GroupId { get; set; }
}
