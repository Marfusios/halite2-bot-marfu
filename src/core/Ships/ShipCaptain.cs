using BotMarfu.core.Headquarter;
using BotMarfu.core.Missions;
using Halite2.hlt;

namespace BotMarfu.core.Ships
{
    class ShipCaptain
    {
        private readonly int _shipId;
        private readonly int _playerId;
        private readonly GameMap _map;

        private IMission _currentMission = MissionVoid.Null;

        public ShipCaptain(int shipId, GameMap map)
        {
            Validations.ValidateInput(map, nameof(GameMap));

            _shipId = shipId;
            _map = map;
            _playerId = map.GetMyPlayerId();
        }

        public int ShipId => _shipId;
        public IMission CurrentMission => _currentMission;
        public Ship Ship => _map.GetShip(_playerId, _shipId);

        public Move ExecuteCommand(Coordinator coordinator)
        {
            var ship = Ship;
            if (!_currentMission.CanExecute(_map, ship))
                _currentMission = coordinator.GiveMeNewMission(this);
            return _currentMission.Execute(_map, ship);
        }

        public void AssignMission(IMission mission)
        {
            _currentMission = mission;
        }
    }
}
