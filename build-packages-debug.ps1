cd .\.nuget

.\nuget.exe pack ..\src\DbLocalizationProvider.Abstractions\DbLocalizationProvider.Abstractions.csproj -Properties Configuration=Debug -Symbols
.\nuget.exe pack ..\src\DbLocalizationProvider\DbLocalizationProvider.csproj -Properties Configuration=Debug -Symbols
.\nuget.exe pack ..\src\DbLocalizationProvider.AdminUI\DbLocalizationProvider.AdminUI.csproj -Properties Configuration=Debug -Symbols
.\nuget.exe pack ..\src\DbLocalizationProvider.EPiServer\DbLocalizationProvider.EPiServer.csproj -Properties Configuration=Debug -Symbols
.\nuget.exe pack ..\src\DbLocalizationProvider.AdminUI.EPiServer\DbLocalizationProvider.AdminUI.EPiServer.csproj -Properties Configuration=Debug -Symbols
.\nuget.exe pack ..\src\DbLocalizationProvider.MigrationTool\DbLocalizationProvider.MigrationTool.csproj -Properties Configuration=Debug -Symbols -tool
cd ..\