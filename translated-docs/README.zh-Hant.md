# CursorPace

**以其他語言閱讀：**

<small id="lang-list">[English](../README.md) · [Português (Brasil)](./README.pt-BR.md) · [Deutsch](./README.de.md) · [Español](./README.es.md) · [Français](./README.fr.md) · [हिन्दी](./README.hi.md) · [日本語](./README.ja.md) · [한국어](./README.ko.md) · [简体中文](./README.zh-Hans.md) · [繁體中文](./README.zh-Hant.md)</small>

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="400">
</p>

適用於 Windows、Linux 和 macOS 的桌面應用程式，用於追蹤計費週期內的 Cursor 模型配額。使用您的 Cursor 帳戶登入以自動提取用量。圖表顯示兩個獨立的百分比：**Cursor** 和 **其他模型**。

虛線的 **預期使用量** 是從週期開始時的 0% 到下次續訂時的 100% 的直線進度。Cursor 和其他模型會依循您的樣本。另一個估計會預測每日消耗量和耗盡時間。[QUICKSTART.md](QUICKSTART.zh-Hant.md) 涵蓋了圖表、通知區域和設定。

登入功能使用內嵌的原生 WebView (Windows 上的 WebView2、macOS 上的 WKWebView、Linux 上的 WebKitGTK) 以及您的 Cursor 儀表板工作階段。沒有官方的個人方案 API，也沒有團隊 API 金鑰。

## 系統需求

