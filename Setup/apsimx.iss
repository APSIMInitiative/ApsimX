
; Inno Setup Compiler 5.5.3

;APSIM setup script

#include  "ISPPBuiltins.iss"
#define AppVerNo GetStringFileInfo("..\Bin\Models.exe", PRODUCT_VERSION) 

[Setup]
AppName=APSIM
AppVerName=APSIM v{#AppVerNo}
AppPublisherURL=http://www.apsim.au
ArchitecturesInstallIn64BitMode=x64
OutputBaseFilename=APSIMSetup
VersionInfoVersion={#AppVerNo}
PrivilegesRequired=poweruser
;LicenseFile=..\license.txt
AppVersion={#AppVerNo}
AppID=APSIM{#AppVerNo}
DefaultDirName={pf}\APSIM{#AppVerNo}
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

function IsDotNetDetected(version: string; service: cardinal): boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1'          .NET Framework 1.1
//    'v2.0'          .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//    'v4.5'          .NET Framework 4.5
//    'v4.5.1'        .NET Framework 4.5.1
//    'v4.5.2'        .NET Framework 4.5.2
//    'v4.6'          .NET Framework 4.6
//    'v4.6.1'        .NET Framework 4.6.1
//    'v4.6.2'        .NET Framework 4.6.2
//    'v4.7'          .NET Framework 4.7
//    'v4.7.1'        .NET Framework 4.7.1
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
    key, versionKey: string;
    install, release, serviceCount, versionRelease: cardinal;
    success: boolean;
begin
    versionKey := version;
    versionRelease := 0;

    // .NET 1.1 and 2.0 embed release number in version key
    if version = 'v1.1' then begin
        versionKey := 'v1.1.4322';
    end else if version = 'v2.0' then begin
        versionKey := 'v2.0.50727';
    end

    // .NET 4.5 and newer install as update to .NET 4.0 Full
    else if Pos('v4.', version) = 1 then begin
        versionKey := 'v4\Full';
        case version of
          'v4.5':   versionRelease := 378389;
          'v4.5.1': versionRelease := 378675; // 378758 on Windows 8 and older
          'v4.5.2': versionRelease := 379893;
          'v4.6':   versionRelease := 393295; // 393297 on Windows 8.1 and older
          'v4.6.1': versionRelease := 394254; // 394271 before Win10 November Update
          'v4.6.2': versionRelease := 394802; // 394806 before Win10 Anniversary Update
          'v4.7':   versionRelease := 460798; // 460805 before Win10 Creators Update
          'v4.7.1': versionRelease := 461308; // 461310 before Win10 Fall Creators Update
        end;
    end;

    // installation key group for all .NET versions
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + versionKey;

    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;

    // .NET 4.0 and newer use value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;

    // .NET 4.5 and newer use additional value Release
    if versionRelease > 0 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
        success := success and (release >= versionRelease);
    end;

    result := success and (install = 1) and (serviceCount >= service);
end;

// this is the main function that detects the required version
function IsRequiredDotNetDetected(): Boolean;  
begin
    result := IsDotNetDetected('v4.6', 0);
end;

function InitializeSetup(): Boolean;
var
  answer: integer;
  ErrorCode: Integer;
begin
    //check for the .net runtime. If it is not found then show a message.
    if not IsRequiredDotNetDetected() then 
    begin
        answer := MsgBox('The Microsoft .NET Framework 4.6 is required.' + #13#10 + #13#10 +
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
Source: ..\Examples\*; DestDir: {commondocs}\Apsim\Examples; Flags: recursesubdirs
Source: ..\Tests\UnderReview\*; DestDir: {app}\UnderReview; Flags: recursesubdirs
Source: ..\Tests\UnderReview\*; DestDir: {commondocs}\Apsim\UnderReview; Flags: recursesubdirs

[INI]

[Tasks]
Name: desktopicon; Description: Create a &desktop icon; Flags: exclusive; GroupDescription: Additional icons:
Name: commondesktopicon; Description: Create a &desktop icon for all users; Flags: exclusive; GroupDescription: Additional icons:
Name: associate; Description: &Associate .apsimx with Apsim; GroupDescription: Other tasks:

[Icons]
Name: {commonprograms}\APSIM{#AppVerNo}; Filename: {app}\Bin\ApsimNG.exe
Name: {userdesktop}\APSIM{#AppVerNo}; Filename: {app}\Bin\ApsimNG.exe; Tasks: desktopicon
Name: {commondesktop}\APSIM{#AppVerNo}; Filename: {app}\Bin\ApsimNG.exe; Tasks: commondesktopicon

[Registry]
; With this key, the embedded WebBrowser emulates IE7, which breaks Google Maps.
; I'm setting a value of 11000 here to emulate IE11; this may need to change in the future.
Root: HKLM; Subkey: "Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"; ValueType: dword; ValueName: "ApsimNG.exe"; ValueData: "11000"
Root: HKLM; Subkey: "Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"; ValueType: dword; ValueName: "ApsimNG.vshost.exe"; ValueData: "11000"

[Run]
Filename: {app}\Bin\ApsimNG.exe; Description: Launch APSIM; Flags: postinstall nowait skipifsilent
