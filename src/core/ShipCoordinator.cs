using System;
using System.Collections.Generic;
using System.Linq;
using BotMarfu.core.Moves;
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

        public void ComputeNextMove(int shipCount, ThrustMoveExtended[] futureMoves)
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
                ComputeNextMoveForExpanding(_gameMap, player, nearest, shipCount, futureMoves);

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

        private void ComputeNextMoveForExpanding(GameMap map, Player player, Planet[] nearest, int shipCount, ThrustMoveExtended[] futureMoves)
        {
            // var nearestSafe = nearest.Skip(shipCount).ToArray();
            foreach (var planet in nearest)
            {
                if (planet.IsOwned())
                {
                    var isOurs = planet.GetOwner() == player.GetId();
                    if (isOurs && !planet.IsFull())
                    {
                        SetNextMove(MoveOrDock(map, planet), futureMoves);
                        break;
                    }
                }

                if(planet.IsOwned())
                    continue;

                SetNextMove(MoveOrDock(map, planet), futureMoves);
                break;
            }

            if(NextMove != null || NextMove == NullMove.Null)
                return;

            var nearestForeign = nearest.Where(x => x.GetOwner() != player.GetId()).ToArray();
            if (nearestForeign.Length <= 0)
                return;
            var foreignPlanet = nearestForeign[(nearestForeign.Length - 1) % ShipId];

            if (_unchangedPositionCount < 3)
            {
                var docked = foreignPlanet.GetDockedShips();
                if (docked.Any())
                {
                    var targetId = docked[(docked.Count - 1) % ShipId];
                    var target = _gameMap.GetShip(foreignPlanet.GetOwner(), targetId);
                    SetNextMove(Move(_gameMap, target), futureMoves);
                    return;
                }

                SetNextMove(MoveOrDock(map, foreignPlanet), futureMoves);
                return;
            }
            var move = new ThrustMove(GetShip(), 90 + 180 * (ShipId % 2), Constants.MAX_SPEED - 1);
            SetNextMove(move, futureMoves);
        }

        private Move MoveOrDock(GameMap map, Planet planet)
        {
            if(GetShip().CanDock(planet))
                return new DockMove(GetShip(), planet);
            return NavigationExtended.NavigateShipToDock(map, GetShip(), planet, Constants.MAX_SPEED - 1) ?? NullMove.Null;
        }

        private Move Move(GameMap map, Entity entity)
        {
            return NavigationExtended.NavigateShipToDock(map, GetShip(), entity, Constants.MAX_SPEED - 1) ?? NullMove.Null;
        }

        private void SetNextMove(Move move, ThrustMoveExtended[] futureMoves)
        {
            var m = move as ThrustMoveExtended;
            if (m != null)
            {
                var collision = false;
                foreach (var otherMove in futureMoves)
                {
                    var distance = m.FuturePosition.GetDistanceTo(otherMove.FuturePosition);
                    if (distance < m.GetShip().GetRadius() + 0.51)
                    {
                        collision = true;
                        break;
                    }
                }
                NextMove = collision ? m.Clone(Math.Max(m.GetThrust() - 2, 0)) : m;
            }
            else
            {
                NextMove = move;
            }
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
