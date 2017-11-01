using System;
using System.Collections.Generic;
using System.Linq;
using BotMarfu.core.Missions;
using BotMarfu.core.Ships;
using Halite2.hlt;

namespace BotMarfu.core.Headquarter
{
    class Strategist
    {
        private readonly Random _random = new Random();
        private readonly GameMap _map;
        private readonly General _general;
        private readonly Navigator _navigator;

        private readonly IDictionary<int, PlanetStrategy> _planetToStrategy = new Dictionary<int, PlanetStrategy>();
        private readonly IDictionary<int, EnemyShipStrategy> _enemyShipToStrategy = new Dictionary<int, EnemyShipStrategy>();

        private int _round;
        private readonly Dictionary<int, int> _initialShipToPlanet = new Dictionary<int, int>();

        public Strategist(GameMap map, General general, Navigator navigator)
        {
            Validations.ValidateInput(map, nameof(GameMap));
            Validations.ValidateInput(general, nameof(General));

            _map = map;
            _general = general;
            _navigator = navigator;

            FillPlanets();
            InitializeInitShipToPlanet();
        }

        public IMission GenerateMissionForExistingShip(ShipCaptain shipCaptain)
        {
            RemoveShipFromStats(shipCaptain);
            return GenerateMissionForShip(shipCaptain, _general.InitialSettlersCount + 1, false);
        }

        public IMission GenerateMissionForNewShip(ShipCaptain shipCaptain, int createdShips)
        {
            return GenerateMissionForShip(shipCaptain, createdShips, true);
        }

        private IMission GenerateMissionForShip(ShipCaptain shipCaptain, int createdShips, bool isNew)
        {
            var nearestEnemyShips = GetNearestEnemyShips(shipCaptain);
            if (nearestEnemyShips.Any())
            {
                var m = GenerateKillerMission(shipCaptain, nearestEnemyShips);
                if (m != MissionVoid.Null)
                    return m;
            }

            //if (createdShips <= 3)
            //{
            //    return GenerateSettlerMissionForBootstrap(shipCaptain);
            //}

            var nearest = FindNearestPlanets(shipCaptain);
            if (createdShips <= _general.InitialSettlersCount)
            {
                return GenerateSettlerMission(shipCaptain, nearest, false, true);
            }

            var random = _random.NextDouble();
            if (random <= _general.SettlerRatio)
            {
                var m = GenerateSettlerMission(shipCaptain, nearest, isNew, false);
                if (m != MissionVoid.Null)
                    return m;
            }
            if (random <= _general.AttackerRatio)
                return GenerateAttackerMission(shipCaptain, nearest);
            if (random <= _general.DefenderRatio)
            {
                var m = GenerateDefenderMission(shipCaptain, nearest, isNew);
                if (m != MissionVoid.Null)
                    return m;
            }

            // Default
            return GenerateAttackerMission(shipCaptain, nearest, true);
        }

        private void InitializeInitShipToPlanet()
        {
            var ships = _map.GetMyPlayer().GetShips();
            var shipsToPlanet = ships.ToDictionary(xx => xx.Key, x => new
                {
                    ship = x.Value,
                    planets = _map
                        .GetAllPlanets()
                        .Select(y => new
                        {
                            dist = x.Value.GetDistanceTo(y.Value),
                            planet = y.Value
                        })
                        .OrderBy(z => z.dist)
                        .Take(3)
                        .ToArray()
                })
                .OrderBy(x => x.Value.planets[0].dist)
                .ToArray();

            var lockedPlanets = new HashSet<int>();

            foreach (var ship in shipsToPlanet)
            {
                foreach (var planet in ship.Value.planets)
                {
                    var planetId = planet.planet.GetId();
                    if (lockedPlanets.Contains(planetId))
                        continue;
                    lockedPlanets.Add(planetId);
                    _initialShipToPlanet.Add(ship.Key, planetId);
                    break;
                }
            }
        }

        private Dictionary<int, Ship> GetNearestEnemyShips(ShipCaptain shipCaptain)
        {
            if (shipCaptain.CurrentMission != null && shipCaptain.CurrentMission.EnemySpotted)
            {
                return shipCaptain.CurrentMission.EnemiesInRange;
            }

            if (shipCaptain.CurrentMission == null || shipCaptain.CurrentMission == MissionVoid.Null)
            {
                return _navigator.FindNearestEnemyShips(shipCaptain.Ship, _general.EnemyCheckRadiusForNew);
            }
            return new Dictionary<int, Ship>();
        }

