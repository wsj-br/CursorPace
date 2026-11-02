# 快速入门

**阅读其他语言版本：**

<small id="lang-list">[English](../QUICKSTART.md) · [Português (Brasil)](./QUICKSTART.pt-BR.md) · [Deutsch](./QUICKSTART.de.md) · [Español](./QUICKSTART.es.md) · [Français](./QUICKSTART.fr.md) · [हिन्दी](./QUICKSTART.hi.md) · [日本語](./QUICKSTART.ja.md) · [한국어](./QUICKSTART.ko.md) · [简体中文](./QUICKSTART.zh-Hans.md) · [繁體中文](./QUICKSTART.zh-Hant.md)</small>

CursorPace 的最终用户指南。有关从源代码构建的信息，请参阅 [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md)。

## 安装

从此存储库的发布页面下载构建版本。

**Windows。** 运行 `CursorPace-*-win-x64-setup.exe`。如果 SmartScreen 警告应用未签名，请选择 **更多信息**，然后选择 **仍然运行**。**登录** 需要 [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703)。如果缺少，安装程序会提供下载页面。

**Linux。** 对于大多数发行版，`chmod +x CursorPace-*-linux-x64.AppImage`（或 `linux-arm64` 文件）并运行它。在 Debian 或 Ubuntu 上，`sudo apt install ./CursorPace-*-linux-x64.deb`（或 `linux-arm64`）会引入 GTK 和 WebKit。Google 登录可能在 WebKit 中被阻止；请使用 GitHub、电子邮件或在 Windows 上登录。

**macOS。** 打开 `CursorPace-*-osx-arm64.dmg`（Apple 芯片）或 `*-osx-x64.dmg`（Intel），然后将 `CursorPace.app` 拖到“应用程序”中。如果 Gatekeeper 阻止未签名的应用，请右键单击 **打开** 一次，然后在 **系统设置 → 隐私与安全性** 中选择 **仍然打开**。

## 首次运行

**在通知托盘中运行** 默认开启，因此首次启动可能只显示托盘图标。从该图标打开窗口，或使用 `--show` 启动。

1. 选择 **登录**。
2. 在嵌入式窗口中完成 Google、GitHub 或双重身份验证登录。当 Cursor 接受会话时，它会自动关闭。如果您已看到您的帐户，请选择 **继续**。
3. 计费周期和用量将显示在图表上。

您可以稍后从 **设置** → **Cursor 帐户** 登录。成功更新后，该标题会显示为 **Cursor 帐户 (已连接)**。标题栏中的 **刷新**，或“设置”中的 **立即刷新**，会立即获取数据。

**退出登录** 会清除 Cursor 会话。在 Windows 和 macOS 上，保存的 Google 或 GitHub 登录信息可能会保留，因此下次登录时可能会跳过密码。在 Linux 上，**退出登录** 会清除整个浏览器配置文件。用量样本会保留在磁盘上。

当 **自动更新使用情况** 开启时，刷新会在当地午夜后的 1、2、4、6 或 12 小时标记时进行。启动时，只有在上次成功刷新至少 20 分钟前，并且错过了预定时间段或整个间隔已过时，才会运行刷新。

## 主窗口

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="600">
</p>

图表上方有三张卡片：

- **周期开始**和**下次续订**。在当前周期中，剩余时间显示在续订日期旁边。
- 每个模型的预测。在周期内，这是耗尽时间。在虚线预期线之上，表示该线何时与读数相符。在线上或线之下，表示续订时的预计百分比。在当前周期中，日期旁边会显示等待时间。
- **上次测量**：本周期中最新的样本，包含 Cursor %、其他 %、预期用量 %，以及每个系列与该进度相差的距离。

月份标题上的“上一个”和“下一个”可切换查看存储的周期。在该标题下方，`Cursor dd-MMM HH:mm` 是最新刷新时间。红点表示帐户已退出登录或刷新失败。单击它可打开 **Cursor 帐户**。配置了同步服务器时，**同步服务器** 及其上次成功时间也会显示在那里。红点表示上次尝试失败。单击它可打开 **同步服务器**。

