using System.Collections.Generic;
using BotMarfu.core.Moves;
using Halite2.hlt;

namespace BotMarfu.core.Missions
{
    class KillerMission : IMission
    {
        private readonly int _targetEnemyShipId;
        private readonly int _targetEnemyShipOwnerId;

        private Ship _targetShip;

        public KillerMission(int targetEnemyShipId, int targetEnemyShipOwnerId)
        {
            _targetEnemyShipId = targetEnemyShipId;
            _targetEnemyShipOwnerId = targetEnemyShipOwnerId;
        }

        public bool EnemySpotted { get; private set; }
        public Dictionary<int, Ship> EnemiesInRange { get; private set; }

        public bool CanExecute(GameMap map, Ship ship)
        {
            _targetShip = map.GetShip(_targetEnemyShipOwnerId, _targetEnemyShipId);
            if (_targetShip == null)
                return false;


            return true;
        }

        public Move Execute(GameMap map, Ship ship)
        {
            _targetShip = _targetShip ?? map.GetShip(_targetEnemyShipOwnerId, _targetEnemyShipId);

            var move = Move(map, _targetShip, ship);
            return move;
        }

        private Move Move(GameMap map, Entity target, Ship ship)
        {
            return NavigationExtended.NavigateShipToDock(map, ship, target, Constants.MAX_SPEED) ?? NullMove.Null;
        }
    }
}
