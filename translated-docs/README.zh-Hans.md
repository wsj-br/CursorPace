# CursorPace

**阅读其他语言版本：**

<small id="lang-list">[English](../README.md) · [Português (Brasil)](./README.pt-BR.md) · [Deutsch](./README.de.md) · [Español](./README.es.md) · [Français](./README.fr.md) · [हिन्दी](./README.hi.md) · [日本語](./README.ja.md) · [한국어](./README.ko.md) · [简体中文](./README.zh-Hans.md) · [繁體中文](./README.zh-Hant.md)</small>

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="400">
</p>

适用于 Windows、Linux 和 macOS 的桌面应用，用于跟踪计费周期内的 Cursor 模型配额。使用您的 Cursor 帐户登录以自动拉取用量。图表显示两个独立的百分比：**Cursor** 和 **其他模型**。

虚线**预期用量**从周期开始时的0%到下次续订时的100%呈直线增长。Cursor 和其他模型遵循您的样本。独立的估算会预测每日消耗和耗尽。[QUICKSTART.md](QUICKSTART.zh-Hans.md)涵盖了图表、托盘和设置。

登录使用嵌入式原生 WebView（Windows 上为 WebView2，macOS 上为 WKWebView，Linux 上为 WebKitGTK）以及您的 Cursor 控制面板会话。没有官方的个人计划 API，也没有团队 API 密钥。

## 要求

- Windows 10 或 11 x64、Linux x64 或 ARM64，或 macOS（Intel 或 Apple Silicon）
- 对于 Windows 版本安装：应用负载是自包含的。**登录** 还需要 [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703)（大多数 Windows 11 电脑已预装；如果缺失，安装程序会提供下载页面）
- Linux：WebKitGTK 4.1 (`libwebkit2gtk-4.1-0`；WPE 可能有效，具体取决于 Avalonia WebView 构建)。GNOME 托盘图标可能需要 AppIndicator 扩展。应用使用捆绑的 Inter 字体并忽略系统 `.woff` 字体，因此 WOFF 默认字体不会导致启动挂起或崩溃。
- macOS：WKWebView（内置）。发布版本未签名；Gatekeeper 可能需要您在 **系统设置 → 隐私与安全性** 中选择 **仍要打开**。
- 从源代码构建：[.NET 10 SDK](https://dotnet.microsoft.com/download)

## 安装

1. 从此存储库的“发布”页面下载适用于您平台的构建版本：
   - **Windows**：`CursorPace-*-win-x64-setup.exe`
   - **Linux**：`CursorPace-*-linux-x64.AppImage` 或 `*-linux-x64.deb` (x86_64)，或匹配的 `linux-arm64` 文件 (ARM64)
   - **macOS**：`CursorPace-*-osx-arm64.dmg` (Apple Silicon) 或 `*-osx-x64.dmg` (Intel)
2. **Windows**：运行安装程序。如果 SmartScreen 警告应用未签名，请选择 **更多信息**，然后选择 **仍要运行**。如果 WebView2 Runtime 缺失，请打开安装程序提供的下载页面。
3. **Linux**：将 AppImage 设置为可执行文件 (`chmod +x`)，然后运行它。首次启动时，捆绑包解压可能需要一些时间。在 Debian 或 Ubuntu 上，`sudo apt install ./CursorPace-*.deb` 会安装软件包并拉取 GTK/WebKit 依赖项。
4. **macOS**：打开 DMG，将 `CursorPace.app` 拖到“应用程序”文件夹，然后打开它。如果 Gatekeeper 阻止未签名的应用，请右键单击 **打开** 一次，然后在 **系统设置 → 隐私与安全性** 中选择 **仍要打开**。
5. 登录 Cursor。如果 **在通知托盘中运行** 已开启（默认设置），请先从托盘图标打开窗口，或使用 `--show` 启动。

有关首次运行设置、Cursor 帐户登录、用量图表、托盘行为和故障排除，请参阅 [QUICKSTART.md](QUICKSTART.zh-Hans.md)。

## 功能

- 登录 Cursor 并按需刷新用量，或每 1、2、4、6 或 12 小时自动刷新
- 计费周期开始和下次续订信息来自 Cursor。“上一个”和“下一个”按钮可打开之前存储的周期
- 显示周期日期、每个模型的预测以及最新读数与预期进度对比的摘要卡片
- 图表范围包括 **1天**、**2D**、**7D**、**1周**、**2周** 和 **1个月**，支持拖动缩放和指针读数
- 最小化到通知托盘。可选的登录时启动。再次启动会打开已在运行的窗口
- 浅色、深色或系统主题，以及从 `locales/ui-languages.json` 获取的语言选择。如果该文件缺失，则只能选择英语。窗口会记住其大小、位置和最大化状态
- 将使用情况导出为 CSV，以及备份或恢复设置和样本
- **可选。** [CursorPace 同步服务器](https://github.com/wsj-br/CursorPace-SyncServer) 使本地网络上的 CursorPace 安装保持同步。它共享使用情况样本和计费周期边界，因此，同一开发环境中的 Mac、Windows PC 和 Linux 机器会显示相同的图表。从该存储库安装并运行服务器。

## 从源代码构建

在 Windows (PowerShell) 或 Linux/macOS (bash) 上：

```bash
dotnet restore
dotnet build
dotnet test ./Tests/CursorPace.Tests.csproj
dotnet run --project ./CursorPace.csproj
```

或者使用维护者脚本：`.\scripts\dev.ps1` / `./scripts/dev.sh`。这些脚本默认显示窗口；使用 `.\scripts\dev.ps1 -NoShow` 或 `./scripts/dev.sh --no-show` 来使用应用的正常启动可见性。

自包含发布（保留 `PublishSingleFile=false`）：

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

Linux 登录需要运行已发布二进制文件的机器上安装 WebKitGTK 4.1。
Linux 上的后台使用情况刷新使用 1x1 透明 WebView 主机，因此它不会闪烁登录窗口；macOS 和 Windows 将该主机保持在登录大小并在屏幕外。

该项目在 `vendor/` 下包含一个 Avalonia.Controls.WebView 12.1.0 的小型兼容性构建，以确保 macOS `CGRect`/`CGSize` ABI 的正确性。无需单独的 macOS WebView 运行时。

发布打包：

```powershell
.\scripts\build.ps1          # Windows: Inno Setup installer
```

```bash
./scripts/build.sh             # Linux: AppImage and .deb; macOS: DMG
```

仅发布（跳过打包）：添加 `--skip-installer` / `-SkipInstaller`。

完整的贡献者工作流程位于 [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md)。

## 文档

| 文档 | 受众 |
| --- | --- |
| [QUICKSTART.md](QUICKSTART.zh-Hans.md) | 安装、登录、日常使用、故障排除 |
| [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md) | 构建、测试、运行、打包、贡献 |
| [dev/CHANGELOG.md](../dev/CHANGELOG.md) | 用户可见的更改 |

## 免责声明

[Cursor](https://cursor.com/home) 是 Anysphere, Inc. 的产品。CursorPace 是一个独立项目，与 Cursor 或 Anysphere 无关联，也未获得其认可。

## 许可证

Copyright 2026 Waldemar Scudeller Jr.

本软件根据 MIT 许可证获得许可。有关详细信息，请参阅 [LICENSE](../LICENSE) 文件。

本页面的翻译是自动生成的。如果翻译与应用不符，请以英文页面为准。
