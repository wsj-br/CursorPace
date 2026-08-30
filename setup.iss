; Cursor Pace Installer Script
; Inno Setup 6.x required

#define MyAppName "Cursor Pace"
#ifndef MyAppVersion
#define MyAppVersion "0.2.2"
#endif
#define MyAppPublisher "Cursor Pace"
#define MyAppExeName "CursorPace.exe"
#ifndef PublishDir
#define PublishDir "bin\Release\net10.0\win-x64\publish"
#endif

[Setup]
AppId={{E0051EAA-2865-44BB-BBC5-0951554456C5}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=installer
OutputBaseFilename=CursorPace-{#MyAppVersion}-win-x64-setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=Assets\cursor_pace.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{localappdata}\CursorPace"

[Code]
function IsWebView2Installed(): Boolean;
var
  Version: String;
begin
  Result := False;
  if RegQueryStringValue(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) then
    Result := True
  else if RegQueryStringValue(HKCU, 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) then
    Result := True
  else if RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}', 'pv', Version) then
    Result := True;
  if Result then
    Result := (Version <> '') and (Version <> '0.0.0.0');
end;

function WaitUntilAppClosed(const ActionPhrase: String): Boolean;
begin
  Result := True;
  while CheckForMutexes('CursorPace_SingleInstance') do
  begin
    if MsgBox('Cursor Pace is currently running. Close it, then click Retry to continue ' + ActionPhrase + ', or click Cancel to abort.', mbError, MB_RETRYCANCEL) <> IDRETRY then
    begin
      Result := False;
      Exit;
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := WaitUntilAppClosed('the installation');
  if not Result then
    Exit;

  if not IsWebView2Installed then
  begin
    if MsgBox('Microsoft Edge WebView2 Runtime is required for automatic Cursor usage updates. Open the download page now?', mbConfirmation, MB_YESNO) = IDYES then
      ShellExec('open', 'https://go.microsoft.com/fwlink/p/?LinkId=2124703', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
  end;
end;

function InitializeUninstall(): Boolean;
begin
  Result := WaitUntilAppClosed('the uninstallation');
end;
