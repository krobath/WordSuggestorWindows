#ifndef Payload
  #error Missing Payload
#endif
[Setup]
AppId={#ProductId}
AppName={#AppName}
AppVersion={#DisplayVersion}
VersionInfoVersion={#PackageVersion}
VersionInfoProductVersion={#PackageVersion}
VersionInfoProductTextVersion={#DisplayVersion}
AppVerName={#AppName} {#DisplayVersion}
AppPublisher=WordSuggestor
AppPublisherURL=https://wordsuggestor.com
DefaultDirName={localappdata}\Programs\{#InstallLeaf}
DefaultGroupName={#AppName}
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\Assets\WordSuggestor.ico
SetupIconFile={#IconPath}
OutputDir={#OutputDirectory}
OutputBaseFilename={#OutputName}
Compression=lzma2/fast
SolidCompression=yes
WizardStyle=modern
CloseApplications=no
RestartApplications=no
AppMutex=Local\{#ProductId}
Uninstallable=yes
UninstallFilesDir={app}\uninstall
#ifdef SignedBuild
SignTool=WordSuggestor
SignedUninstaller=yes
#endif
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "danish"; MessagesFile: "compiler:Languages\Danish.isl"
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
[Files]
Source: "{#Payload}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\WordSuggestorWindows.App.exe"; IconFilename: "{app}\Assets\WordSuggestor.ico"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\WordSuggestorWindows.App.exe"; IconFilename: "{app}\Assets\WordSuggestor.ico"; Tasks: desktopicon
[Registry]
Root: HKCU; Subkey: "Software\WordSuggestor\Installations\{#ProductId}"; ValueType: string; ValueName: "Version"; ValueData: "{#PackageVersion}"; Flags: uninsdeletekey
#ifndef LocalPreview
Root: HKCU; Subkey: "Software\Classes\wordsuggestor-ocr"; ValueType: string; ValueData: "URL:WordSuggestor OCR"; Check: CanRegisterOcr
Root: HKCU; Subkey: "Software\Classes\wordsuggestor-ocr"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Check: CanRegisterOcr
Root: HKCU; Subkey: "Software\Classes\wordsuggestor-ocr\shell\open\command"; ValueType: string; ValueData: """{app}\WordSuggestorWindows.App.exe"" ""%1"""; Check: CanRegisterOcr
#endif
[Run]
Filename: "{app}\WordSuggestorWindows.App.exe"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
[Code]
#ifndef LocalPreview
function OwnOcrCommand(): String;
begin
  Result := ExpandConstant('"{app}\WordSuggestorWindows.App.exe" "%1"');
end;
function CanRegisterOcr(): Boolean;
var Previous: String;
begin
  Result := not RegQueryStringValue(HKCU, 'Software\Classes\wordsuggestor-ocr\shell\open\command', '', Previous);
  if not Result then Result := CompareText(Previous, OwnOcrCommand()) = 0;
end;
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var Current: String;
begin
  if CurUninstallStep = usPostUninstall then
    if RegQueryStringValue(HKCU, 'Software\Classes\wordsuggestor-ocr\shell\open\command', '', Current) then
      if CompareText(Current, OwnOcrCommand()) = 0 then
        RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\wordsuggestor-ocr');
end;
#endif
function CompareNumericVersion(A, B: String): Integer;
var I, VA, VB, P: Integer; PartA, PartB: String;
begin
  Result := 0;
  for I := 1 to 4 do begin
    P := Pos('.', A); if P = 0 then P := Length(A) + 1;
    PartA := Copy(A, 1, P - 1); Delete(A, 1, P);
    P := Pos('.', B); if P = 0 then P := Length(B) + 1;
    PartB := Copy(B, 1, P - 1); Delete(B, 1, P);
    VA := StrToIntDef(PartA, -1); VB := StrToIntDef(PartB, -1);
    if (VA < 0) or (VB < 0) then begin Result := 1; Exit; end;
    if VA > VB then begin Result := 1; Exit; end;
    if VA < VB then begin Result := -1; Exit; end;
  end;
end;
function InitializeSetup(): Boolean;
var Previous: String;
begin
  Result := True;
  if RegQueryStringValue(HKCU, 'Software\WordSuggestor\Installations\{#ProductId}', 'Version', Previous) then
    if CompareNumericVersion(Previous, '{#PackageVersion}') > 0 then begin
      SuppressibleMsgBox('A newer WordSuggestor version is installed. Install a newer release to roll back safely.', mbError, MB_OK, IDOK);
      Result := False;
    end;
end;
