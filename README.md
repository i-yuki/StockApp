# StockApp

在庫管理システム(Blazor WebAssembly + MudBlazor + Supabase)。

## 機能

- ログイン
- 在庫一覧(商品グループで絞り込み)
- 入庫・出庫
- 商品別入出庫履歴

## 設定

`StockApp/wwwroot/appsettings.json` に Supabase の URL と publishable key を設定する。
`service_role` キーなどの秘密情報は絶対に記載しない(ブラウザへ配信される)。

## GitHub Pages

`master` への push で `.github/workflows/deploy-pages.yml` が公開用ビルドを行う。
リポジトリの Settings > Pages > Source を「GitHub Actions」に設定すること。
公開時は `base href` をリポジトリ名に差し替え、`404.html` を `index.html` のコピーとして配置する。
