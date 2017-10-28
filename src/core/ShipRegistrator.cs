using System.Collections;
using System.Collections.Generic;
using Halite2.hlt;

namespace BotMarfu.core
{
    public class ShipRegistrator : IEnumerable<ShipCoordinator>
    {
        private readonly Dictionary<int, ShipCoordinator> _shipToCoordinator = new Dictionary<int, ShipCoordinator>();

        public void RegisterIfNeeded(GameMap gameMap, int shipId)
        {
            if(!_shipToCoordinator.ContainsKey(shipId))
                _shipToCoordinator[shipId] = new ShipCoordinator(gameMap, shipId);
        }

        public ShipCoordinator Find(GameMap gameMap, int shipId)
        {
            RegisterIfNeeded(gameMap, shipId);
            return _shipToCoordinator[shipId];
        }

        public IEnumerator<ShipCoordinator> GetEnumerator()
        {
            return _shipToCoordinator.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
