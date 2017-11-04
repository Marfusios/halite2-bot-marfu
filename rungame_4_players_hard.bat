del *.log
del *.hlt

dotnet build src\BotMarfu.csproj -o ..\artefacts\marfu2
halite.exe -d "300 240" -s 33 "dotnet artefacts\opponent_v15\BotMarfu.dll" "dotnet artefacts\opponent_v5\BotMarfu.dll" "dotnet artefacts\marfu2\BotMarfu.dll" "dotnet artefacts\opponent_v17\BotMarfu.dll"