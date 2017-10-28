using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Halite2.hlt;

namespace BotMarfu
{
    public class MyBot
    {

        public static void Main(string[] args)
        {
            //while(!Debugger.IsAttached) { }
            string name = args.Length > 0 ? args[0] : "Marfu";

            var networking = new Networking();
            var gameMap = networking.Initialize(name);

            var step = 0;
            var allOwned = false;
            var shipToDocked = new Dictionary<int, int>();
            var shipToMovingPlanet = new Dictionary<int, int>();

            var moveList = new List<Move>();
            for (; ; )
            {
                step++; 

                moveList.Clear();
                gameMap.UpdateMap(Networking.ReadLineIntoMetadata());

                foreach (Ship ship in gameMap.GetMyPlayer().GetShips().Values)
                {
                    var shipId = ship.GetId();
                    if (!shipToDocked.ContainsKey(shipId))
                        shipToDocked[shipId] = 0;
                    var shipDocketCount = shipToDocked[shipId];
                    if (ship.GetDockingStatus() != Ship.DockingStatus.Undocked)
                    {
                        if (shipDocketCount > 30)
                        {
                            if(gameMap.GetMyPlayer().GetShips().Count(x => x.Value.GetDockedPlanet() == ship.GetDockedPlanet()) <= 1)
                                continue;
                            moveList.Add(new UndockMove(ship));
                            shipToDocked[shipId] = 0;
                        }
                        else
                        {

                            shipToDocked[shipId] = shipToDocked[shipId] + 1;
                        }
                        continue;
                    }

                    if (shipToMovingPlanet.ContainsKey(shipId))
                    {
                        var planet = gameMap.GetPlanet(shipToMovingPlanet[shipId]);

                        if (!planet.IsOwned() || allOwned)
                        {                        

                            if (ship.CanDock(planet))
                            {
                                moveList.Add(new DockMove(ship, planet));
                                shipToMovingPlanet.Remove(shipId);
                                break;
                            }

                            var newThrustMove = Navigation.NavigateShipToDock(gameMap, ship, planet, Constants.MAX_SPEED - 1);
                            if (newThrustMove != null)
                            {
                                moveList.Add(newThrustMove);
                                DebugLog.AddLog($"{step}:\t\tMoving to planet: {planet.GetId()}");
                            }
                        }
                    }
                    else
                    {
                        foreach (Planet planet in GetPlanetsShaked(gameMap))
                        {
                            var planetId = planet.GetId();

                            if (planet.IsOwned())
                            {
                                continue;
                            }

                            if (ship.CanDock(planet))
                            {
                                moveList.Add(new DockMove(ship, planet));
                                shipToMovingPlanet.Remove(shipId);
                                break;
                            }

                            var newThrustMove = Navigation.NavigateShipToDock(gameMap, ship, planet, Constants.MAX_SPEED - 1);
                            if (newThrustMove != null)
                            {
                                moveList.Add(newThrustMove);
                                shipToMovingPlanet[shipId] = planetId;
                            }

                            break;
                        }

                        if (gameMap.GetAllPlanets().All(x => x.Value.IsOwned()))
                        {
                            allOwned = true;
                            var me = gameMap.GetMyPlayer();
                            foreach (var planet in GetPlanetsShaked(gameMap))
                            {
                                if(planet.GetOwner() == me.GetId())
                                    continue;
                                var newThrustMove = Navigation.NavigateShipToDock(gameMap, ship, planet, Constants.MAX_SPEED - 1);
                                if (newThrustMove != null)
                                {
                                    moveList.Add(newThrustMove);
                                    shipToMovingPlanet[shipId] = planet.GetId();
                                    DebugLog.AddLog($"{step}:\t\tMoving to foreign planet: {planet.GetId()}");
                                }
                                break;
                            }
                        }
                    }

                    
                }
                Networking.SendMoves(moveList);
            }
        }

        private static Planet[] GetPlanetsShaked(GameMap gameMap)
        {
            var planets = gameMap.GetAllPlanets().Values.ToArray();
            var rnd = new Random();
            return planets.OrderBy(item => rnd.Next()).ToArray();
        }
    }
}
