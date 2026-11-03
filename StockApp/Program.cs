using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using StockApp;
using StockApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

var supabaseOptions = builder.Configuration.GetSection("Supabase").Get<SupabaseOptions>()
    ?? throw new InvalidOperationException("appsettings.json に Supabase の設定がありません。");
builder.Services.AddSingleton(supabaseOptions);
builder.Services.AddSingleton(new HttpClient());
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<StockService>();

await builder.Build().RunAsync();
