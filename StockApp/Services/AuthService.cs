using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace StockApp.Services;

/// <summary>
/// Supabase Auth(メール・パスワード)。パスワードは保持せず、セッションはメモリ上にのみ置く。
/// </summary>
public sealed class AuthService
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromSeconds(60);

    private readonly HttpClient _http;
    private readonly SupabaseOptions _options;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _expiresAt;

    public AuthService(HttpClient http, SupabaseOptions options)
    {
        _http = http;
        _options = options;
    }

    public bool IsSignedIn => _accessToken is not null;

    public async Task<bool> SignInAsync(string email, string password)
    {
        using var request = CreateRequest(HttpMethod.Post, $"{_options.Url}/auth/v1/token?grant_type=password", null);
        request.Content = JsonContent.Create(new { email, password });
        using var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return false;
        }

        var session = await response.Content.ReadFromJsonAsync<SessionResponse>();
        if (string.IsNullOrEmpty(session?.AccessToken))
        {
            return false;
        }

        Apply(session);
        return true;
    }

    public async Task SignOutAsync()
    {
        var token = _accessToken;
        Clear();

        if (token is null)
        {
            return;
        }

        try
        {
            using var request = CreateRequest(HttpMethod.Post, $"{_options.Url}/auth/v1/logout", token);
            using var _ = await _http.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            // サーバー側の失効に失敗しても、ブラウザ側のセッションは破棄済み。
        }
    }

    /// <summary>有効なアクセストークンを返す。期限が近ければ更新し、更新できなければサインアウト状態にして null を返す。</summary>
    public async Task<string?> GetAccessTokenAsync()
    {
        if (_accessToken is null)
        {
            return null;
        }

        if (DateTimeOffset.UtcNow < _expiresAt - RefreshMargin)
        {
            return _accessToken;
        }

        await _refreshLock.WaitAsync();
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow < _expiresAt - RefreshMargin)
            {
                return _accessToken;
            }

            if (_refreshToken is null || !await RefreshAsync(_refreshToken))
            {
                Clear();
                return null;
            }

            return _accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public HttpRequestMessage CreateRequest(HttpMethod method, string url, string? accessToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", _options.PublishableKey);
        request.Headers.Add("Authorization", $"Bearer {accessToken ?? _options.PublishableKey}");
        return request;
    }

    private async Task<bool> RefreshAsync(string refreshToken)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Post, $"{_options.Url}/auth/v1/token?grant_type=refresh_token", null);
            request.Content = JsonContent.Create(new { refresh_token = refreshToken });
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var session = await response.Content.ReadFromJsonAsync<SessionResponse>();
            if (string.IsNullOrEmpty(session?.AccessToken))
            {
                return false;
            }

            Apply(session);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private void Apply(SessionResponse session)
    {
        _accessToken = session.AccessToken;
        _refreshToken = session.RefreshToken;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(session.ExpiresIn);
    }

    private void Clear()
    {
        _accessToken = null;
        _refreshToken = null;
    }

    private sealed class SessionResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
