using System;
using System.Collections.Generic;
using System.Diagnostics;
using BotMarfu.core;
using Halite2.hlt;

namespace BotMarfu
{
    public class MyBot
    {

        public static void Main(string[] args)
        {
            //while(!Debugger.IsAttached) { }
            string name = args.Length > 0 ? args[0] : "Marfu_v4";

            var networking = new Networking();
            var gameMap = networking.Initialize(name);

            var shipRegistrator = new ShipRegistrator();

            var step = 0;
            var moveList = new List<Move>();
            for (; ; )
            {
                step++; 

                moveList.Clear();
                if (!UpdateTick(gameMap))
                    return;

                var shipCount = 0;
                foreach (var ship in gameMap.GetMyPlayer().GetShips().Values)
                {
                    var shipCoordinator = shipRegistrator.Find(gameMap, ship.GetId());

                    shipCoordinator.ComputeNextMove(shipCount);
                    
                    if (shipCoordinator.NextMove != null)
                        moveList.Add(shipCoordinator.NextMove);
                    shipCount++;
                }
                Networking.SendMoves(moveList);
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
                //return true;
                return false;                
            }
        }
    }
}