        public void HandleDestroyedShip(ShipCaptain shipCaptain)
        {
            RemoveShipFromStats(shipCaptain);
        }

        private void RemoveShipFromStats(ShipCaptain shipCaptain)
        {
            var id = shipCaptain.ShipId;
            foreach (var planetStrategy in _planetToStrategy)
            {
                var strat = planetStrategy.Value;
                strat.ShipsForwardedToAttack.Remove(id);
                strat.ShipsForwardedToSettle.Remove(id);
                strat.ShipsForwardedToDefend.Remove(id);
            }
            foreach (var enemyShipStrategy in _enemyShipToStrategy)
            {
                enemyShipStrategy.Value.KillersForwardedToEnemy.Remove(id);
            }
        }

        private IMission GenerateSettlerMissionForBootstrap(ShipCaptain captain)
        {
            var planetId = _initialShipToPlanet[captain.ShipId];
            return new SettlerMission(planetId, _navigator);
        }

        
        private IMission GenerateSettlerMission(ShipCaptain captain, Planet[] nearest, bool isNewShip, bool isInitialSettler)
        {
            var planets = nearest
                .Select(x => _planetToStrategy[x.GetId()])
                .Where(x => x.CanSettle(isNewShip, isInitialSettler))
                .Take(isNewShip ? 1 : _general.NearestPlanetCount)
                .ToArray();

            var targetPlanet = GetTargetPlanet(planets, captain);
            if(targetPlanet == null)
                return MissionVoid.Null;

            var planetId = targetPlanet.PlanetId;
            targetPlanet.ShipsForwardedToSettle.Add(captain.ShipId);

            return new SettlerMission(planetId, _navigator);
        }

        private IMission GenerateAttackerMission(ShipCaptain captain, Planet[] nearest, bool aggresive = false)
        {
            var planets = new PlanetStrategy[0];
            if (!aggresive)
            {
                planets = nearest
                    .Select(x => _planetToStrategy[x.GetId()])
                    .Where(x => x.CanAttack)
                    .Take(_general.NearestPlanetCount)
                    .OrderByDescending(x => x.Planet.GetRadius())
                    .ToArray();
            }

            var targetPlanet = GetTargetPlanet(planets, captain);
            if (targetPlanet == null)
            {
                planets = nearest
                    .Select(x => _planetToStrategy[x.GetId()])
                    .Where(x => x.IsForeign)
                    .Take(_general.NearestPlanetCount)
                    .OrderByDescending(x => x.Planet.GetRadius())
                    .ToArray();
                targetPlanet = GetTargetPlanet(planets, captain);
                if (targetPlanet == null)
                    return MissionVoid.Null;
            }

            var planetId = targetPlanet.PlanetId;
            targetPlanet.ShipsForwardedToAttack.Add(captain.ShipId);

            return new AttackerMission(planetId, _navigator);
        }

        private IMission GenerateDefenderMission(ShipCaptain captain, Planet[] nearest, bool isNewShip)
        {
            var planets = nearest
                .Select(x => _planetToStrategy[x.GetId()])
                .Where(x => x.CanDefend)
                .Take(isNewShip ? 1 : _general.NearestPlanetCount)
                .OrderByDescending(x => x.Planet.GetRadius());

            var targetPlanet = planets.FirstOrDefault();
            if (targetPlanet == null)
            {
                return MissionVoid.Null;
            }

            var planetId = targetPlanet.PlanetId;
            targetPlanet.ShipsForwardedToDefend.Add(captain.ShipId);

            return new DefenderMission(planetId, _general.DefenderMaxRounds);
        }

        private IMission GenerateKillerMission(ShipCaptain captain, Dictionary<int, Ship> nearestEnemyShips)
        {
            foreach (var enemy in nearestEnemyShips)
            {
                var strategy = FindEnemyShip(enemy.Key);
                if(!strategy.CanKill)
                    continue;

                strategy.KillersForwardedToEnemy.Add(captain.ShipId);
                return new KillerMission(enemy.Key, enemy.Value.GetOwner());
            }
            return MissionVoid.Null;
        }

