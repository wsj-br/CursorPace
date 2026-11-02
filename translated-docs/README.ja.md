# CursorPace

**他の言語で読む:**

<small id="lang-list">[English](../README.md) · [Português (Brasil)](./README.pt-BR.md) · [Deutsch](./README.de.md) · [Español](./README.es.md) · [Français](./README.fr.md) · [हिन्दी](./README.hi.md) · [日本語](./README.ja.md) · [한국어](./README.ko.md) · [简体中文](./README.zh-Hans.md) · [繁體中文](./README.zh-Hant.md)</small>

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="400">
</p>

Windows、Linux、macOS向けのデスクトップアプリで、請求サイクル全体でのCursorモデルのクォータを追跡します。Cursorアカウントでサインインすると、使用状況が自動的に取得されます。このグラフは、**Cursorモデル**と**その他のモデル**という2つの独立したパーセンテージを表示します。

破線の**予想される使用状況**ラインは、サイクル開始時の0%から次回更新時の100%まで直線的に推移します。Cursorおよびその他のモデルは、お客様のサンプルに従います。別途、推定値が1日あたりの消費量と使い切りの時期を予測します。[QUICKSTART.md](QUICKSTART.ja.md)では、チャート、トレイ、設定について説明しています。

サインインには、組み込みのネイティブWebView (WindowsではWebView2、macOSではWKWebView、LinuxではWebKitGTK) およびCursorダッシュボードのセッションが使用されます。公式の個人プランAPIやチームAPIキーはありません。

## 要件

