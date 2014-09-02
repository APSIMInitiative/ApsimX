
; Inno Setup Compiler 5.5.3

;ApsimX setup script

#define AppVerNo '14.10.1' 

[Setup]
AppName=ApsimX
AppVerName=ApsimX version {#AppVerNo}, Oct 2014
AppPublisherURL=http://www.csiro.au
OutputBaseFilename=setupx
VersionInfoVersion={#AppVerNo}
PrivilegesRequired=poweruser
;LicenseFile=..\license.txt
AppVersion={#AppVerNo}
AppID=apsimx
DefaultDirName={pf}\ApsimX
DefaultGroupName=APSIM
UninstallDisplayIcon={app}\Bin\Userinterface.exe
Compression=lzma/Max
ChangesAssociations=true
WizardSmallImageFile=csiro_logo.bmp
WizardImageFile=.\asetup.bmp
;InfoBeforeFile=
VersionInfoCompany=CSIRO
VersionInfoDescription=Apsim Modelling
VersionInfoProductName=ApsimX
VersionInfoProductVersion={#AppVerNo}


[Code]
var
  DataDirPage: TInputDirWizardPage;
  OptionPage: TInputOptionWizardPage;

[InstallDelete]
Name: {localappdata}\VirtualStore\ApsimX\*.*; Type: filesandordirs
Name: {localappdata}\VirtualStore\ApsimX; Type: dirifempty

[Files]
Source: ..\Bin\UserInterface.exe; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\ApServer.exe; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\ApsimFile.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\CSGeneral.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\Excel.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\ICSharpCode.NRefactory.Cecil.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\ICSharpCode.NRefactory.CSharp.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\ICSharpCode.NRefactory.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\ICSharpCode.SharpZipLib.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\ICSharpCode.TextEditor.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\Importer.exe; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\MathNet.Numerics.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\Model.exe; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\Model.XmlSerializers.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\ModelEditControl.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\Mono.Cecil.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\OxyPlot.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\OxyPlot.WindowsForms.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\sqlite3.dll; DestDir: {app}\Bin; Flags: ignoreversion; 

Source: changes.html; DestDir: {app}\Documentation;
;Source: ..\Documentation\ApsimX tutorial.pdf; DestDir: {app}\Documentation;

;Sample files 
Source: ..\Examples\*; DestDir: {app}\Examples; Flags: recursesubdirs
Source: ..\Examples\*; DestDir: {userdocs}\ApsimX\Examples; Flags: recursesubdirs

[INI]

[Tasks]
Name: desktopicon; Description: Create a &desktop icon; Flags: exclusive; GroupDescription: Additional icons:
Name: commondesktopicon; Description: Create a &desktop icon for all users; Flags: exclusive; GroupDescription: Additional icons:
Name: associate; Description: &Associate .apsimx with ApsimX; GroupDescription: Other tasks:

[Icons]
Name: {commonprograms}\CSIRO\ApsimX; Filename: {app}\Bin\UserInterface.exe
;Name: {commonprograms}\CSIRO\ApsimX Tutorial; Filename: {app}\docs\ApsimX tutorial.pdf
Name: {userdesktop}\ApsimX; Filename: {app}\Bin\UserInterface.exe; Tasks: desktopicon
Name: {commondesktop}\ApsimX; Filename: {app}\Bin\UserInterface.exe; Tasks: commondesktopicon

[Registry]


[Run]
Filename: {app}\Documentation\changes.html; Description: View the CHANGES file; Flags: postinstall shellexec skipifsilent
Filename: {app}\Bin\UserInterface.exe; Description: Launch ApsimX; Flags: postinstall nowait skipifsilent