        private PlanetStrategy GetTargetPlanet(PlanetStrategy[] planets, ShipCaptain captain)
        {
            var formatted = planets
                .Select(x => new
                {
                    strategy = x,
                    size = x.Planet.GetRadius(),
                    distance = captain.Ship.GetDistanceTo(x.Planet) + x.PlanetId / 1000.0,
                    distanceToCenter = x.Planet.GetDistanceTo(_navigator.MapCenter)
                })
                .OrderByDescending(x => x.size)
                .ToDictionary(x => x.distance, y => y);

            if (!formatted.Any())
                return null;

            var minDist = formatted.Min(x => x.Key);
            var shouldUseMinDist = true;
            foreach (var planet in formatted)
            {
                if(Math.Abs(planet.Key - minDist) < 0.00001)
                    continue;
                if (planet.Key % minDist < _general.NearestPlanetMaxDistanceRatio)
                    shouldUseMinDist = false;
            }

            if (shouldUseMinDist)
                return formatted[minDist].strategy;

            var ordered = formatted
                .OrderBy(x => x.Value.distanceToCenter)
                .Skip(1)
                .OrderByDescending(x => x.Value.size)
                .ToArray();
            if (ordered.Any())
                return ordered.First().Value.strategy;
            return formatted.First().Value.strategy;
        }

        private Planet[] FindNearestPlanets(ShipCaptain shipCaptain)
        {
            var planets = _map.GetAllPlanets().Values;
            var ship = shipCaptain.Ship;

            return planets
                .Select(x => new
                {
                    dist = ship.GetDistanceTo(x),
                    planet = x
                })
                .OrderBy(x => x.dist)
                .Select(x => x.planet)
                .ToArray();
        }

        private void FillPlanets()
        {
            foreach (var planet in _map.GetAllPlanets())
            {
                _planetToStrategy[planet.Key] = new PlanetStrategy(planet.Key, _map);
            }
        }

        private EnemyShipStrategy FindEnemyShip(int enemyShipId)
        {
            if(!_enemyShipToStrategy.ContainsKey(enemyShipId))
                _enemyShipToStrategy[enemyShipId] = new EnemyShipStrategy(enemyShipId, _map, _general);
            return _enemyShipToStrategy[enemyShipId];
        }

        private class PlanetStrategy
        {
            private readonly GameMap _map;
            private readonly int _playerId;

            public PlanetStrategy(int planetId, GameMap map)
            {
                PlanetId = planetId;
                _map = map;
                _playerId = map.GetMyPlayerId();
            }

            public int PlanetId { get; }
            public Planet Planet => _map.GetPlanet(PlanetId);
            public bool IsOur => Planet.GetOwner() == _playerId;
            public bool IsFree => !Planet.IsOwned();
            public bool IsOurOrFree => IsOur || IsFree;
            public bool IsForeign => !IsOurOrFree;

            public HashSet<int> ShipsForwardedToSettle { get; } = new HashSet<int>();
            public HashSet<int> ShipsForwardedToAttack { get; } = new HashSet<int>();
            public HashSet<int> ShipsForwardedToDefend { get; } = new HashSet<int>();

            public bool CanSettle(bool isNew, bool isInitial) => IsOurOrFree && 
                                     !Planet.IsFull() &&
                                     ShipsForwardedToSettle.Count < 
                                        Planet.GetDockingSpots() + (isNew ? 1 : 0); // settle to n-1 if not new ship

            public bool CanAttack => IsForeign &&
                                     ShipsForwardedToAttack.Count < 2 * Planet.GetDockingSpots();

            public bool CanDefend => IsOur &&
                                     ShipsForwardedToDefend.Count < 2 * Planet.GetDockedShips().Count;
        }

        private class EnemyShipStrategy
        {
            private readonly GameMap _map;
            private readonly General _general;
            private readonly int _playerId;

            public EnemyShipStrategy(int enemyShipId, GameMap map, General general)
            {
                _map = map;
                _general = general;
                _playerId = map.GetMyPlayerId();
            }

            public bool CanKill => KillersForwardedToEnemy.Count < _general.KillersPerEnemyShip;

            public HashSet<int> KillersForwardedToEnemy { get; } = new HashSet<int>();
        }

        public void LogState(int round)
        {
            var settle = _planetToStrategy.Sum(x => x.Value.ShipsForwardedToSettle.Count);
            var attack = _planetToStrategy.Sum(x => x.Value.ShipsForwardedToAttack.Count);
            var defend = _planetToStrategy.Sum(x => x.Value.ShipsForwardedToDefend.Count);
            DebugLog.AddLog(round, $"[STRATEG] AttackerMission: {attack}, SettlerMission: {settle}, DefenderMission: {defend}");
        }

        public void Update(int round)
        {
            _round = round;
        }
    }
}
