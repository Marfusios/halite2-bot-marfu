del *.log
del *.hlt

dotnet build ..\src\BotMarfu.csproj -o ..\artefacts\marfu

python .\client.py gym -r "dotnet ..\artefacts\marfu\BotMarfu.dll" -r "dotnet ..\artefacts\opponent_v6\BotMarfu.dll" -b "..\halite.exe" -i 100 -W 240 -H 160 -id "v16" -desc "vs v6       "
python .\client.py gym -r "dotnet ..\artefacts\marfu\BotMarfu.dll" -r "dotnet ..\artefacts\opponent_v9\BotMarfu.dll" -b "..\halite.exe" -i 100 -W 240 -H 160 -id "v16" -desc "vs v9       "
python .\client.py gym -r "dotnet ..\artefacts\marfu\BotMarfu.dll" -r "dotnet ..\artefacts\opponent_v14\BotMarfu.dll" -b "..\halite.exe" -i 100 -W 240 -H 160 -id "v16" -desc "vs v14      "
python .\client.py gym -r "dotnet ..\artefacts\marfu\BotMarfu.dll" -r "dotnet ..\artefacts\opponent_v15\BotMarfu.dll" -b "..\halite.exe" -i 100 -W 240 -H 160 -id "v16" -desc "vs v15      "
python .\client.py gym -r "dotnet ..\artefacts\marfu\BotMarfu.dll" -r "dotnet ..\artefacts\opponent_v10\BotMarfu.dll" -r "dotnet ..\artefacts\opponent_v6\BotMarfu.dll" -r "dotnet ..\artefacts\opponent_v5\BotMarfu.dll" -b "..\halite.exe" -i 50 -W 240 -H 160 -id "v16" -desc "vs v10 v6 v5"