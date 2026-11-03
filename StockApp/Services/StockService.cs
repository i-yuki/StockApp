using System.Net;
using System.Net.Http.Json;
using StockApp.Models;

namespace StockApp.Services;

/// <summary>Supabase REST(PostgREST)経由のデータアクセス。RLS により認証ユーザーのデータのみ扱える。</summary>
public sealed class StockService
{
    private readonly HttpClient _http;
    private readonly SupabaseOptions _options;
    private readonly AuthService _auth;

    public StockService(HttpClient http, SupabaseOptions options, AuthService auth)
    {
        _http = http;
        _options = options;
        _auth = auth;
    }

    public async Task<List<ItemGroupDto>> GetItemGroupsAsync() =>
        await GetAsync<ItemGroupDto>("item_groups?select=id,name&order=order_number.asc,id.asc");

    public async Task<List<MemoDto>> GetMemosAsync() =>
        await GetAsync<MemoDto>("memos?select=id,memo&order=order_number.asc,id.asc");

    public async Task<List<ItemStockDto>> GetItemStocksAsync(long groupId) =>
        await GetAsync<ItemStockDto>($"item_stocks?select=group_id,item_id,item_name,qty&group_id=eq.{groupId}");

    public async Task<List<ItemHistoryDto>> GetHistoryAsync(long itemId)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "rpc/item_quantity_history");
        request.Content = JsonContent.Create(new { p_item_id = itemId });
        using var response = await SendAsync(request);
        return await response.Content.ReadFromJsonAsync<List<ItemHistoryDto>>() ?? [];
    }

    /// <summary>入庫・出庫を登録する。出庫は数量を負数にして保存する(現行 WinForms 版と同じ)。</summary>
    public async Task InsertStockAsync(long itemId, StockType stockType, int quantity, string memo)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, "stocks");
        request.Headers.Add("Prefer", "return=minimal");
        request.Content = JsonContent.Create(new
        {
            item_id = itemId,
            stock_type = (int)stockType,
            quantity = stockType == StockType.Out ? -quantity : quantity,
            memo,
            lock_flag = false,
        });
        using var response = await SendAsync(request);
    }

    private async Task<List<T>> GetAsync<T>(string path)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, path);
        using var response = await SendAsync(request);
        return await response.Content.ReadFromJsonAsync<List<T>>() ?? [];
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string path)
    {
        var token = await _auth.GetAccessTokenAsync()
            ?? throw new SessionExpiredException();
        return _auth.CreateRequest(method, $"{_options.Url}/rest/v1/{path}", token);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            throw new StockServiceException("通信に失敗しました。ネットワーク接続を確認して再試行してください。");
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var status = response.StatusCode;
        response.Dispose();
        throw status switch
        {
            HttpStatusCode.Unauthorized => new SessionExpiredException(),
            HttpStatusCode.Forbidden => new StockServiceException("この操作を行う権限がありません。"),
            _ => new StockServiceException("サーバーでエラーが発生しました。時間をおいて再試行してください。"),
        };
    }
}

/// <summary>利用者にそのまま表示できるメッセージを持つ例外。</summary>
public class StockServiceException : Exception
{
    public StockServiceException(string message) : base(message)
    {
    }
}

/// <summary>未ログイン、またはセッション期限切れ。ログイン画面へ戻す。</summary>
public sealed class SessionExpiredException : StockServiceException
{
    public SessionExpiredException() : base("セッションの有効期限が切れました。再度ログインしてください。")
    {
    }
}
