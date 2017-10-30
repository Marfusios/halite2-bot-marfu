del *.log
del *.hlt

dotnet build src\BotMarfu.csproj -o ..\artefacts\marfu
halite.exe -d "240 160" -s 3839831370 "dotnet artefacts\marfu\BotMarfu.dll" "dotnet artefacts\opponent_v6\BotMarfu.dll"