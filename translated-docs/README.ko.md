# CursorPace

**다른 언어로 읽기:**

<small id="lang-list">[English](../README.md) · [Português (Brasil)](./README.pt-BR.md) · [Deutsch](./README.de.md) · [Español](./README.es.md) · [Français](./README.fr.md) · [हिन्दी](./README.hi.md) · [日本語](./README.ja.md) · [한국어](./README.ko.md) · [简体中文](./README.zh-Hans.md) · [繁體中文](./README.zh-Hant.md)</small>

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="400">
</p>

Windows, Linux, macOS용 데스크톱 앱으로, 청구 주기 동안 Cursor 모델 할당량을 추적합니다. Cursor 계정으로 로그인하여 사용량을 자동으로 가져옵니다. 차트는 두 가지 독립적인 백분율인 **Cursor 모델** 및 **기타 모델**을 보여줍니다.

점선 **예상 사용량** 선은 청구 주기 시작 시 0%에서 다음 갱신 시 100%까지의 일정한 속도입니다. Cursor 및 기타 모델은 사용자의 샘플을 따릅니다. 별도의 추정치는 일일 소모량과 소진 시점을 예측합니다. [QUICKSTART.md](QUICKSTART.ko.md)는 차트, 트레이 및 설정을 다룹니다.

로그인은 내장된 네이티브 WebView(Windows에서는 WebView2, macOS에서는 WKWebView, Linux에서는 WebKitGTK)와 Cursor 대시보드 세션을 사용합니다. 공식적인 개인 플랜 API는 없으며 팀 API 키도 없습니다.

## 요구 사항

