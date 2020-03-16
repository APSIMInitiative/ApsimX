
; Inno Setup Compiler 5.5.3

;APSIM setup script

#include  "ISPPBuiltins.iss"
#define AppVerNo GetStringFileInfo("..\Bin\Models.exe", PRODUCT_VERSION) 

[Setup]
AppName=APSIM
AppVerName=APSIM v{#AppVerNo}
AppPublisherURL=https://www.apsim.info
ArchitecturesInstallIn64BitMode=x64
OutputBaseFilename=APSIMSetup
VersionInfoVersion={#AppVerNo}
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
;LicenseFile=..\license.txt
AppVersion={#AppVerNo}
AppID=APSIM{#AppVerNo}
DefaultDirName={autopf}\APSIM{#AppVerNo}
DefaultGroupName=APSIM{#AppVerNo}
UninstallDisplayIcon={app}\Bin\ApsimNG.exe
Compression=lzma/Max
ChangesAssociations=true
WizardSmallImageFile=apsim_logo32.bmp
WizardImageFile=.\APSIMInitiativeBanner.bmp
;InfoBeforeFile=
VersionInfoCompany=APSIM Initiative4
VersionInfoDescription=Apsim Modelling
VersionInfoProductName=Apsim
VersionInfoProductVersion={#AppVerNo}


[Code]
function InitializeSetup(): Boolean;
var
  answer: integer;
  ErrorCode: Integer;
begin
    //check for the .net runtime. If it is not found then show a message.
    if not IsDotNetInstalled(net462, 0) then 
    begin
        answer := MsgBox('The Microsoft .NET Framework 4.6 or above is required.' + #13#10 + #13#10 +
        'Click OK to go to the web site or Cancel to quit', mbInformation, MB_OKCANCEL);        
        result := false;
        if (answer = MROK) then
        begin
          ShellExecAsOriginalUser('open', 'http://www.microsoft.com/en-au/download/details.aspx?id=48130', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
        end;
    end
    else
      result := true;
end; 

[InstallDelete]
Name: {localappdata}\VirtualStore\Apsim\*.*; Type: filesandordirs
Name: {localappdata}\VirtualStore\Apsim; Type: dirifempty

[Files]
Source: ..\Bin\*.exe; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\*.dll; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\Tools\*.*; DestDir: {app}\Bin\Tools; Flags: ignoreversion;
Source: ..\DeploymentSupport\Windows\Bin32\*.dll; DestDir: {app}\Bin; Flags: ignoreversion; Check: not Is64BitInstallMode
Source: ..\DeploymentSupport\Windows\Bin32\lib\gtk-2.0\2.10.0\engines\*.dll; DestDir: {app}\lib\gtk-2.0\2.10.0\engines; Flags: ignoreversion; Check: not Is64BitInstallMode
Source: ..\DeploymentSupport\Windows\Bin64\*.dll; DestDir: {app}\Bin; Flags: ignoreversion; Check: Is64BitInstallMode
Source: ..\DeploymentSupport\Windows\Bin64\lib\gtk-2.0\2.10.0\engines\*.dll; DestDir: {app}\lib\gtk-2.0\2.10.0\engines; Flags: ignoreversion; Check: Is64BitInstallMode
Source: ..\Bin\.gtkrc; DestDir: {app}\Bin; Flags: ignoreversion;
Source: ..\Bin\ApsimNG.exe.config; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\Bin\Models.xml; DestDir: {app}\Bin; Flags: ignoreversion; 
Source: ..\APSIM.bib; DestDir: {app}; Flags: ignoreversion;

;Sample files 
Source: ..\Examples\*; DestDir: {app}\Examples; Flags: recursesubdirs
Source: ..\Examples\*; DestDir: {autodocs}\Apsim\Examples; Flags: recursesubdirs
Source: ..\Tests\UnderReview\*; DestDir: {app}\UnderReview; Flags: recursesubdirs
Source: ..\Tests\UnderReview\*; DestDir: {autodocs}\Apsim\UnderReview; Flags: recursesubdirs

[Tasks]
Name: desktopicon; Description: Create a &desktop icon; Flags: exclusive; GroupDescription: Additional icons:
Name: commondesktopicon; Description: Create a &desktop icon for all users; Flags: exclusive; GroupDescription: Additional icons:
Name: associate; Description: &Associate .apsimx with Apsim; GroupDescription: Other tasks:

[Icons]
Name: {autoprograms}\APSIM{#AppVerNo}; Filename: {app}\Bin\ApsimNG.exe
Name: {userdesktop}\APSIM{#AppVerNo}; Filename: {app}\Bin\ApsimNG.exe; Tasks: desktopicon
Name: {autodesktop}\APSIM{#AppVerNo}; Filename: {app}\Bin\ApsimNG.exe; Tasks: commondesktopicon

; The following registry changes should no longer be needed, as we're now doing the mapping through Leaflet rather than Goold Maps
;[Registry]
; With this key, the embedded WebBrowser emulates IE7, which breaks Google Maps.
; I'm setting a value of 11000 here to emulate IE11; this may need to change in the future.
; Root: HKLM; Subkey: "Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"; ValueType: dword; ValueName: "ApsimNG.exe"; ValueData: "11000"
; Root: HKLM; Subkey: "Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"; ValueType: dword; ValueName: "ApsimNG.vshost.exe"; ValueData: "11000"

[Run]
Filename: {app}\Bin\ApsimNG.exe; Description: Launch APSIM; Flags: postinstall nowait skipifsilent
