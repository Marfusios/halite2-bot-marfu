del *.log
del *.hlt

dotnet build ..\src\BotMarfu.csproj -o ..\artefacts\marfu
python .\client.py gym -r "dotnet ..\artefacts\marfu\BotMarfu.dll" -r "dotnet ..\artefacts\opponent_v6\BotMarfu.dll" -b "..\halite.exe" -i 100 -W 240 -H 160