| 范围 | 显示内容 |
| --- | --- |
| **1D** / **2D** | 最近 24 或 48 小时 |
| **7D** / **1周** | 最近七天 |
| **2周** | 最近14天 |
| **1个月** | 整个显示周期，每个本地日一个样本 |

在实时周期中，短范围在当前时间结束。在较早的周期中，它在该周期的续订时结束。如果窗口原本会更早开始，则起始点保持在周期开始时。

拖动图表以缩放。右键单击返回到 **1个月**。选择一个范围按钮，包括已选中的按钮，将退出缩放并显示该范围。**所有样本，最长可达** (**2D**、**4D**、**7D** 或 **14D**，默认 **14D**) 设置图表绘制每个样本的距离并标记本地时间。较长的范围保留每天的最后一个样本并标记月份中的日期。垂直轴适应屏幕上的值，最高可达120%。

移动指针以读取该时间的 Cursor、其他模型和预期用量。在最后一个样本之后，Cursor 和其他模型显示估计值，标记为 `estimated`。在缩放期间，读数还会显示缩放开始时的每个值以及从那时起的变化。

| 控件 | 操作 |
| --- | --- |
| **刷新** | 立即获取 Cursor 用量。在“设置”页面上隐藏 |
| **设置** | 在此窗口中打开设置 |
| **返回** | 返回图表 |
| **最小化** / **最大化** | 标准窗口控件。应用程序保留在托盘中 |
| 关闭 | 隐藏窗口。应用程序保留在托盘中 |

窗口会记住其大小、位置和最大化状态。信息标签可以被选中和复制。

## 预期和估计

- **预期用量**在图表上是虚线，从周期开始时的0%到下次续订时的100%。悬停时使用此线。
- **Cursor**和**其他模型**是测量的样本。
- **预期**在周期CSV中跟随每个样本，然后根据剩余配额调整进度，以在续订时达到100%。
- **估计**是圆点虚线投影，从最后一个样本到续订。超过最后一个样本后，悬停时使用此线。

Cursor 和其他模型保持独立。

## 设置

设置会替换图表。**返回**，或“**设置**”标题，都会返回到图表。上次打开的选项卡下次会再次打开，包括重启后。

