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

        private readonly IDictionary<int, PlanetStrategy> _planetToStrategy = new Dictionary<int, PlanetStrategy>();

        public Strategist(GameMap map, General general)
        {
            Validations.ValidateInput(map, nameof(GameMap));
            Validations.ValidateInput(general, nameof(General));

            _map = map;
            _general = general;

            FillPlanets();
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
            var nearest = FindNearestPlanets(shipCaptain);
            if (createdShips <= _general.InitialSettlersCount)
            {
                return GenerateSettlerMission(shipCaptain, nearest, false);
            }

            var random = _random.NextDouble();
            if (random <= _general.SettlerRatio)
            {
                var m = GenerateSettlerMission(shipCaptain, nearest, isNew);
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
        }

        private IMission GenerateSettlerMission(ShipCaptain captain, Planet[] nearest, bool isNewShip)
        {
            var planets = nearest
                .Select(x => _planetToStrategy[x.GetId()])
                .Where(x => x.CanSettle)
                .Take(isNewShip ? 1 : _general.NearestCount)
                .OrderByDescending(x => x.Planet.GetRadius());

            var targetPlanet = planets.FirstOrDefault();
            if (targetPlanet == null)
                return MissionVoid.Null;

            var planetId = targetPlanet.PlanetId;
            targetPlanet.ShipsForwardedToSettle.Add(captain.ShipId);

            return new SettlerMission(planetId);
        }

        private IMission GenerateAttackerMission(ShipCaptain captain, Planet[] nearest, bool aggresive = false)
        {
            var planets = new PlanetStrategy[0];
            if (!aggresive)
            {
                planets = nearest
                    .Select(x => _planetToStrategy[x.GetId()])
                    .Where(x => x.CanAttack)
                    .Take(_general.NearestCount)
                    .OrderByDescending(x => x.Planet.GetRadius())
                    .ToArray();
            }

            var targetPlanet = planets.FirstOrDefault();
            if (targetPlanet == null)
            {
                targetPlanet = nearest
                    .Select(x => _planetToStrategy[x.GetId()])
                    .Where(x => x.IsForeign)
                    .Take(_general.NearestCount)
                    .OrderByDescending(x => x.Planet.GetRadius())
                    .FirstOrDefault();
                if (targetPlanet == null)
                    return MissionVoid.Null;
            }

            var planetId = targetPlanet.PlanetId;
            targetPlanet.ShipsForwardedToAttack.Add(captain.ShipId);

            return new AttackerMission(planetId);
        }

        private IMission GenerateDefenderMission(ShipCaptain captain, Planet[] nearest, bool isNewShip)
        {
            var planets = nearest
                .Select(x => _planetToStrategy[x.GetId()])
                .Where(x => x.CanDefend)
                .Take(isNewShip ? 1 : _general.NearestCount)
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
            public bool IsFree => Planet.GetOwner() == -1;
            public bool IsOurOrFree => IsOur || IsFree;
            public bool IsForeign => !IsOurOrFree;

            public HashSet<int> ShipsForwardedToSettle { get; } = new HashSet<int>();
            public HashSet<int> ShipsForwardedToAttack { get; } = new HashSet<int>();
            public HashSet<int> ShipsForwardedToDefend { get; } = new HashSet<int>();

            public bool CanSettle => IsOurOrFree && 
                                     !Planet.IsFull() &&
                                     ShipsForwardedToSettle.Count < Planet.GetDockingSpots();

            public bool CanAttack => IsForeign &&
                                     ShipsForwardedToAttack.Count < 2 * Planet.GetDockingSpots();

            public bool CanDefend => IsOur &&
                                     ShipsForwardedToDefend.Count < 2 * Planet.GetDockedShips().Count;
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
