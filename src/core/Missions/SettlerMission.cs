using BotMarfu.core.Moves;
using Halite2.hlt;

namespace BotMarfu.core.Missions
{
    class SettlerMission : IMission
    {
        private readonly int _targetPlanetId;
        private int _lastVoidMoves;

        public SettlerMission(int targetPlanetId)
        {
            _targetPlanetId = targetPlanetId;
        }

        public bool CanExecute(GameMap map, Ship ship)
        {
            var planet = map.GetPlanet(_targetPlanetId);
            if (planet == null)
                return false;
            if (planet.IsOwned() && planet.IsFull())
                return false;
            if (!planet.IsOwned() && planet.GetOwner() != -1)
                return false;
            if (_lastVoidMoves > 2)
                return false;
            return true;
        }

        public Move Execute(GameMap map, Ship ship)
        {
            var move = NullMove.Null;
            var planet = map.GetPlanet(_targetPlanetId);
            if (planet == null)
            {
                UpdateLastVoidMoves(move);
                return NullMove.Null;
            }

            var currentStatus = ship.GetDockingStatus();
            if (currentStatus != Ship.DockingStatus.Undocked)
                return NullMove.Null;

            move = MoveOrDock(map, planet, ship);
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

        private Move MoveOrDock(GameMap map, Planet planet, Ship ship)
        {
            if (ship.CanDock(planet))
                return new DockMove(ship, planet);
            return NavigationExtended.NavigateShipToDock(map, ship, planet, Constants.MAX_SPEED) ?? NullMove.Null;
        }
    }
}
