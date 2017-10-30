using System;
using BotMarfu.core.Headquarter;
using Halite2.hlt;

namespace BotMarfu
{
    public class MyBot
    {
        public static void Main(string[] args)
        {
            //while(!Debugger.IsAttached) { }

            var botName = args.Length > 0 ? args[0] : "Marfu_v7";

            var networking = new Networking();
            var gameMap = networking.Initialize(botName);

            var general = new General();
            var coordinator = new Coordinator(gameMap, general);

            for (; ; )
            {
                if (!UpdateTick(gameMap))
                    return;

                var commands = coordinator.DoCommands();

                Networking.SendMoves(commands);
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
