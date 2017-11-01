using System;
using System.Diagnostics;
using BotMarfu.core.Headquarter;
using Halite2.hlt;

namespace BotMarfu
{
    public class MyBot
    {
        public static void Main(string[] args)
        {
            //while(!Debugger.IsAttached) { }

            var round = 0;
            try
            {
                var botName = args.Length > 0 ? args[0] : "Marfu_v11";

                var networking = new Networking();
                var gameMap = networking.Initialize(botName);

                var general = new General();
                var coordinator = new Coordinator(gameMap, general);

                for (; ; )
                {
                    round++;
                    if (!UpdateTick(gameMap))
                        return;

                    var commands = coordinator.DoCommands(round);

                    Networking.SendMoves(commands);
                }
            }
            catch (Exception e)
            {
                DebugLog.AddLog(round, $"Exception: {e}");
                throw;
            }
        }

        private static bool UpdateTick(GameMap gameMap)
        {
            try
            {
                gameMap.UpdateMap(Networking.ReadLineIntoMetadata());
                return true;
            }
            catch (Exception)
            {
                return false;                
            }
        }
    }
}
