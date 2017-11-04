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
        private readonly ShipRegistrator _registrator;

        private readonly IDictionary<int, PlanetStrategy> _planetToStrategy = new Dictionary<int, PlanetStrategy>();
        private readonly IDictionary<int, EnemyShipStrategy> _enemyShipToStrategy = new Dictionary<int, EnemyShipStrategy>();

        private int _round;
        //private readonly Dictionary<int, int> _initialShipToPlanet = new Dictionary<int, int>();
        private int _bootstrapKillerShipId = -1;

        public Strategist(GameMap map, General general, Navigator navigator, ShipRegistrator registrator)
        {
            Validations.ValidateInput(map, nameof(GameMap));
            Validations.ValidateInput(general, nameof(General));

            _map = map;
            _general = general;
            _navigator = navigator;
            _registrator = registrator;

            FillPlanets();
            //InitializeInitShipToPlanet();
        }

        public void Update(int round)
        {
            _round = round;

            if (_round == 3)
            {
                foreach (var reg in _registrator)
                {
                    if (reg.Captain.Ship == null || reg.State == ShipRegistrator.ShipState.Destroyed)
                        return;

                    var m = GenerateKillerMissionForBootstrap(reg.Captain);
                    if (m != MissionVoid.Null)
                    {
                        _bootstrapKillerShipId = reg.Captain.ShipId;
                        RemoveShipFromStats(reg.Captain);
                        reg.Captain.AssignMission(m);
                        break;
                    }
                }                     
            }
        }

        public void HelpMe(Dictionary<int, Ship> enemies)
        {
            if (!enemies.Any())
                return;
            var firstEnemy = enemies.First().Value;
            var ourClose = _navigator.FindNearestOurShips(firstEnemy, 6);

            foreach (var ourShip in ourClose)
            {
                var reg = _registrator.Find(_map, ourShip.Key);
                if(reg.Captain.CurrentMission.Important)
                    continue;
                var m = GenerateKillerMission(reg.Captain, enemies, true) as KillerMission;
                if (m != null)
                {
                    RemoveShipFromStats(reg.Captain);
                    var strategy = FindEnemyShip(m.TargetEnemyShipId);
                    strategy.KillersForwardedToEnemy.Add(reg.Captain.ShipId);
                    reg.Captain.AssignMission(m);
                }
            }
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
                var m = GenerateKillerMission(shipCaptain, nearestEnemyShips, false);
                if (m != MissionVoid.Null)
                    return m;
            }

            var nearest = FindNearestPlanets(shipCaptain);

            if (shipCaptain.ShipId == _bootstrapKillerShipId)
            {
                return GenerateAttackerMission(shipCaptain, nearest, true, true);
            }

            if (createdShips <= _general.InitialSettlersCount)
            {
                return GenerateSettlerMission(shipCaptain, nearest, false, createdShips <= 3);
            }

            if (shipCaptain.CurrentMission is AttackerMission)
            {
                var m = GenerateSettlerMission(shipCaptain, nearest, true, false, true);
                if (m != MissionVoid.Null)
                {
                    return m;
                }
            }

            //if (_general.TwoPlayers && isNew)
            //{
            //    var m = GenerateSettlerFarMission(shipCaptain, nearest);
            //    if (m != MissionVoid.Null)
            //    {
            //        return m;
            //    }
            //}

            var random = _random.NextDouble();
            if (random <= _general.SettlerRatio)
            {
                var m = GenerateSettlerMission(shipCaptain, nearest, isNew);
                if (m != MissionVoid.Null)
                    return m;
            }
            if (random > _general.SettlerRatio && random <= _general.AttackerRatio)
                return GenerateAttackerMission(shipCaptain, nearest);
            if (random > _general.AttackerRatio && random <= _general.AttackerFarRatio)
            {
                var m = GenerateAttackerFarMission(shipCaptain, nearest);
                if (m != MissionVoid.Null)
                    return m;
            }
            if (random > _general.AttackerFarRatio && random <= _general.DefenderRatio)
            {
                var m = GenerateDefenderMission(shipCaptain, nearest, isNew);
                if (m != MissionVoid.Null)
                    return m;
            }

            // Default
            return GenerateAttackerMission(shipCaptain, nearest, true);
        }

        private IMission GenerateKillerMissionForBootstrap(ShipCaptain captain)
        {
            var enemy = _map.GetAllShips()
                .Where(x => x.GetOwner() != _map.GetMyPlayerId())
                .Select(x => new {dist = captain.Ship.GetDistanceTo(x), enemy = x})
                .Where(x => x.dist < _general.BootstrapKillerMisionMaxRange)
                .OrderBy(x => x.dist)
                .Select(x => x.enemy)
                .FirstOrDefault();
            if(enemy == null)
                return MissionVoid.Null;
            return new KillerMission(enemy.GetId(), enemy.GetOwner(), true);
        }

        //private void InitializeInitShipToPlanet()
        //{
        //    var ships = _map.GetMyPlayer().GetShips();
        //    var shipsToPlanet = ships.ToDictionary(xx => xx.Key, x => new
        //        {
        //            ship = x.Value,
        //            planets = _map
        //                .GetAllPlanets()
        //                .Select(y => new
        //                {
        //                    dist = x.Value.GetDistanceTo(y.Value),
        //                    planet = y.Value
        //                })
        //                .OrderBy(z => z.dist)
        //                .Take(3)
        //                .ToArray()
        //        })
        //        .OrderBy(x => x.Value.planets[0].dist)
        //        .ToArray();

        //    var lockedPlanets = new HashSet<int>();

        //    foreach (var ship in shipsToPlanet)
        //    {
        //        foreach (var planet in ship.Value.planets)
        //        {
        //            var planetId = planet.planet.GetId();
        //            if (lockedPlanets.Contains(planetId))
        //                continue;
        //            lockedPlanets.Add(planetId);
        //            _initialShipToPlanet.Add(ship.Key, planetId);
        //            break;
        //        }
        //    }
        //}

        private Dictionary<int, Ship> GetNearestEnemyShips(ShipCaptain shipCaptain)
        {
            if (shipCaptain.CurrentMission != null && shipCaptain.CurrentMission.EnemySpotted)
            {
                return shipCaptain.CurrentMission.EnemiesInRange;
            }

            if (shipCaptain.CurrentMission == null || 
                shipCaptain.CurrentMission == MissionVoid.Null)
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

        //private IMission GenerateSettlerMissionForBootstrap(ShipCaptain captain)
        //{
        //    var planetId = _initialShipToPlanet[captain.ShipId];
        //    return new SettlerMission(planetId, _navigator, this);
        //}

        
        private IMission GenerateSettlerMission(ShipCaptain captain, Planet[] nearest, bool isNewShip, 
            bool isStart = false, bool force = false)
        {
            var nearestPlanet = nearest.FirstOrDefault();

            var planets = nearest
                .Select(x => _planetToStrategy[x.GetId()])
                .Where(x => force ? x.CanSettleForce : x.CanSettle(isNewShip, _round))
                .Take(_general.NearestPlanetCount)
                .ToArray();


            PlanetStrategy target = null;
            if (!isStart && nearestPlanet != null && Equals(nearestPlanet, planets.FirstOrDefault()?.Planet))
            {
                target = _planetToStrategy[nearestPlanet.GetId()];
            }
            else
            {
                target = GetTargetPlanet(planets, captain);
            }

            if (target == null)
                return MissionVoid.Null;

            var planetId = target.PlanetId;
            target.ShipsForwardedToSettle.Add(captain.ShipId);

            return new SettlerMission(planetId, _navigator, this);
        }

        private IMission GenerateSettlerFarMission(ShipCaptain captain, Planet[] nearest)
        {
            var nearestPlanet = nearest.FirstOrDefault();

            var planets = nearest
                .Select(x => _planetToStrategy[x.GetId()])
                .Where(x => x.CanSettle(true, _round))
                .Take(_general.NearestPlanetCount)
                .ToArray();


            PlanetStrategy target = null;
            if (nearestPlanet != null && Equals(nearestPlanet, planets.FirstOrDefault()?.Planet))
            {
                target = _planetToStrategy[nearestPlanet.GetId()];
            }
            else
            {
                planets = nearest
                    .Select(x => _planetToStrategy[x.GetId()])
                    .Where(x => x.CanSettleFar)
                    .Take(_general.NearestPlanetCount)
                    .ToArray();

                target = GetTargetPlanet(planets, captain);
            }

            if (target == null)
                return MissionVoid.Null;

            var planetId = target.PlanetId;
            target.ShipsForwardedToSettle.Add(captain.ShipId);

            return new SettlerMission(planetId, _navigator, this);
        }

        private IMission GenerateAttackerMission(ShipCaptain captain, Planet[] nearest, bool aggresive = false, bool mustComplete = false)
        {
            var planets = new PlanetStrategy[0];
            if (!aggresive)
            {
                planets = nearest
                    .Select(x => _planetToStrategy[x.GetId()])
                    .Where(x => x.CanAttack)
                    .Take(mustComplete ? 1 : _general.NearestPlanetCount)
                    .OrderByDescending(x => x.Planet.GetRadius())
                    .ToArray();
            }

            var targetPlanet = GetTargetPlanet(planets, captain);
            if (targetPlanet == null)
            {
                planets = nearest
                    .Select(x => _planetToStrategy[x.GetId()])
                    .Where(x => x.CanAttackAggressive)
                    .Take(mustComplete ? 1 :_general.NearestPlanetCount)
                    .OrderByDescending(x => x.Planet.GetRadius())
                    .ToArray();
                targetPlanet = GetTargetPlanet(planets, captain);
                if (targetPlanet == null)
                    return MissionVoid.Null;
            }

            var planetId = targetPlanet.PlanetId;
            targetPlanet.ShipsForwardedToAttack.Add(captain.ShipId);

            return new AttackerMission(planetId, _navigator, mustComplete);
        }

        private IMission GenerateAttackerFarMission(ShipCaptain captain, Planet[] nearest)
        {
            var farrest = nearest
                .Select(x => new
                {
                    planet = x,
                    size = x.GetRadius(),
                    distanceToCenter = x.GetDistanceTo(_navigator.MapCenter)
                })
                .OrderByDescending(x => x.distanceToCenter);

            var planets = farrest
                .Select(x => _planetToStrategy[x.planet.GetId()])
                .Where(x => x.CanAttack)
                .ToArray();
            var targetPlanet = GetTargetPlanet(planets, captain);
            if (targetPlanet == null)
                return MissionVoid.Null;
            

            var planetId = targetPlanet.PlanetId;
            targetPlanet.ShipsForwardedToAttack.Add(captain.ShipId);

            return new AttackerMission(planetId, _navigator, true);
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

        private IMission GenerateKillerMission(ShipCaptain captain, Dictionary<int, Ship> nearestEnemyShips, bool important)
        {
            foreach (var enemy in nearestEnemyShips)
            {
                var strategy = FindEnemyShip(enemy.Key);
                if(!strategy.CanKill)
                    continue;

                strategy.KillersForwardedToEnemy.Add(captain.ShipId);
                return new KillerMission(enemy.Key, enemy.Value.GetOwner(), important);
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
                if (planet.Key / minDist < _general.NearestPlanetMaxDistanceRatio)
                    shouldUseMinDist = false;
            }

            if (shouldUseMinDist)
                return formatted[minDist].strategy;

            var skipCount = _general.NearestPlanetToCenterSkipCount(_round);
            if (skipCount > 0)
            {
                var ordered = formatted
                    .OrderBy(x => x.Value.distanceToCenter)
                    .Skip(skipCount)
                    .OrderByDescending(x => x.Value.size)
                    .ToArray();
                if (ordered.Any())
                    return ordered.First().Value.strategy;
            }
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
                _planetToStrategy[planet.Key] = new PlanetStrategy(planet.Key, _map, _navigator);
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
            private readonly Navigator _navigator;
            private readonly int _playerId;

            public PlanetStrategy(int planetId, GameMap map, Navigator navigator)
            {
                PlanetId = planetId;
                _map = map;
                _navigator = navigator;
                _playerId = map.GetMyPlayerId();
            }

            public int PlanetId { get; }
            public Planet Planet => _map.GetPlanet(PlanetId);
            public bool IsOur => Planet.GetOwner() == _playerId;
            public bool IsFree => !Planet.IsOwned();
            public bool IsOurOrFree => IsOur || IsFree;
            public bool IsForeign => !IsOurOrFree;

            public bool SomethingIsDocking => _navigator.FindNearestEnemyShips(Planet, 3).Any(x => x.Value.GetDockingProgress() > 0);

            public HashSet<int> ShipsForwardedToSettle { get; } = new HashSet<int>();
            public HashSet<int> ShipsForwardedToAttack { get; } = new HashSet<int>();
            public HashSet<int> ShipsForwardedToDefend { get; } = new HashSet<int>();

            public bool CanSettle(bool isNew, int round) => IsOurOrFree &&
                                                            !Planet.IsFull() &&
                                                            ShipsForwardedToSettle.Count <
                                                                Planet.GetDockingSpots() +
                                                                (isNew ? 1 : 0) && // settle to n-1 if not new ship
                                                            !SomethingIsDocking;

            public bool CanSettleForce => IsOurOrFree &&
                                          !Planet.IsFull() &&
                                          !SomethingIsDocking;

            public bool CanSettleFar => IsFree &&
                                        ShipsForwardedToSettle.Count < 1 &&
                                        !SomethingIsDocking;


            public bool CanAttack => (IsForeign || SomethingIsDocking) &&
                                     ShipsForwardedToAttack.Count < 2 * Planet.GetDockingSpots();

            public bool CanAttackAggressive => IsForeign || SomethingIsDocking;

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
    }
}
