[Setup]
AppId={{C9D9E6A8-2F6E-4B2D-9C87-BC0D8F2A1B90}
AppName=Bro Cam
AppVersion=0.1.0
AppPublisher=Bro Cam
DefaultDirName={autopf}\\Bro Cam
DefaultGroupName=Bro Cam
OutputDir=output
OutputBaseFilename=BroCam-Setup
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
WizardStyle=modern
UninstallDisplayIcon={app}\\BroCam.exe
Compression=lzma2
SolidCompression=yes

[Files]
Source: "..\\..\\build\\app\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\\Bro Cam"; Filename: "{app}\\BroCam.exe"
Name: "{autodesktop}\\Bro Cam"; Filename: "{app}\\BroCam.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Run]
Filename: "{sys}\\regsvr32.exe"; Parameters: "/s \"{app}\\VCamNetSampleSourceAOT.dll\""; Flags: runhidden waituntilterminated
Filename: "{app}\\BroCam.exe"; Description: "Start Bro Cam"; Flags: postinstall nowait skipifsilent

[UninstallRun]
Filename: "{sys}\\regsvr32.exe"; Parameters: "/u /s \"{app}\\VCamNetSampleSourceAOT.dll\""; Flags: runhidden waituntilterminated
