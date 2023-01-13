#define MyAppName "Traceability"
#define MyAppVersion "1.0" 
#define MyAppExeName "Traceability.exe"
#define MyAppPublisher "Beko rus"
#define MyAppURL "http://beko.ru/"
#define BinFolder "D:\TFS\ru.beko\Traceability\Traceability\bin"


;надо добавить для eng win
#define MyRuleName1ru "Инструментарий управления Windows (DCOM - входящий трафик)"
#define MyRuleName1en "Windows Management Instrumentation (DCOM-In)"



[Setup]
PrivilegesRequired=admin
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)

AppId={{1D12CBFA-EF2A-4619-80DB-9A88BBE495A4}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
;AppPublisherURL={#MyAppURL}
;AppSupportURL={#MyAppURL}
;AppUpdatesURL={#MyAppURL}   
;AppVerName={#MyAppName} {#MyAppVersion}
;DefaultDirName={autopf}\{#MyAppName}
;по умолчанию мы устанавливаем в program files или program files (x86)
DefaultDirName=C:\{#MyAppName}
DisableProgramGroupPage=yes
;InfoBeforeFile=D:\TFS\local\freezerHandle\freezerHandle\InnoSetup\before.txt
;InfoAfterFile=D:\TFS\local\freezerHandle\freezerHandle\InnoSetup\after.txt
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
OutputDir={#BinFolder}\Setup
OutputBaseFilename=Setup_{#MyAppName}_v{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]                                          
Name: "english"; MessagesFile: "D:\Distrib\InnoSetup\English.isl"     
Name: "russian"; MessagesFile: "D:\Distrib\InnoSetup\Russian.isl"
;Name: "english"; MessagesFile: "compiler:Default.isl"
;Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"


[Tasks]
Name: "installNet48"; Description:"{cm:InstallNet48}";  GroupDescription: "{cm:AdditionalIcons}";
Name: "installKepware"; Description: "{cm:InstallKepware}";  GroupDescription: "{cm:AdditionalIcons}";
Name: "installFirewall"; Description:  "{cm:AddToFirewall}";  GroupDescription: "{cm:AdditionalIcons}";
Name: "addUser"; Description: "{cm:AddUserBeko}"; GroupDescription: "{cm:AdditionalIcons}";
Name: "autoLogonUser"; Description: "{cm:BekoAutoLogon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: restart;
Name: "commonstartupicon"; Description: "{cm:AutoStartProgram}"; GroupDescription: "{cm:AdditionalIcons}";
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";
; Flags: unchecked
;Name: "regOPC"; Description: "Зарегистрировать OPC библитотеку"; GroupDescription: "{cm:AdditionalIcons}";

[Files]
Source: "{#BinFolder}\Release\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BinFolder}\Release\{#MyAppExeName}.config"; DestDir: "{app}"; Flags: ignoreversion
;Source: "{#BinFolder}\Release\docs\About.txt"; DestDir: "{app}\docs"; Flags: ignoreversion
;Source: "{#BinFolder}\Release\docs\Manual.png"; DestDir: "{app}\docs"; Flags: ignoreversion
;Source: "{#BinFolder}\Release\docs\TechInfo.txt"; DestDir: "{app}\docs"; Flags: ignoreversion


; NOTE: Don't use "Flags: ignoreversion" on any shared system files

; .NET Framework 4.8
Source: "D:\Distrib\dotNET\ndp48-x86-x64-allos-enu.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Tasks: installNet48

;Kepware Client
Source: "D:\Distrib\OPC\KEPServerEx5.18.673.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Tasks:  installKepware

;User bat
;Source: "D:\Distrib\InnoSetup\userBekoLogon.bat"; DestDir: "{tmp}"; Flags: deleteafterinstall; Tasks:  autoLogonUser


[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{commonstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppName}"; WorkingDir: "{app}"; Tasks: commonstartupicon   
 

[Registry]
;------------------------------------------------------------------------------
;   Секция работы с реестром
;------------------------------------------------------------------------------

Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"; ValueType: string; ValueName: DefaultUserName; ValueData: "Beko"; Flags: createvalueifdoesntexist;  Check: "not IsWin64"; Tasks: autoLogonUser; 
Root: HKLM64; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"; ValueType: string; ValueName: DefaultUserName; ValueData: "Beko"; Flags: createvalueifdoesntexist; Check: IsWin64; Tasks: autoLogonUser;
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"; ValueType: string; ValueName: DefaultUserName; ValueData: "Beko"; Flags: createvalueifdoesntexist; Check: IsWin64; MinVersion:10.0; Tasks: autoLogonUser;
;Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"; ValueType: string; ValueName: "DefaultPassword"; ValueData: "12345678"; Flags: createvalueifdoesntexist; Check: "not IsWin64"; Tasks: autoLogonUser; 
Root: HKLM64; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"; ValueType: string; ValueName: "DefaultPassword"; ValueData: "12345678"; Flags: createvalueifdoesntexist; Check: IsWin64; Tasks: autoLogonUser; 
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"; ValueType: string; ValueName: "AutoAdminLogon"; ValueData: "1"; Flags: createvalueifdoesntexist; Check: "not IsWin64"; Tasks: autoLogonUser; 
Root: HKLM64; Subkey: "SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"; ValueType: string; ValueName: "AutoAdminLogon"; ValueData: "1"; Flags: createvalueifdoesntexist; Check: IsWin64; Tasks: autoLogonUser; 


[Run]
;Filename: {tmp}\NDP472-KB4054530-x86-x64-AllOS-ENU.exe; Parameters: "/q:a /c:""install /l /q"""; Check: not IsRequiredDotNetDetected; StatusMsg: {cm:MsgStart} Microsoft Framework 4.7.2 {cm:MsgEnd}; Tasks: installNet472

Filename: cmd.exe; Parameters: "/c netsh advfirewall firewall delete rule name=""{#MyAppName}"" "; Tasks:  installFirewall  
Filename: cmd.exe; Parameters: "/c netsh advfirewall firewall add rule name=""{#MyAppName}"" dir=in action=allow program=""{app}\{#MyAppExeName}"" enable=yes"; Tasks:  installFirewall  
;разрешаем DCOM port 135:
Filename: cmd.exe; Parameters: "/c netsh advfirewall firewall set rule name=""{#MyRuleName1ru}"" new enable=yes"; Tasks:  installFirewall  
Filename: cmd.exe; Parameters: "/c netsh advfirewall firewall set rule name=""{#MyRuleName1en}"" new enable=yes"; Tasks:  installFirewall  
Filename: cmd.exe; Parameters: "/c net user Beko 12345678 /add "; Tasks:  addUser
Filename: cmd.exe; Parameters: "/c net user Beko 12345678 "; Tasks:  addUser
;автологин пользователя
;Filename: cmd.exe; Parameters: "/c reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"" /v DefaultUserName /f"; Tasks:  autoLogonUser;  
;Filename: cmd.exe; Parameters: "/c reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"" /v DefaultPassword /f"; Tasks:  autoLogonUser;  
;Filename: cmd.exe; Parameters: "/c reg delete ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"" /v AutoAdminLogon /f"; Tasks:  autoLogonUser;  
;Filename: cmd.exe; Parameters: "/c reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"" /v DefaultUserName /t REG_SZ /d Beko   pause"; Tasks:  autoLogonUser;  
;Filename: cmd.exe; Parameters: "/c reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"" /v DefaultPassword /t REG_SZ /d 12345678"; Tasks:  autoLogonUser;  
;Filename: cmd.exe; Parameters: "/c reg add ""HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon"" /v AutoAdminLogon /t REG_SZ /d 1"; Tasks:  autoLogonUser;  
;не истекает пароль
Filename: cmd.exe; Parameters: "/c WMIC USERACCOUNT WHERE Name='Beko' SET PasswordExpires=FALSE"; Tasks:  addUser;  

;автологин пользователя, не истекает пароль 
;Flags: shellexec postinstall;
;Filename: {tmp}\userBekoLogon.bat;  Tasks:  autoLogonUser;      

Filename: cmd.exe; Parameters: "/c net localgroup Administrators Beko  /add "; Tasks:  addUser
Filename: cmd.exe; Parameters: "/c net localgroup Администраторы Beko  /add "; Tasks:  addUser


;Filename: cmd.exe; Parameters: "/c netsh advfirewall firewall add rule name=""freezerHandle"" dir=out action=allow program=""{app}\{#MyAppExeName}"" enable=yes"; Tasks:  installFirewall  



Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent; 




;------------------------------------------------------------------------------
;   Секция кода включенная из отдельного файла
;------------------------------------------------------------------------------
[Code]


[Run]
;------------------------------------------------------------------------------
;   Секция запуска после инсталляции
;------------------------------------------------------------------------------

Filename: {tmp}\KEPServerEx5.18.673.exe; Parameters: "/q"; StatusMsg: {cm:MsgStart} Kepware OPC Client. {cm:MsgEnd}; Tasks:  installKepware
Filename: {tmp}\ndp48-x86-x64-allos-enu.exe; StatusMsg: {cm:MsgStart} NET Framework 4.8. {cm:MsgEnd}; Tasks:  installNet48
