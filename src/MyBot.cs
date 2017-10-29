using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BotMarfu.core;
using BotMarfu.core.Moves;
using Halite2.hlt;

namespace BotMarfu
{
    public class MyBot
    {

        public static void Main(string[] args)
        {
            //while(!Debugger.IsAttached) { }
            string name = args.Length > 0 ? args[0] : "Marfu_v5";

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

                    shipCoordinator.ComputeNextMove(shipCount, GetThrustMoves(moveList));
                    
                    if (shipCoordinator.NextMove != null)
                        moveList.Add(shipCoordinator.NextMove);
                    shipCount++;
                }
                Networking.SendMoves(moveList);
            }
        }

        private static ThrustMoveExtended[] GetThrustMoves(List<Move> moveList)
        {
            return moveList
                .Where(x => x is ThrustMoveExtended)
                .Cast<ThrustMoveExtended>()
                .ToArray();
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
