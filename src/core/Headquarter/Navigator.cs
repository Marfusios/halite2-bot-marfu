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


            _horizontalTiles = width / _tileSize;
            _verticalTiles = height / _tileSize;
            _shipsPerTile = new Dictionary<int,Ship>[_horizontalTiles, _verticalTiles];
        }

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
            var result = new Dictionary<int, Ship>();
            var breath = radius;

            var horizontal = (int)Math.Abs(position.GetXPos()-1) / _tileSize;
            var vertical = (int)Math.Abs(position.GetYPos()-1) / _tileSize;

            var startHor = horizontal - breath;
            var startVer = vertical - breath;

            for (int i = startHor; i <= startHor + breath; i++)
            {
                if (UnCorrectHorizontalTile(i))
                    continue;
                for (int j = startVer; j <= startVer + breath; j++)
                {
                    if(UnCorrectVerticalTile(j))
                        continue;
                    var tile = _shipsPerTile[i, j];
                    CopyShips(tile, result);
                }
            }

            return result;
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

        private void CopyShips(Dictionary<int, Ship> from, Dictionary<int, Ship> to)
        {
            foreach (var ship in from)
            {
                if(!to.ContainsKey(ship.Key))
                    to.Add(ship.Key, ship.Value);
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
    }
}