**同步服务器** 是可选的。[CursorPace 同步服务器](https://github.com/wsj-br/CursorPace-SyncServer) 在本地网络上的机器之间共享用量样本和计费周期边界，因此同一开发环境中的 Mac、Windows PC 和 Linux 机器会显示相同的图表。从该存储库安装并运行服务器。主题、语言、窗口位置和启动选项保留在每台计算机上。

| 标签页 | 可设置的内容 |
| --- | --- |
| 启动与显示 | **登录时启动**、**在通知托盘中运行**、主题（系统、浅色或深色）、**语言**（`locales/ui-languages.json`中的语言区域，或当该文件缺失时使用英语）、以及**所有样本，最长可达** |
| Cursor 帐户 | **登录**（连接时隐藏）、**立即刷新**、**退出登录**、以及 1、2、4、6 或 12 小时自动刷新 |
| 同步服务器 | **与服务器同步**、服务器 URL、API 令牌、机器名称（默认为主机名）和**立即重新同步**。该链接会打开同步服务器存储库 |
| 导出与备份 | **导出周期 CSV** 用于屏幕上显示的周期，**导出使用情况** 用于登录时存储的每个样本，以及将设置和样本**备份**/**恢复**为一个 zip 文件。**打开文件夹** 会打开目标位置 |
| 关于 | 版本、UTC 构建日期和时间、版权、MIT 许可证和 GitHub 存储库 |

备份和恢复会保留此机器上的 Cursor 登录信息。

## 托盘

点击图标，或选择**打开**，以显示窗口。**退出**会退出应用。工具提示显示今日百分比，并且，一旦有足够数据，还会显示续订时的预计百分比。

在 macOS 上，点击会打开托盘菜单，并且当窗口隐藏或最小化时，Dock 图标会被隐藏。从托盘、使用`--show`或通过再次启动应用来显示窗口。

如果图标丢失，请打开通知溢出区域。在 GNOME 上，可能需要 AppIndicator 扩展。

第二次启动会显示已在运行的窗口。第二次`--background`启动会使其保持隐藏。`--show`会强制打开窗口。**登录时启动**遵循**在通知托盘中运行**。

## 数据

```text
Windows: %LocalAppData%\CursorPace\
Linux:   ~/.local/share/CursorPace/
macOS:   ~/Library/Application Support/CursorPace/
```

`settings.json`保存偏好设置、窗口位置和周期边界。`usage-samples.json`保存样本。Cursor 登录信息存储在 Windows 上的`WebView2`中，Linux 和 macOS 上的`WebView`中，或在运行 Linux AppImage 时存储在`WebView-AppImage`中。无法解析的文件会被复制到`*.corrupt.json`并替换为空白文件。锁定或不可读的文件会保持原样。

删除该文件夹以重新开始。在 Windows 上，卸载应用会将其删除。在 Linux 和 macOS 上，请自行删除。

## 续订

报告新周期开始的获取操作会归档上一周期并保留其样本。“上一周期”和“下一周期”会打开这些周期。当本地日期达到存储的续订日期时，应用也会刷新。窗口可以保持隐藏。进程必须运行才能进行该检查和自动更新。

## 卸载

从托盘菜单退出。在 Windows 上，从**已安装的应用**中卸载**CursorPace**。在 Linux 和 macOS 上，删除应用，并为了全新开始，删除上述数据文件夹。

## 故障排除

**应用无法启动。** 结束任何`CursorPace`进程并再次启动。在 Windows 上，检查事件查看器。在 Linux 上，尝试`journalctl --user -xe`。在 macOS 上，检查 Console.app。如果 Linux CPU 占用率达到 100%，或`crash.log`显示`Could not create glyphTypeface`，请安装 0.3.0 以上的版本。如果**登录时启动**仍然指向已删除的 AppImage，请运行新的 AppImage 一次。

**登录失败，或“找不到指定的模块”。** 在 Windows 上，安装 [WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (Evergreen x64)。在 Linux 上，安装`libwebkit2gtk-4.1-0`。如果 Google 阻止嵌入式浏览器，请使用 GitHub 或电子邮件。当您的 Cursor 帐户已显示在屏幕上时，选择**继续**。

**使用情况未更新。** 确认**Cursor 帐户 (已连接)** 和自动更新，或选择**刷新**。检查系统日期、时间以及时区。在达到 Cursor 速率限制后，应用会等待下一个间隔。

**托盘或任务栏图标不见了。** 打开通知溢出区域。在 Linux 上，重启应用，使其能够写入 `~/.local/share/applications/CursorPace.desktop`。如果任务栏图标仍然是通用齿轮图标，请注销一次，或者在 X11 上按 Alt+F2，输入 `r`，然后按 Enter。

**设置未保存，或今天的百分比看起来不对。** 确认您可以写入数据文件夹。如果 `settings.corrupt.json` 存在，请删除它和 `settings.json` 以重置。然后检查时钟并选择**刷新**。

**登录时启动无效。** 确认该设置已开启。Windows 将 `CursorPace` 存储在当前用户的 Run 键中，并在托盘启动开启时添加 `--background`。已签名的 macOS 应用会出现在**登录时打开**下。未签名的构建使用 `~/Library/LaunchAgents/com.cursorpace.app.plist` 并通过 `/usr/bin/open` 启动 `.app`。Linux 使用 `~/.config/autostart/cursor-pace.desktop`。AppImage 条目必须指向 `.AppImage` 文件。关闭并重新开启该设置，或启动新的 AppImage 一次，以重写它。

**两个托盘图标，或 Linux 重启后已注销。** 从托盘退出并启动应用一次。在 Linux 上，使用当前构建再次登录，以便会话存储在 `cookies.sqlite` 中。

本页面的翻译是自动生成的。如果翻译与应用不符，请以英文页面为准。
