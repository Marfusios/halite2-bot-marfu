using System.Collections.Generic;
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
        private readonly bool _mustCompleteMission;

        private int _lastAttackedShipOwner = -1;
        private int _lastAttackedShipId = -1;
        private int _lastVoidMoves;
        private int _moves;

        public AttackerMission(int targetPlanetId, Navigator navigator, bool mustCompleteMission = false)
        {
            _targetPlanetId = targetPlanetId;
            _navigator = navigator;
            _mustCompleteMission = mustCompleteMission;
        }

        public bool EnemySpotted { get; private set; }
        public Dictionary<int, Ship> EnemiesInRange { get; private set; }
        public bool Important => false;

        public bool CanExecute(GameMap map, Ship ship)
        {
            _moves++;

            var planet = map.GetPlanet(_targetPlanetId);
            if (planet == null)
                return false;
            if (planet.GetOwner() == map.GetMyPlayerId())
                return false;
            if (_lastVoidMoves > 5)
                return false;
            if (!_mustCompleteMission && _moves < 3)
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
            Ship target = null;

            if (_lastAttackedShipOwner >= 0 && _lastAttackedShipId >= 0)
            {
                target = map.GetShip(_lastAttackedShipOwner, _lastAttackedShipId);
            }
            else if (planet.GetOwner() < 0)
            {
                var enemies = _navigator.FindNearestEnemyShips(planet, 4).Where(x => x.Value.GetDockingProgress() > 0).ToArray();
                if (!enemies.Any())
                {
                    UpdateLastVoidMoves(NullMove.Null);
                    return NullMove.Null;
                }

                target = enemies.First().Value;
                _lastAttackedShipOwner = target.GetOwner();
                _lastAttackedShipId = target.GetId();
            }
            else
            {
                _lastAttackedShipOwner = planet.GetOwner();
                target = map.GetShip(_lastAttackedShipOwner, _lastAttackedShipId);
            }

            if (target == null)
            {
                _lastVoidMoves = 0;
                var docked = planet.GetDockedShips();
                if (docked.Count <= 0)
                {
                    UpdateLastVoidMoves(NullMove.Null);
                    return NullMove.Null;
                }
                _lastAttackedShipId = docked[ship.GetId() % docked.Count];
                target = map.GetShip(planet.GetOwner(), _lastAttackedShipId);
            }

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
