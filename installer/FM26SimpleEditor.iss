#ifndef AppVersion
  #define AppVersion "14.1.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish"
#endif

[Setup]
AppId={{9318D1E2-F728-4F26-82EA-48B7D3C5AA61}
AppName=FM26 Simple Editor
AppVersion={#AppVersion}
AppPublisher=hierro1981
AppPublisherURL=https://github.com/hierro1981/FM26SimpleEditor
AppSupportURL=https://github.com/hierro1981/FM26SimpleEditor/issues
AppUpdatesURL=https://github.com/hierro1981/FM26SimpleEditor/releases
DefaultDirName={localappdata}\Programs\FM26SimpleEditor
DefaultGroupName=FM26 Simple Editor
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\artifacts
OutputBaseFilename=FM26SimpleEditor-{#AppVersion}-Setup-win-x64
SetupIconFile=..\Assets\App.ico
UninstallDisplayIcon={app}\FM26SimpleEditor.exe
LicenseFile=..\LICENSE
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=FM26SimpleEditor.exe
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[CustomMessages]
english.Prerequisites=Optional prerequisites (Internet connection required):
italian.Prerequisites=Prerequisiti facoltativi (richiedono Internet):
english.PythonTask=Configure Python and editor dependencies; download Python from python.org only if missing
italian.PythonTask=Configura Python e dipendenze; scarica Python da python.org solo se manca
english.DotNetTask=Download and install .NET 8 Desktop Runtime from Microsoft only if missing (may request administrator permission)
italian.DotNetTask=Scarica e installa .NET 8 Desktop Runtime da Microsoft solo se manca (puo richiedere permessi amministrativi)
english.DesktopTask=Create a desktop shortcut
italian.DesktopTask=Crea un collegamento sul desktop
english.Preparing=Configuring selected prerequisites. This may take several minutes...
italian.Preparing=Configurazione dei prerequisiti selezionati. Puo richiedere alcuni minuti...
english.PrerequisiteFailure=The editor is installed, but prerequisite configuration failed. See %LOCALAPPDATA%\FM26SimpleEditor\installer-prerequisites.log. You can run Setup again, or install .NET Desktop 8 manually and use the editor's startup checks for Python.
italian.PrerequisiteFailure=L'editor e installato, ma la configurazione dei prerequisiti non e riuscita. Consulta %LOCALAPPDATA%\FM26SimpleEditor\installer-prerequisites.log. Puoi rieseguire il setup oppure installare .NET Desktop 8 manualmente e usare i controlli Python all'avvio dell'editor.

[Tasks]
Name: "python"; Description: "{cm:PythonTask}"; GroupDescription: "{cm:Prerequisites}"; Flags: unchecked
Name: "dotnet"; Description: "{cm:DotNetTask}"; GroupDescription: "{cm:Prerequisites}"; Flags: unchecked
Name: "desktopicon"; Description: "{cm:DesktopTask}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Install-Prerequisites.ps1"; DestDir: "{app}\installer"; Flags: ignoreversion

[Icons]
Name: "{group}\FM26 Simple Editor"; Filename: "{app}\FM26SimpleEditor.exe"
Name: "{autodesktop}\FM26 Simple Editor"; Filename: "{app}\FM26SimpleEditor.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\FM26SimpleEditor.exe"; Description: "{cm:LaunchProgram,FM26 Simple Editor}"; Flags: nowait postinstall skipifsilent; Check: PrerequisitesSucceeded

[Code]
var
  PrerequisitesOK: Boolean;

function PrerequisitesSucceeded: Boolean;
begin
  Result := PrerequisitesOK;
end;

procedure InitializeWizard;
begin
  PrerequisitesOK := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Parameters: String;
  ExitCode: Integer;
begin
  if (CurStep = ssPostInstall) and
     (WizardIsTaskSelected('python') or WizardIsTaskSelected('dotnet')) then
  begin
    WizardForm.StatusLabel.Caption := CustomMessage('Preparing');
    Parameters := '-NoProfile -ExecutionPolicy Bypass -File "' +
      ExpandConstant('{app}\installer\Install-Prerequisites.ps1') +
      '" -AppDirectory "' + ExpandConstant('{app}') + '"';
    if WizardIsTaskSelected('python') then
      Parameters := Parameters + ' -ConfigurePython';
    if WizardIsTaskSelected('dotnet') then
      Parameters := Parameters + ' -InstallDesktopRuntime';
    if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
      Parameters, ExpandConstant('{app}'), SW_SHOW, ewWaitUntilTerminated, ExitCode) then
      PrerequisitesOK := False
    else
      PrerequisitesOK := ExitCode = 0;
    if not PrerequisitesOK then
      SuppressibleMsgBox(CustomMessage('PrerequisiteFailure'), mbError, MB_OK, IDOK);
  end;
end;
