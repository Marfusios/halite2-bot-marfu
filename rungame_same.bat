del *.log
del *.hlt

dotnet build src\BotMarfu.csproj -o ..\artefacts\marfu
halite.exe -d "240 160" "dotnet artefacts\marfu\BotMarfu.dll" "dotnet artefacts\marfu\BotMarfu.dll"