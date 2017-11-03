del *.log
del *.hlt

dotnet build src\BotMarfu.csproj -o ..\artefacts\marfu2
# halite.exe -d "264 176" -s 671260812 "dotnet artefacts\marfu2\BotMarfu.dll" "dotnet artefacts\opponent_v6\BotMarfu.dll" -t
halite.exe -d "240 160" -s 2845971487 "dotnet artefacts\marfu2\BotMarfu.dll" "dotnet artefacts\opponent_v6\BotMarfu.dll" -t
# halite.exe -d "240 160" -s 33 "dotnet artefacts\marfu2\BotMarfu.dll" "dotnet artefacts\opponent_v6\BotMarfu.dll" -t
# halite.exe -d "240 160" -s 102 "dotnet artefacts\marfu2\BotMarfu.dll" "dotnet artefacts\opponent_v6\BotMarfu.dll" -t