- Windows 10または11 x64、Linux x64またはARM64、macOS (IntelまたはApple Silicon)
- Windowsリリース版のインストールの場合: アプリのペイロードは自己完結型です。**サインイン**には、[Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703)も必要です (ほとんどのWindows 11 PCにはすでにインストールされています。インストーラーは、不足している場合にダウンロードページを提示します)。
- Linux: WebKitGTK 4.1 (`libwebkit2gtk-4.1-0`; Avalonia WebViewのビルドによってはWPEが動作する場合があります)。GNOMEのトレイアイコンにはAppIndicator拡張機能が必要な場合があります。アプリはバンドルされたInterを使用し、システムの`.woff`フォントを無視するため、WOFFのデフォルトフォントが起動を停止させたりクラッシュさせたりすることはありません。
- macOS: WKWebView (内蔵)。リリースは署名されていません。Gatekeeperにより、**システム設定 → プライバシーとセキュリティ**で**とにかく開く**が必要になる場合があります。
- ソースからビルドする場合: [.NET 10 SDK](https://dotnet.microsoft.com/download)

## インストール

1. このリポジトリのリリースぺージから、お使いのプラットフォーム用のビルドをダウンロードします。
   - **Windows**: `CursorPace-*-win-x64-setup.exe`
   - **Linux**: `CursorPace-*-linux-x64.AppImage` または `*-linux-x64.deb` (x86_64)、または対応する `linux-arm64` ファイル (ARM64)
   - **macOS**: `CursorPace-*-osx-arm64.dmg` (Apple Silicon) または `*-osx-x64.dmg` (Intel)
2. **Windows**: インストーラーを実行します。SmartScreenがアプリが未署名であると警告した場合、**詳細情報**を選択し、次に**とにかく実行**を選択します。WebView2 Runtimeが不足している場合は、インストーラーが提示するダウンロードページを開きます。
3. **Linux**: AppImageを実行可能にし (`chmod +x`)、実行します。初回起動時は、バンドルが展開されるまで時間がかかる場合があります。DebianまたはUbuntuでは、`sudo apt install ./CursorPace-*.deb` でパッケージをインストールし、GTK/WebKitの依存関係を解決します。
4. **macOS**: DMGを開き、`CursorPace.app` を「アプリケーション」にドラッグし、開きます。Gatekeeperが未署名のアプリをブロックした場合、一度**開く**を右クリックし、**システム設定 → プライバシーとセキュリティ**で**とにかく開く**を選択します。
5. Cursorにサインインします。「**通知トレイに表示**」がオンの場合（デフォルト）、まずトレイアイコンからウィンドウを開くか、または`--show`で起動します。

初回セットアップ、Cursorアカウントへのサインイン、使用状況グラフ、トレイの動作、およびトラブルシューティングについては、[QUICKSTART.md](QUICKSTART.ja.md)を参照してください。

## 機能

- Cursorにサインインして、使用状況をオンデマンドで、または1、2、4、6、12時間ごとに自動的に更新します
- 請求サイクル開始と次回更新はCursorから取得されます。「前へ」と「次へ」は以前に保存されたサイクルを開きます
- サイクル日付、各モデルの予測、および最新の読み取り値と予想されるペースの比較を示す概要カード
- ドラッグしてズーム機能とポインターの読み取り値を備えた、**1日**、**2D**、**7D**、**1週間**、**2週間**、**1ヶ月**のチャート範囲
- 通知トレイに閉じます。ログイン時に起動（オプション）。2回目の起動では、すでに実行中のウィンドウが開きます。
- ライト、ダーク、またはシステムテーマ、および`locales/ui-languages.json`から取得される言語選択。そのファイルがない場合、選択肢は英語のみです。ウィンドウはサイズ、位置、最大化された状態を記憶します。
- 使用状況をCSVとしてエクスポートし、設定とサンプルをバックアップまたは復元します。
- **オプション。** [CursorPace同期サーバー](https://github.com/wsj-br/CursorPace-SyncServer)は、ローカルネットワーク上のCursorPaceインストールを同期させます。これにより、使用状況のサンプルと請求サイクルの境界が共有されるため、同じ開発環境にあるMac、Windows PC、Linuxマシンで同じグラフが表示されます。そのリポジトリからサーバーをインストールして実行します。

## ソースからビルド

Windows (PowerShell) または Linux/macOS (bash) で:

```bash
dotnet restore
dotnet build
dotnet test ./Tests/CursorPace.Tests.csproj
dotnet run --project ./CursorPace.csproj
```

または、メンテナースクリプトを使用してください: `.\scripts\dev.ps1` / `./scripts/dev.sh`。これらのスクリプトはデフォルトでウィンドウを表示します。アプリの通常の起動時の表示を使用するには、`.\scripts\dev.ps1 -NoShow` または `./scripts/dev.sh --no-show` を使用してください。

自己完結型パブリッシュ (`PublishSingleFile=false` を保持):

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

Linuxでのサインインには、公開されたバイナリを実行するマシンにWebKitGTK 4.1がインストールされている必要があります。
Linuxでのバックグラウンド使用状況の更新は、サインインウィンドウが点滅しないように1x1の透明なWebViewホストを使用します。macOSとWindowsでは、そのホストをログインサイズで画面外に保持します。

このプロジェクトには、macOS `CGRect`/`CGSize` のABIの正確性のために、`vendor/` の下にAvalonia.Controls.WebView 12.1.0の小さな互換性ビルドが含まれています。個別のmacOS WebViewランタイムは必要ありません。

リリースパッケージング:

```powershell
.\scripts\build.ps1          # Windows: Inno Setup installer
```

```bash
./scripts/build.sh             # Linux: AppImage and .deb; macOS: DMG
```

パブリッシュのみ (パッケージングをスキップ): `--skip-installer` / `-SkipInstaller` を追加します。

完全なコントリビューターワークフローは [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md) にあります。

## ドキュメント

| ドキュメント | 対象者 |
| --- | --- |
| [QUICKSTART.md](QUICKSTART.ja.md) | インストール、サインイン、日常使用、トラブルシューティング |
| [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md) | ビルド、テスト、実行、パッケージ、貢献 |
| [dev/CHANGELOG.md](../dev/CHANGELOG.md) | ユーザーに表示される変更点 |

## 免責事項

[Cursor](https://cursor.com/home) はAnysphere, Inc.の製品です。CursorPaceは独立したプロジェクトであり、CursorまたはAnysphereとは関連がなく、また承認もされていません。

## ライセンス

Copyright 2026 Waldemar Scudeller Jr.

このソフトウェアはMITライセンスの下でライセンスされています。詳細については、[LICENSE](../LICENSE) ファイルを参照してください。

このページの翻訳は自動的に生成されています。翻訳がアプリと一致しない場合は、この英語のページを参照してください。
