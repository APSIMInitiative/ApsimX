
; Inno Setup Compiler 6.0.4

;APSIM setup script

#include  "ISPPBuiltins.iss"
#define ApsimX "..\..\.."
#define Setup ApsimX + "\Setup"

; We allow VERSION to be passed as a command-line parameter (e.g. iscc /DVERSION=1.2.3.4 apsimx.iss)
#ifdef VERSION
#define AppVerNo VERSION
#else
#define AppVerNo GetStringFileInfo(ApsimX + "\bin\Release\netcoreapp3.1\win-x64\publish\Models.exe", PRODUCT_VERSION) 
#endif
[Setup]
AppName=APSIM
AppVerName=APSIM v{#AppVerNo}
AppPublisherURL=https://www.apsim.info
ArchitecturesInstallIn64BitMode=x64
OutputBaseFilename=APSIMSetup
VersionInfoVersion={#AppVerNo}
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
;LicenseFile={#ApsimX}\license.txt
AppVersion={#AppVerNo}
AppID=APSIM{#AppVerNo}
DefaultDirName={autopf}\APSIM{#AppVerNo}
DefaultGroupName=APSIM{#AppVerNo}
UninstallDisplayIcon={app}\bin\ApsimNG.exe
Compression=lzma/Max
ChangesAssociations=true
WizardSmallImageFile={#Setup}\apsim_logo32.bmp
WizardImageFile={#Setup}\APSIMInitiativeBanner.bmp
;InfoBeforeFile=
VersionInfoCompany=APSIM Initiative4
VersionInfoDescription=Apsim Modelling
VersionInfoProductName=Apsim
VersionInfoProductVersion={#AppVerNo}


[Code]
// https://stackoverflow.com/questions/37825650/compare-version-strings-in-inno-setup
// 
// Returns 0, if the versions are equal.
// Returns -1, if the V1 is older than the V2.
// Returns 1, if the V1 is newer than the V2.
// 1.12 is considered newer than 1.1.
// 1.1 is considered the same as 1.1.0.
// Throws an exception, when a version is syntactically invalid (only digits and dots are allowed).
function CompareVersion(V1, V2: string): Integer;
var
  P, N1, N2: Integer;
begin
  Result := 0;
  while (Result = 0) and ((V1 <> '') or (V2 <> '')) do
  begin
    P := Pos('.', V1);
    if P > 0 then
    begin
      N1 := StrToInt(Copy(V1, 1, P - 1));
      Delete(V1, 1, P);
    end
      else
    if V1 <> '' then
    begin
      N1 := StrToInt(V1);
      V1 := '';
    end
      else
    begin
      N1 := 0;
    end;

    P := Pos('.', V2);
    if P > 0 then
    begin
      N2 := StrToInt(Copy(V2, 1, P - 1));
      Delete(V2, 1, P);
    end
      else
    if V2 <> '' then
    begin
      N2 := StrToInt(V2);
      V2 := '';
    end
      else
    begin
      N2 := 0;
    end;

    if N1 < N2 then Result := -1
      else
    if N1 > N2 then Result := 1;
  end;
end;

function HasDotNetCore(version: string) : boolean;
var
	runtimes: TArrayOfString;
	I: Integer;
	versionCompare: Integer;
	registryKey: string;
begin
	registryKey := 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App';
	if(not IsWin64) then
	   registryKey :=  'SOFTWARE\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.NETCore.App';
	   
	Log('[.NET] Look for version ' + version);
	   
	if not RegGetValueNames(HKLM, registryKey, runtimes) then
	begin
	  Log('[.NET] Issue getting runtimes from registry');
	  Result := False;
	  Exit;
	end;
	
    for I := 0 to GetArrayLength(runtimes)-1 do
	begin
	  versionCompare := CompareVersion(runtimes[I], version);
	  Log(Format('[.NET] Compare: %s/%s = %d', [runtimes[I], version, versionCompare]));
	  if(not (versionCompare = -1)) then
	  begin
	    Log(Format('[.NET] Selecting %s', [runtimes[I]]));
	    Result := True;
	  	Exit;
	  end;
    end;
	Log('[.NET] No compatible found');
	Result := False;
end;

// this is the main function that detects the required version
function IsRequiredDotNetDetected(): Boolean;  
begin
    result := HasDotNetCore('3.1.0');
end;

function InitializeSetup(): Boolean;
var
  answer: integer;
  ErrorCode: Integer;
begin
    //check for the .net runtime. If it is not found then show a message.
    if not IsRequiredDotNetDetected() then 
    begin
        answer := MsgBox('The Microsoft .NET Core Runtime 3.1 or above is required.' + #13#10 + #13#10 +
        'Click OK to go to the web site or Cancel to quit', mbInformation, MB_OKCANCEL);        
        result := false;
        if (answer = MROK) then
        begin
          ShellExecAsOriginalUser('open', 'https://download.visualstudio.microsoft.com/download/pr/88437980-f813-4a01-865c-f992ad4909bb/9a936984781f6ce3526ffc946267e0ea/windowsdesktop-runtime-3.1.14-win-x64.exe', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
        end;
    end
    else
      result := true;
end;

[InstallDelete]
Name: {localappdata}\VirtualStore\Apsim\*.*; Type: filesandordirs
Name: {localappdata}\VirtualStore\Apsim; Type: dirifempty

[Files]
Source: {#ApsimX}\bin\Release\netcoreapp3.1\win-x64\publish\*; DestDir: {app}\bin; Flags: ignoreversion;
;fixme
Source: {#ApsimX}\DeploymentSupport\global\*; DestDir: {app}\bin; Flags: ignoreversion;
Source: {#ApsimX}\DeploymentSupport\Windows\Bin64\sqlite3.dll; DestDir: {app}\bin; Flags: ignoreversion;
Source: {#ApsimX}\bin\Release\netcoreapp3.1\win-x64\publish\Models.xml; DestDir: {app}\bin; Flags: ignoreversion; 
Source: {#ApsimX}\APSIM.bib; DestDir: {app}; Flags: ignoreversion;
Source: {#ApsimX}\ApsimNG\Resources\world\*; DestDir: {app}\ApsimNG\Resources\world; Flags: recursesubdirs

;Sample files 
Source: {#ApsimX}\Examples\*; DestDir: {app}\Examples; Flags: recursesubdirs
Source: {#ApsimX}\Examples\*; DestDir: {autodocs}\Apsim\Examples; Flags: recursesubdirs
Source: {#ApsimX}\Tests\UnderReview\*; DestDir: {app}\UnderReview; Flags: recursesubdirs
Source: {#ApsimX}\Tests\UnderReview\*; DestDir: {autodocs}\Apsim\UnderReview; Flags: recursesubdirs

[Tasks]
Name: desktopicon; Description: Create a &desktop icon; GroupDescription: Additional icons:; Flags: unchecked
Name: associate; Description: &Associate .apsimx with Apsim Next Generation; GroupDescription: Other tasks:

[UninstallDelete]
Type: files; Name: "{app}\apsim.url"

[INI]
Filename: "{app}\apsim.url"; Section: "InternetShortcut"; Key: "URL"; String: "https://apsimnextgeneration.netlify.com/" 

[Icons]
;Name: {autoprograms}\APSIM{#AppVerNo}; Filename: {app}\bin\ApsimNG.exe
Name: "{group}\APSIM User Interface"; Filename: {app}\bin\ApsimNG.exe
Name: "{group}\APSIM Next Generation home page"; Filename: "{app}\apsim.url";
Name: {autodesktop}\APSIM{#AppVerNo}; Filename: {app}\bin\ApsimNG.exe; Tasks: desktopicon

[Registry]
;do the associations
Root: HKA; Subkey: "Software\Classes\.apsimx"; ValueType: string; ValueName: ""; ValueData: APSIMXFile; Flags: uninsdeletevalue; Tasks: associate
Root: HKA; Subkey: "Software\Classes\APSIMXFile"; ValueType: string; ValueName: ""; ValueData: APSIM Next Generation File; Flags: uninsdeletekey; Tasks: associate
Root: HKA; Subkey: "Software\Classes\APSIMXFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: {app}\bin\ApsimNG.exe,0; Tasks: associate
Root: HKA; Subkey: "Software\Classes\APSIMXFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\bin\ApsimNG.exe"" ""%1"""; Tasks: associate

[Run]
Filename: {app}\bin\ApsimNG.exe; Description: Launch APSIM; Flags: postinstall nowait skipifsilent
