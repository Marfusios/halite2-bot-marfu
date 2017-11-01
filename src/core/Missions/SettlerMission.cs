using System.Collections.Generic;
using System.Linq;
using BotMarfu.core.Headquarter;
using BotMarfu.core.Moves;
using Halite2.hlt;

namespace BotMarfu.core.Missions
{
    class SettlerMission : IMission
    {
        private readonly Navigator _navigator;
        private readonly Strategist _strategist;
        private readonly int _targetPlanetId;
        private int _lastVoidMoves;
        private int _moves;
        private Ship.DockingStatus _status = Ship.DockingStatus.Undocked;

        public SettlerMission(int targetPlanetId, Navigator navigator, Strategist strategist)
        {
            _targetPlanetId = targetPlanetId;
            _navigator = navigator;
            _strategist = strategist;
        }

        public bool EnemySpotted { get; private set; }
        public Dictionary<int, Ship> EnemiesInRange { get; private set; }
        public bool Important => _status != Ship.DockingStatus.Undocked;
        public Ship.DockingStatus Status => _status;

        public bool CanExecute(GameMap map, Ship ship)
        {
            _status = ship.GetDockingStatus();
            _moves++;

            var planet = map.GetPlanet(_targetPlanetId);
            if (planet == null)
                return false;
            if (planet.IsOwned() && planet.IsFull())
                return false;
            if (!planet.IsOwned() && planet.GetOwner() != -1)
                return false;
            if (_status == Ship.DockingStatus.Undocked && _lastVoidMoves > 2)
                return false;

            var enemies = _navigator.FindNearestEnemyShips(ship);
            if (enemies.Any())
            {
                if (_status == Ship.DockingStatus.Undocked && _moves < 5)
                {
                    EnemiesInRange = enemies;
                    EnemySpotted = true;
                    return false;
                }
                if (_status != Ship.DockingStatus.Undocked)
                {
                    _strategist.HelpMe(enemies);
                }
            }
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

            if (_status == Ship.DockingStatus.Docked)
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
