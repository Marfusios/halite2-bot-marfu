using System;
using System.Collections.Generic;
using System.Linq;
using Halite2.hlt;

namespace BotMarfu.core.Headquarter
{
    class Navigator
    {
        private readonly GameMap _map;
        private readonly General _general;

        private readonly int _horizontalTiles;
        private readonly int _verticalTiles;

        private readonly int _tileSize = Constants.MAX_SPEED - 1;

        private readonly Dictionary<int,Ship>[,] _shipsPerTile;

        public Navigator(GameMap map, General general)
        {
            Validations.ValidateInput(map, nameof(map));

            _map = map;
            _general = general;
            var width = map.GetWidth();
            var height = map.GetHeight();
            MapCenter = new Position(width/2.0, height/2.0);


            _horizontalTiles = width / _tileSize;
            _verticalTiles = height / _tileSize;
            _shipsPerTile = new Dictionary<int,Ship>[_horizontalTiles, _verticalTiles];
        }

        public Position MapCenter { get; }

        public void Update()
        {
            InitTiles();

            var ships = _map.GetAllShips();
            foreach (var ship in ships)
            {
                var horizontal = (int)Math.Abs(ship.GetXPos()-1) / _tileSize;
                var vertical = (int)Math.Abs(ship.GetYPos()-1) / _tileSize;

                if(UnCorrectHorizontalTile(horizontal) || UnCorrectVerticalTile(vertical))
                    continue;

                var tile = _shipsPerTile[horizontal, vertical];
                if (!tile.ContainsKey(ship.GetId()))
                    tile.Add(ship.GetId(), ship);
            }
        }

        public Dictionary<int, Ship> FindNearestShips(Position position, int radius)
        {
            var result = new Dictionary<int, ShipTilePosition>();
            var breath = radius;

            var horizontal = (int)Math.Abs(position.GetXPos()-1) / _tileSize;
            var vertical = (int)Math.Abs(position.GetYPos()-1) / _tileSize;

            var startHor = horizontal - breath;
            var startVer = vertical - breath;

            for (int i = startVer; i <= startVer + breath; i++)
            {
                if (UnCorrectVerticalTile(i))
                    continue;
                for (int j = startHor; j <= startHor + breath; j++)
                {
                    if(UnCorrectHorizontalTile(j))
                        continue;
                    var tile = _shipsPerTile[j, i];
                    var distance = Math.Abs(j - horizontal) + Math.Abs(i - vertical);
                    CopyShips(tile, result, distance);
                }
            }

            return result
                .OrderBy(x => x.Value.Distance)
                .ThenByDescending(x => (int)x.Value.Ship.GetDockingStatus())
                .ToDictionary(x => x.Key, y => y.Value.Ship);
        }

        private bool UnCorrectVerticalTile(int j)
        {
            return j < 0 || j >= _verticalTiles;
        }

        private bool UnCorrectHorizontalTile(int i)
        {
            return i < 0 || i >= _horizontalTiles;
        }

        public Dictionary<int, Ship> FindNearestEnemyShips(Position position, int radius)
        {
            var nearest = FindNearestShips(position, radius);
            return nearest
                .Where(x => x.Value.GetOwner() != _map.GetMyPlayerId())
                .ToDictionary(x => x.Key, y => y.Value);
        }

        public Dictionary<int, Ship> FindNearestEnemyShips(Position position)
        {
            return FindNearestEnemyShips(position, _general.EnemyCheckRadius);
        }

        public Dictionary<int, Ship> FindNearestOurShips(Position firstEnemy, int radius)
        {
            var nearest = FindNearestShips(firstEnemy, radius);
            return nearest
                .Where(x => x.Value.GetOwner() == _map.GetMyPlayerId())
                .ToDictionary(x => x.Key, y => y.Value);
        }

        private void CopyShips(Dictionary<int, Ship> from, Dictionary<int, ShipTilePosition> to, int distance)
        {
            foreach (var ship in from)
            {
                if(!to.ContainsKey(ship.Key))
                    to.Add(ship.Key, new ShipTilePosition(ship.Value, distance));
            }
        }

        private void InitTiles()
        {
            for (int i = 0; i < _horizontalTiles; i++)
            {
                for (int j = 0; j < _verticalTiles; j++)
                {
                    _shipsPerTile[i,j] = new Dictionary<int,Ship>();
                }
            }
            
        }

        public class ShipTilePosition
        {
            public ShipTilePosition(Ship ship, int distance)
            {
                Ship = ship;
                Distance = distance;
            }

            public Ship Ship { get; }
            public int Distance { get; }
        }
    }
}
