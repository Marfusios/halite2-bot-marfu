using System.Collections.Generic;
using System.Linq;
using Halite2.hlt;

namespace BotMarfu.core
{
    public class ShipCoordinator
    {
        private readonly GameMap _gameMap;
        private int _firstDockedPlanetId = -1;
        private Position _lastPosition;
        private int _unchangedPositionCount = 0;

        public ShipCoordinator(GameMap gameMap, int shipId)
        {
            Validations.ValidateInput(gameMap, nameof(GameMap));

            ShipId = shipId;
            _gameMap = gameMap;
            IsNew = true;
        }

        public int ShipId { get; }
        public bool IsNew { get; }
        public Move NextMove { get; private set; }

        public Ship GetShip()
        {
            return _gameMap.GetMyPlayer().GetShip(ShipId);
        }

        public Planet[] GetNearestPlanets(Planet[] planets)
        {
            return planets
                .Select(x => new
                {
                    dist = GetShip().GetDistanceTo(x),
                    planet = x
                })
                .OrderBy(x => x.dist)
                .Select(x => x.planet)
                .ToArray();
        }

        public Planet FindCurrentlyDockedPlanet(Planet[] planets)
        {
            var planetId = GetShip().GetDockedPlanet();
            return planets.FirstOrDefault(x => x.GetId() == planetId);
        }

        public void ComputeNextMove(int shipCount)
        {
            NextMove = null;
            UpdateUnchangedPosition();

            var player = _gameMap.GetMyPlayer();
            var planets = _gameMap.GetAllPlanets();

            var currentStatus = GetShip().GetDockingStatus();
            if (currentStatus == Ship.DockingStatus.Docking || currentStatus == Ship.DockingStatus.Undocking)
                return;

            var nearest = GetNearestPlanets(planets.Values.ToArray());
            if (currentStatus == Ship.DockingStatus.Docked)
                ComputeNextMoveForDocked(nearest, FindCurrentlyDockedPlanet(nearest));
            else
                ComputeNextMoveForExpanding(_gameMap, player, nearest, shipCount);

            _lastPosition = (Position)GetShip();
        }

        private void ComputeNextMoveForDocked(Planet[] nearest, Planet current)
        {
            if (_firstDockedPlanetId < 0)
            {
                _firstDockedPlanetId = current.GetId();
                return;
            }

            var dockedShips = current.GetDockedShips().Count;
            var positions = current.GetDockingSpots();
        }

        private void ComputeNextMoveForExpanding(GameMap map, Player player, Planet[] nearest, int shipCount)
        {
            // var nearestSafe = nearest.Skip(shipCount).ToArray();
            foreach (var planet in nearest)
            {
                if (planet.IsOwned())
                {
                    var isOurs = planet.GetOwner() == player.GetId();
                    if (isOurs && !planet.IsFull())
                    {
                        NextMove = MoveOrDock(map, planet);
                        break;
                    }
                }

                if(planet.IsOwned())
                    continue;

                NextMove = MoveOrDock(map, planet);
                break;
            }

            if(NextMove != null || NextMove == NullMove.Null)
                return;

            var nearestForeign = nearest.Where(x => x.GetOwner() != player.GetId()).ToArray();
            foreach (var planet in nearestForeign)
            {
                if (_unchangedPositionCount < 4)
                {
                    NextMove = MoveOrDock(map, planet);
                    break;
                }
                else
                {
                    NextMove = new ThrustMove(GetShip(), 90, Constants.MAX_SPEED / 2);
                    break;
                }
                
            }
        }

        private Move MoveOrDock(GameMap map, Planet planet)
        {
            if(GetShip().CanDock(planet))
                return new DockMove(GetShip(), planet);
            return Navigation.NavigateShipToDock(map, GetShip(), planet, Constants.MAX_SPEED - 1) ?? NullMove.Null;
        }

        private void UpdateUnchangedPosition()
        {
            if (_lastPosition == null)
                return;
            var current = (Position)GetShip();
            if (_lastPosition.Equals(current))
                _unchangedPositionCount++;
            else
                _unchangedPositionCount--;
        }
    }
}
