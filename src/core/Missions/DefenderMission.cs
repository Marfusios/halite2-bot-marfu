using System.Collections.Generic;
using BotMarfu.core.Moves;
using Halite2.hlt;

namespace BotMarfu.core.Missions
{
    class DefenderMission : IMission
    {
        private readonly int _targetPlanetId;
        private readonly int _maxRounds;

        private int _lastDefendedShipId = -1;
        private int _alreadyRounds;
        private int _lastVoidMoves;

        public DefenderMission(int targetPlanetId, int maxRounds)
        {
            _targetPlanetId = targetPlanetId;
            _maxRounds = maxRounds;
        }

        public bool EnemySpotted { get; private set; }
        public Dictionary<int, Ship> EnemiesInRange { get; private set; }
        public bool Important { get; } = false;

        public bool CanExecute(GameMap map, Ship ship)
        {
            _alreadyRounds++;
            if (_alreadyRounds >= _maxRounds)
                return false;

            var planet = map.GetPlanet(_targetPlanetId);
            if (planet == null)
                return false;
            if (!planet.IsOwned())
                return false;
            return true;
        }

        public Move Execute(GameMap map, Ship ship)
        {
            var planet = map.GetPlanet(_targetPlanetId);
            var target = map.GetShip(planet.GetOwner(), _lastDefendedShipId);

            if (target == null)
            {
                _lastVoidMoves = 0;
                var docked = planet.GetDockedShips();
                _lastDefendedShipId = docked[ship.GetId() % docked.Count];
                target = map.GetShip(planet.GetOwner(), _lastDefendedShipId);
            }

            if (_lastVoidMoves > 3)
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
