﻿using System.Collections.Generic;
using System.Linq;
using BotMarfu.core.Headquarter;
using BotMarfu.core.Moves;
using Halite2.hlt;

namespace BotMarfu.core.Missions
{
    class AttackerMission : IMission
    {
        private readonly Navigator _navigator;
        private readonly int _targetPlanetId;

        private int _lastAttackedShipId = -1;
        private int _lastVoidMoves;
        private int _moves;

        public AttackerMission(int targetPlanetId, Navigator navigator)
        {
            _targetPlanetId = targetPlanetId;
            _navigator = navigator;
        }

        public bool EnemySpotted { get; private set; }
        public Dictionary<int, Ship> EnemiesInRange { get; private set; }
        public bool Important { get; } = false;

        public bool CanExecute(GameMap map, Ship ship)
        {
            _moves++;

            var planet = map.GetPlanet(_targetPlanetId);
            if (planet == null)
                return false;
            if (planet.GetOwner() == map.GetMyPlayerId())
                return false;
            if (!planet.GetDockedShips().Any())
                return false;
            if (_lastVoidMoves > 5)
                return false;
            if (_moves < 5)
            {
                EnemiesInRange = _navigator.FindNearestEnemyShips(ship);
                if (EnemiesInRange.Any())
                {
                    EnemySpotted = true;
                    return false;
                }
            }
            return true;
        }

        public Move Execute(GameMap map, Ship ship)
        {
            var planet = map.GetPlanet(_targetPlanetId);
            var target = map.GetShip(planet.GetOwner(), _lastAttackedShipId);

            if (target == null)
            {
                _lastVoidMoves = 0;
                var docked = planet.GetDockedShips();
                _lastAttackedShipId = docked[ship.GetId() % docked.Count];
                target = map.GetShip(planet.GetOwner(), _lastAttackedShipId);
            }

            if (_lastVoidMoves > 2)
                return NullMove.Null;

            var move = Move(map, target, ship);
            UpdateLastVoidMoves(move);
            return move;
        }

        private void UpdateLastVoidMoves(Move move)
        {
            if (move == NullMove.Null)
                _lastVoidMoves++;
            else
                _lastVoidMoves = 0;
        }

        private Move Move(GameMap map, Entity target, Ship ship)
        {
            return NavigationExtended.NavigateShipToDock(map, ship, target, Constants.MAX_SPEED) ?? NullMove.Null;
        }
    }
}