- Windows 10 또는 11 x64, Linux x64 또는 ARM64, 또는 macOS(Intel 또는 Apple Silicon)
- Windows 릴리스 설치의 경우: 앱 페이로드는 자체 포함되어 있습니다. **로그인**하려면 [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703)도 필요합니다(대부분의 Windows 11 PC에는 이미 설치되어 있으며, 설치 프로그램에서 누락된 경우 다운로드 페이지를 제공합니다).
- Linux: WebKitGTK 4.1 (`libwebkit2gtk-4.1-0`; Avalonia WebView 빌드에 따라 WPE가 작동할 수 있습니다). GNOME 트레이 아이콘에는 AppIndicator 확장이 필요할 수 있습니다. 앱은 번들된 Inter를 사용하며 시스템 `.woff` 글꼴을 무시하므로 WOFF 기본 글꼴로 인해 시작이 멈추거나 충돌하지 않습니다.
- macOS: WKWebView (내장). 릴리스는 서명되지 않았습니다. Gatekeeper는 **시스템 설정 → 개인 정보 보호 및 보안**에서 **모든 곳에서 열기**를 요구할 수 있습니다.
- 소스에서 빌드하려면: [.NET 10 SDK](https://dotnet.microsoft.com/download)

## 설치

1. 이 저장소의 릴리스 페이지에서 해당 플랫폼용 빌드를 다운로드합니다.
   - **Windows**: `CursorPace-*-win-x64-setup.exe`
   - **Linux**: `CursorPace-*-linux-x64.AppImage` 또는 `*-linux-x64.deb` (x86_64), 또는 일치하는 `linux-arm64` 파일 (ARM64)
   - **macOS**: `CursorPace-*-osx-arm64.dmg` (Apple Silicon) 또는 `*-osx-x64.dmg` (Intel)
2. **Windows**: 설치 프로그램을 실행합니다. SmartScreen에서 앱이 서명되지 않았다고 경고하면 **자세히**를 선택한 다음 **어쨌든 실행**을 선택합니다. WebView2 Runtime이 없으면 설치 프로그램이 제공하는 다운로드 페이지를 엽니다.
3. **Linux**: AppImage를 실행 가능하게 만든 다음(`chmod +x`) 실행합니다. 번들이 압축 해제되는 동안 첫 실행은 시간이 걸릴 수 있습니다. Debian 또는 Ubuntu에서는 `sudo apt install ./CursorPace-*.deb`가 패키지를 설치하고 GTK/WebKit 종속성을 가져옵니다.
4. **macOS**: DMG를 열고 `CursorPace.app`를 응용 프로그램으로 드래그한 다음 엽니다. Gatekeeper가 서명되지 않은 앱을 차단하면 **열기**를 한 번 마우스 오른쪽 버튼으로 클릭한 다음 **시스템 설정 → 개인 정보 보호 및 보안**에서 **모든 곳에서 열기**를 선택합니다.
5. Cursor에 로그인합니다. **알림 트레이에서 시작**이 켜져 있으면(기본값) 먼저 트레이 아이콘에서 창을 열거나 `--show`로 실행합니다.

첫 실행 설정, Cursor 계정 로그인, 사용량 차트, 트레이 동작 및 문제 해결에 대한 자세한 내용은 [QUICKSTART.md](QUICKSTART.ko.md)를 참조하십시오.

## 기능

- Cursor에 로그인하고 필요에 따라 사용량을 새로 고치거나 1, 2, 4, 6, 12시간마다 자동으로 새로 고칩니다.
- 청구 주기 시작 및 다음 갱신은 Cursor에서 가져옵니다. 이전 및 다음은 이전에 저장된 주기를 엽니다.
- 청구 주기 날짜, 각 모델의 예측, 예상 속도 대비 최신 판독값에 대한 요약 카드.
- 차트 범위는 **1일**, **2D**, **7D**, **1주**, **2주**, **1개월**이며 드래그하여 확대/축소 및 포인터 판독 기능을 제공합니다.
- 알림 트레이로 닫힙니다. 로그인 시 실행은 선택 사항입니다. 두 번째 실행 시 이미 실행 중인 창이 열립니다.
- 밝은 테마, 어두운 테마 또는 시스템 테마, 그리고 애플리케이션에 내장된 `locales/ui-languages.json`에서 가져온 언어 선택. 해당 매니페스트를 읽을 수 없는 경우 영어만 선택할 수 있습니다. 창은 크기, 위치 및 최대화된 상태를 기억합니다.
- 사용량 내보내기를 CSV로 하고, 설정 및 샘플을 백업하거나 복원합니다.
- **선택 사항입니다.** [CursorPace 동기화 서버](https://github.com/wsj-br/CursorPace-SyncServer)는 로컬 네트워크의 CursorPace 설치를 동기화된 상태로 유지합니다. 사용량 샘플과 청구 주기 경계를 공유하므로, 동일한 개발 환경에 있는 Mac, Windows PC 및 Linux 머신이 동일한 차트를 표시합니다. 해당 저장소에서 서버를 설치하고 실행하십시오.

## 소스에서 빌드

Windows(PowerShell) 또는 Linux/macOS(bash)에서:

```bash
dotnet restore
dotnet build
dotnet test ./Tests/CursorPace.Tests.csproj
dotnet run --project ./CursorPace.csproj
```

또는 유지 관리 스크립트를 사용하십시오: `.\scripts\dev.ps1` / `./scripts/dev.sh`. 이 스크립트들은 기본적으로 창을 표시합니다. 앱의 일반적인 시작 시 가시성을 사용하려면 `.\scripts\dev.ps1 -NoShow` 또는 `./scripts/dev.sh --no-show`를 사용하십시오.

자체 포함 게시 (`PublishSingleFile=false` 유지):

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

Linux 로그인은 게시된 바이너리를 실행하는 머신에 WebKitGTK 4.1이 설치되어 있어야 합니다.
Linux에서 백그라운드 사용량 새로 고침은 1x1 투명 WebView 호스트를 사용하여 로그인 창이 깜박이지 않도록 합니다. macOS 및 Windows는 해당 호스트를 로그인 크기로 화면 밖에 유지합니다.

이 프로젝트는 macOS `CGRect`/`CGSize` ABI 정확성을 위해 `vendor/` 아래에 Avalonia.Controls.WebView 12.1.0의 작은 호환성 빌드를 포함합니다. 별도의 macOS WebView 런타임은 필요하지 않습니다.

릴리스 패키징:

```powershell
.\scripts\build.ps1          # Windows: Inno Setup installer
```

```bash
./scripts/build.sh             # Linux: AppImage and .deb; macOS: DMG
```

게시만 (패키징 건너뛰기): `--skip-installer` / `-SkipInstaller`를 추가하십시오.

전체 기여자 워크플로는 [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md)에 있습니다.

## 문서

| 문서 | 대상 |
| --- | --- |
| [QUICKSTART.md](QUICKSTART.ko.md) | 설치, 로그인, 일상적인 사용, 문제 해결 |
| [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md) | 빌드, 테스트, 실행, 패키지, 기여 |
| [dev/CHANGELOG.md](../dev/CHANGELOG.md) | 사용자에게 보이는 변경 사항 |

## 면책 조항

[Cursor](https://cursor.com/home)는 Anysphere, Inc.의 제품입니다. CursorPace는 독립적인 프로젝트이며 Cursor 또는 Anysphere와 관련이 없거나 승인되지 않았습니다.

## 라이선스

Copyright 2026 Waldemar Scudeller Jr.

이 소프트웨어는 MIT 라이선스에 따라 라이선스가 부여됩니다. 자세한 내용은 [LICENSE](../LICENSE) 파일을 참조하십시오.

이 페이지의 번역은 자동으로 생성됩니다. 번역이 앱과 일치하지 않을 경우 이 영어 페이지를 참조하십시오.
