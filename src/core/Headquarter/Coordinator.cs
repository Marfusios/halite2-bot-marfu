using System;
using System.Collections.Generic;
using System.Linq;
using BotMarfu.core.Missions;
using BotMarfu.core.Moves;
using BotMarfu.core.Ships;
using Halite2.hlt;

namespace BotMarfu.core.Headquarter
{
    class Coordinator
    {
        private readonly GameMap _map;
        private readonly General _general;
        private readonly Strategist _strategist;
        private readonly Navigator _navigator;
        private readonly ShipRegistrator _shipRegistrator;

        private int _createdShips;

        public Coordinator(GameMap map, General general)
        {
            Validations.ValidateInput(map, nameof(GameMap));
            Validations.ValidateInput(general, nameof(General));

            _general = general;
            _map = map;
            _navigator = new Navigator(map, _general);
            _shipRegistrator = new ShipRegistrator();
            _strategist = new Strategist(_map, _general, _navigator, _shipRegistrator);

            _general.AdjustInitialStrategy(_map);
        }

        public Move[] DoCommands(int round)
        {
            _general.AdjustGlobalStrategy(_map, round);
            _navigator.Update(round);
            _strategist.Update(round);

            var player = _map.GetMyPlayer();
            var ships = player.GetShips();
            var shipsSafe = ships.Take(150).ToArray();

            DebugLog.AddLog(round, "SHIPS " + ships.Count);

            //if (round < 10 && ships.Count < 3)
            //{
            //    DebugLog.AddLog(round, "[ERROR] too low ships " + ships.Count);
            //}

            _shipRegistrator.UpdateRegistration(ships);

            foreach (var currentShip in _shipRegistrator.ToArray())
            {
                if(currentShip.State == ShipRegistrator.ShipState.Destroyed)
                    HandleDestroyedShip(currentShip.Captain);
            }

            var commands = new List<Move>();
            foreach (var ship in shipsSafe)
            {
                var found = _shipRegistrator.Find(_map, ship.Key);
                var state = found.State;
                var captain = found.Captain;
                switch (state)
                {
                    case ShipRegistrator.ShipState.New:
                        _createdShips++;
                        captain.AssignMission(_strategist.GenerateMissionForNewShip(captain, _createdShips));
                        break;
                    case ShipRegistrator.ShipState.Destroyed:
                        captain.AssignMission(MissionVoid.Null);                        
                        break;
                }
                var command = captain.ExecuteCommand(this);
                if(command == null || command == NullMove.Null)
                    continue;
                var correctedCommand = CorrectCommand(command, commands, round);
                commands.Add(correctedCommand);
            }

            //LogState(round);
            //_strategist.LogState(round);
            return commands.ToArray();
        }

        private void HandleDestroyedShip(ShipCaptain captain)
        {
            _shipRegistrator.Remove(captain.ShipId);
            _strategist.HandleDestroyedShip(captain);
        }

        public IMission GiveMeNewMission(ShipCaptain shipCaptain)
        {
            return _strategist.GenerateMissionForExistingShip(shipCaptain);
        }


        private Move CorrectCommand(Move move, List<Move> futureMoves, int round)
        {
            var extended = futureMoves
                .Where(x => x is ThrustMoveExtended)
                .Cast<ThrustMoveExtended>()
                .ToArray();

            var m = move as ThrustMoveExtended;
            if (m != null)
            {
                var collision = false;
                var safeCollision = false;
                var collisionThrust = m.GetThrust();
                foreach (var otherMove in extended)
                {
                    if (Collision.TwoLineSegmentIntersect(m.GetShip(), m.FuturePosition, otherMove.GetShip(),
                        otherMove.FuturePosition, round))
                    {
                        //DebugLog.AddLog(round, $"Two line intersection ship1: {m.GetShip().GetId()} ship2: {otherMove.GetShip().GetId()}");
                        collision = true;
                        break;
                    }
                    var distance = m.FuturePosition.GetDistanceTo(otherMove.FuturePosition);
                    if (distance < m.GetShip().GetRadius() + 0.51)
                    {
                        collision = true;
                        break;
                    }
                    //if (distance < m.GetShip().GetRadius() + 1.51)
                    //{
                    //    safeCollision = true;
                    //    collisionThrust = Math.Min(otherMove.GetThrust(), collisionThrust);
                    //    break;
                    //}
                    //if (round < 10)
                    //{
                        //distance = m.FuturePosition.GetDistanceTo(otherMove.FuturePosition);
                        //if (distance < m.GetShip().GetRadius() + 7)
                        //{
                        //    collision = true;
                        //    break;
                        //}
                        
                    //}
                }
                return collision ? NullMove.Null :
                    safeCollision ? m.Clone(Math.Max(collisionThrust-1, 0)) : m;
            }
            return move;
        }

        private void LogState(int round)
        {
            var currentMissions = _shipRegistrator.GroupBy(x => x.Captain.CurrentMission.GetType()).Select(x => $"{x.Key.Name}: {x.Count()}");
            //var settlers = _shipRegistrator.Where(x => x.Captain.CurrentMission is SettlerMission)
            //    .Select(x => x.Captain.CurrentMission).Cast<SettlerMission>();
            //var docking = settlers.Count(x => x.Status == Ship.DockingStatus.Docking);
            var log = string.Join(", ", currentMissions);
            DebugLog.AddLog(round, $"[COORDIN] {log}");
            //DebugLog.AddLog(round, $"[!!! docking] {docking}");
        }
    }
}