- Windows 10 或 11 x64、Linux x64 或 ARM64，或 macOS (Intel 或 Apple Silicon)
- 對於 Windows 版本安裝：應用程式負載是獨立的。**登入** 也需要 [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (大多數 Windows 11 電腦上已安裝；如果缺少，安裝程式會提供下載頁面)
- Linux：WebKitGTK 4.1 (`libwebkit2gtk-4.1-0`；WPE 可能會根據 Avalonia WebView 的建置而運作)。GNOME 通知區域圖示可能需要 AppIndicator 擴充功能。應用程式使用捆綁的 Inter 字體並忽略系統 `.woff` 字體，因此 WOFF 預設字體不會導致啟動掛起或崩潰。
- macOS：WKWebView (內建)。版本未簽署；Gatekeeper 可能需要在 **系統設定 → 隱私權與安全性** 中選擇 **仍要開啟**
- 若要從原始碼建置：[.NET 10 SDK](https://dotnet.microsoft.com/download)

## 安裝

1. 從此儲存庫的「發行版本」頁面下載適用於您平台的建置版本：
   - **Windows**：`CursorPace-*-win-x64-setup.exe`
   - **Linux**：`CursorPace-*-linux-x64.AppImage` 或 `*-linux-x64.deb` (x86_64)，或匹配的 `linux-arm64` 檔案 (ARM64)
   - **macOS**：`CursorPace-*-osx-arm64.dmg` (Apple Silicon) 或 `*-osx-x64.dmg` (Intel)
2. **Windows**：執行安裝程式。如果 SmartScreen 警告應用程式未簽署，請選擇 **更多資訊**，然後選擇 **仍要執行**。如果缺少 WebView2 Runtime，請開啟安裝程式提供的下載頁面。
3. **Linux**：將 AppImage 設為可執行檔 (`chmod +x`)，然後執行它。首次啟動時，捆綁包解壓縮可能需要一些時間。在 Debian 或 Ubuntu 上，`sudo apt install ./CursorPace-*.deb` 會安裝套件並提取 GTK/WebKit 依賴項。
4. **macOS**：開啟 DMG，將 `CursorPace.app` 拖曳到「應用程式」資料夾，然後開啟它。如果 Gatekeeper 阻止未簽署的應用程式，請右鍵點擊 **開啟** 一次，然後在 **系統設定 → 隱私權與安全性** 中選擇 **仍要開啟**。
5. 登入 Cursor。如果 **在通知區域中開始** 已開啟 (預設值)，請先從通知區域圖示開啟視窗，或使用 `--show` 啟動。

請參閱 [QUICKSTART.md](QUICKSTART.zh-Hant.md) 以了解首次執行設定、Cursor 帳戶登入、用量圖表、通知區域行為和疑難排解。

## 功能

- 登入 Cursor 並按需重新整理用量，或每 1、2、4、6 或 12 小時自動重新整理
- 計費週期開始和下次續訂來自 Cursor。「上一個」和「下一個」會開啟先前儲存的週期
- 顯示週期日期、每個模型的預測以及最新讀數與預期進度對比的摘要卡片
- 圖表範圍 **1 天**、**2 天**、**7 天**、**1 週**、**2 週** 和 **1 個月**，具有拖曳縮放和指標讀數功能
- 關閉時縮小到通知區域。可選擇登入時啟動。第二次啟動會開啟已在執行的視窗。
- 淺色、深色或系統主題，以及從應用程式中內嵌的 `locales/ui-languages.json` 取得的語言選擇。當無法讀取該資訊清單時，只有英文可供選擇。視窗會記住其大小、位置和最大化狀態。
- 匯出用量為 CSV，以及備份或還原設定和樣本。
- **選用。** [CursorPace 同步伺服器](https://github.com/wsj-br/CursorPace-SyncServer) 讓本機網路上的 CursorPace 安裝保持同步。它會共用用量樣本和計費週期範圍，因此在相同開發環境中的 Mac、Windows PC 和 Linux 機器會顯示相同的圖表。從該儲存庫安裝並執行伺服器。

## 從原始碼建置

在 Windows (PowerShell) 或 Linux/macOS (bash) 上：

```bash
dotnet restore
dotnet build
dotnet test ./Tests/CursorPace.Tests.csproj
dotnet run --project ./CursorPace.csproj
```

或者使用維護者指令碼：`.\scripts\dev.ps1` / `./scripts/dev.sh`。這些指令碼預設會顯示視窗；使用 `.\scripts\dev.ps1 -NoShow` 或 `./scripts/dev.sh --no-show` 來使用應用程式的正常啟動可見性。

獨立發佈 (保留 `PublishSingleFile=false`):

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

Linux 登入需要將 WebKitGTK 4.1 安裝在執行已發佈二進位檔的機器上。
Linux 上的背景使用情況重新整理會使用 1x1 透明的 WebView 主機，這樣它就不會閃爍登入視窗；macOS 和 Windows 會將該主機保持在登入大小並置於螢幕外。

該專案在 `vendor/` 下包含一個 Avalonia.Controls.WebView 12.1.0 的小型相容性建置，以確保 macOS `CGRect`/`CGSize` ABI 的正確性。無需單獨的 macOS WebView 執行時。

發佈打包:

```powershell
.\scripts\build.ps1          # Windows: Inno Setup installer
```

```bash
./scripts/build.sh             # Linux: AppImage and .deb; macOS: DMG
```

僅發佈 (跳過打包): 新增 `--skip-installer` / `-SkipInstaller`。

完整的貢獻者工作流程請參閱 [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md)。

## 文件

| 文件 | 受眾 |
| --- | --- |
| [QUICKSTART.md](QUICKSTART.zh-Hant.md) | 安裝、登入、日常使用、疑難排解 |
| [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md) | 建置、測試、執行、打包、貢獻 |
| [dev/CHANGELOG.md](../dev/CHANGELOG.md) | 使用者可見的變更 |

## 免責聲明

[Cursor](https://cursor.com/home) 是 Anysphere, Inc. 的產品。CursorPace 是一個獨立專案，且與 Cursor 或 Anysphere 無關聯，也未獲得其認可。

## 授權條款

版權所有 2026 Waldemar Scudeller Jr.

本軟體根據 MIT 授權條款授權。有關詳細資訊，請參閱 [LICENSE](../LICENSE) 檔案。

本頁面的翻譯是自動產生的。當翻譯與應用程式不符時，請參考此英文頁面。
