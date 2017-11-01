using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Halite2.hlt;

namespace BotMarfu.core.Ships
{
    class ShipRegistrator : IEnumerable<ShipRegistrator.ShipRegistration>
    {
        private readonly Dictionary<int, ShipRegistration> _ships = new Dictionary<int, ShipRegistration>();

        public void UpdateRegistration(IDictionary<int, Ship> currentShips)
        {
            foreach (var ship in _ships.Values.ToArray())
            {
                var id = ship.Captain.ShipId;
                if (currentShips.ContainsKey(id))
                {
                    _ships[id] = ship.Clone(ShipState.Normal);
                }
                else
                {
                    _ships[id] = ship.Clone(ShipState.Destroyed);
                }
            }
        }

        public void RegisterIfNeeded(GameMap gameMap, int shipId)
        {
            if (!_ships.ContainsKey(shipId))
            {
                var captain = new ShipCaptain(shipId, gameMap);
                _ships[shipId] = new ShipRegistration(captain);
            }

        }

        public ShipRegistration Find(GameMap gameMap, int shipId)
        {
            RegisterIfNeeded(gameMap, shipId);
            return _ships[shipId];
        }

        public void Remove(int shipId)
        {
            if (_ships.ContainsKey(shipId))
                _ships.Remove(shipId);
        }

        public IEnumerator<ShipRegistration> GetEnumerator()
        {
            return _ships.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public class ShipRegistration
        {
            public ShipRegistration(ShipCaptain captain)
            {
                State = ShipState.New;
                Captain = captain;
            }

            public ShipRegistration(ShipCaptain captain, ShipState state)
            {
                State = state;
                Captain = captain;
            }

            public ShipState State { get; set; }
            public ShipCaptain Captain { get; }

            public ShipRegistration Clone(ShipState newState)
            {
                return new ShipRegistration(Captain, newState);
            }
        }

        public enum ShipState
        {
            Normal,
            New,
            Destroyed
        }
    }
}
