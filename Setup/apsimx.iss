
; Inno Setup Compiler 5.5.3

;APSIM setup script

#include  "ISPPBuiltins.iss"
#define AppVerNo GetStringFileInfo("..\Bin\Model.exe", PRODUCT_VERSION) 

[Setup]
AppName=APSIM
AppVerName=APSIM v{#AppVerNo}
AppPublisherURL=http://www.apsim.au
OutputBaseFilename=APSIMSetup
VersionInfoVersion={#AppVerNo}
PrivilegesRequired=poweruser
;LicenseFile=..\license.txt
AppVersion={#AppVerNo}
AppID=APSIM{#AppVerNo}
DefaultDirName={pf}\APSIM{#AppVerNo}
DefaultGroupName=APSIM{#AppVerNo}
UninstallDisplayIcon={app}\Bin\Userinterface.exe
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
var
  DataDirPage: TInputDirWizardPage;
  OptionPage: TInputOptionWizardPage;

type
  //
  // Enumeration used to specify a .NET framework version 
  //
  TDotNetFramework = (
    DotNet_v11_4322,  // .NET Framework 1.1
    DotNet_v20_50727, // .NET Framework 2.0
    DotNet_v30,       // .NET Framework 3.0
    DotNet_v35,       // .NET Framework 3.5
    DotNet_v4_Client, // .NET Framework 4.0 Client Profile
    DotNet_v4_Full,   // .NET Framework 4.0 Full Installation
    DotNet_v45);      // .NET Framework 4.5

//
// Checks whether the specified .NET Framework version and service pack
// is installed (See: http://www.kynosarges.de/DotNetVersion.html)
//
// Parameters:
//   Version     - Required .NET Framework version
//   ServicePack - Required service pack level (0: None, 1: SP1, 2: SP2 etc.)
//
function IsDotNetInstalled(Version: TDotNetFramework; ServicePack: cardinal): boolean;
  var
    KeyName      : string;
    Check45      : boolean;
    Success      : boolean;
    InstallFlag  : cardinal; 
    ReleaseVer   : cardinal;
    ServiceCount : cardinal;
  begin
    // Registry path for the requested .NET Version
    KeyName := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\';

    case Version of
      DotNet_v11_4322:  KeyName := KeyName + 'v1.1.4322';
      DotNet_v20_50727: KeyName := KeyName + 'v2.0.50727';
      DotNet_v30:       KeyName := KeyName + 'v3.0';
      DotNet_v35:       KeyName := KeyName + 'v3.5';
      DotNet_v4_Client: KeyName := KeyName + 'v4\Client';
      DotNet_v4_Full:   KeyName := KeyName + 'v4\Full';
      DotNet_v45:       KeyName := KeyName + 'v4\Full';
    end;

    // .NET 3.0 uses "InstallSuccess" key in subkey Setup
    if (Version = DotNet_v30) then
      Success := RegQueryDWordValue(HKLM, KeyName + '\Setup', 'InstallSuccess', InstallFlag) else
      Success := RegQueryDWordValue(HKLM, KeyName, 'Install', InstallFlag);

    // .NET 4.0/4.5 uses "Servicing" key instead of "SP"
    if (Version = DotNet_v4_Client) or
       (Version = DotNet_v4_Full) or
       (Version = DotNet_v45) then
      Success := Success and RegQueryDWordValue(HKLM, KeyName, 'Servicing', ServiceCount) else
      Success := Success and RegQueryDWordValue(HKLM, KeyName, 'SP', ServiceCount);

    // .NET 4.5 is distinguished from .NET 4.0 by the Release key
    if (Version = DotNet_v45) then
      begin
        Success := Success and RegQueryDWordValue(HKLM, KeyName, 'Release', ReleaseVer);
        Success := Success and (ReleaseVer >= 378389);
      end;

    Result := Success and (InstallFlag = 1) and (ServiceCount >= ServicePack);
  end;

// .net v4.0 requires a fix that is not in the v4.0 installer. On Win7 and Win8
// this will eventually be installed in the Windows Updates.
function Isv40FixInstalled(): boolean;
var
    KeyName      : string;
    Success      : boolean;
    Flag         : string;
begin
    KeyName := 'SOFTWARE\Microsoft\Updates\Microsoft .NET Framework 4 Extended\KB2468871';
    RegQueryStringValue(HKLM, KeyName,'ThisVersionInstalled', Flag);
    Success := Flag = 'Y';

    KeyName := 'SOFTWARE\Wow6432Node\Microsoft\Updates\Microsoft .NET Framework 4 Extended\KB2468871';
    RegQueryStringValue(HKLM, KeyName,'ThisVersionInstalled', Flag);
    Success := Success or (Flag = 'Y');

    result := Success;
end;

// this is the main function that detects the required version
function IsRequiredDotNetDetected(): Boolean;  
begin
    result := IsDotNetInstalled(DotNet_v4_Full, 0);
end;

function InitializeSetup(): Boolean;
var
  answer: integer;
  ErrorCode: Integer;
begin
    //check for the .net runtime. If it is not found then show a message.
    if not IsRequiredDotNetDetected() then 
    begin
        answer := MsgBox('The Microsoft .NET Framework 4.0 is required.' + #13#10 + #13#10 +
        'Click OK to go to the web site or Cancel to quit', mbInformation, MB_OKCANCEL);        
        result := false;
        if (answer = MROK) then
        begin
          ShellExecAsOriginalUser('open', 'http://www.microsoft.com/en-au/download/details.aspx?id=17718', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
        end;
    end
    else
      if not Isv40FixInstalled() then
      begin
        answer := MsgBox('A fix pack for .NET Framework 4.0 is required.' + #13#10 + #13#10 +
        'Click OK to go to the web site or Cancel to quit', mbInformation, MB_OKCANCEL);        
        result := false;
        if (answer = MROK) then
        begin
          ShellExecAsOriginalUser('open', 'http://support.microsoft.com/kb/2468871', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
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
Source: changes.html; DestDir: {app}\Documentation;
Source: ..\Bin\UserInterface.exe.config; DestDir: {app}\Bin; Flags: ignoreversion; 

;Sample files 
Source: ..\Examples\*; DestDir: {app}\Examples; Flags: recursesubdirs
Source: ..\Examples\*; DestDir: {commondocs}\Apsim\Examples; Flags: recursesubdirs

[INI]

[Tasks]
Name: desktopicon; Description: Create a &desktop icon; Flags: exclusive; GroupDescription: Additional icons:
Name: commondesktopicon; Description: Create a &desktop icon for all users; Flags: exclusive; GroupDescription: Additional icons:
Name: associate; Description: &Associate .apsimx with Apsim; GroupDescription: Other tasks:

[Icons]
Name: {commonprograms}\APSIM{#AppVerNo}; Filename: {app}\Bin\UserInterface.exe
Name: {userdesktop}\APSIM{#AppVerNo}; Filename: {app}\Bin\UserInterface.exe; Tasks: desktopicon
Name: {commondesktop}\APSIM{#AppVerNo}; Filename: {app}\Bin\UserInterface.exe; Tasks: commondesktopicon

[Registry]


[Run]
Filename: {app}\Bin\UserInterface.exe; Description: Launch APSIM; Flags: postinstall nowait skipifsilent
