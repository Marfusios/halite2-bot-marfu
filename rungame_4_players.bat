del *.log
del *.hlt

dotnet build src\BotMarfu.csproj -o ..\artefacts\marfu
halite.exe -d "240 160" -s 33 "dotnet artefacts\marfu\BotMarfu.dll" "dotnet artefacts\opponent_v2\BotMarfu_old.dll" "dotnet artefacts\opponent_v3\BotMarfu.dll" "dotnet artefacts\opponent_v4\BotMarfu.dll"