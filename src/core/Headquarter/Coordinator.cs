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
            _strategist = new Strategist(_map, _general, _navigator);
            _shipRegistrator = new ShipRegistrator();

            _general.AdjustInitialStrategy(_map);
        }

        public Move[] DoCommands(int round)
        {
            _general.AdjustGlobalStrategy(_map, round);
            _navigator.Update();

            var player = _map.GetMyPlayer();
            var ships = player.GetShips();
            var allShips = _map.GetAllShips();

            _shipRegistrator.UpdateRegistration(allShips);

            foreach (var currentShip in _shipRegistrator.ToArray())
            {
                if(currentShip.State == ShipRegistrator.ShipState.Destroyed)
                    HandleDestroyedShip(currentShip.Captain);
            }

            var commands = new List<Move>();
            foreach (var ship in ships)
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
                var correctedCommand = CorrectCommand(command, commands);
                commands.Add(correctedCommand);
            }
            // CorrectCommands(commands);

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


        private Move CorrectCommand(Move move, List<Move> futureMoves)
        {
            var extended = futureMoves
                .Where(x => x is ThrustMoveExtended)
                .Cast<ThrustMoveExtended>()
                .ToArray();

            var m = move as ThrustMoveExtended;
            if (m != null)
            {
                var collision = false;
                foreach (var otherMove in extended)
                {
                    var distance = m.FuturePosition.GetDistanceTo(otherMove.FuturePosition);
                    if (distance < m.GetShip().GetRadius() + 0.51)
                    {
                        collision = true;
                        break;
                    }
                }
                return collision ? m.Clone(Math.Max(m.GetThrust() - 2, 0)) : m;
            }
            return move;
        }

        //private void CorrectCommands(List<Move> futureMoves)
        //{
        //    var extended = futureMoves
        //        .Where(x => x is ThrustMoveExtended)
        //        .Cast<ThrustMoveExtended>()
        //        .ToArray();

        //    foreach (var move in extended)
        //    {
        //        foreach (var otherMove in extended)
        //        {
        //            if(otherMove == move)
        //                continue;

        //            var distance = move.FuturePosition.GetDistanceTo(otherMove.FuturePosition);
        //            if (distance < move.GetShip().GetRadius() + 0.51)
        //            {
        //                futureMoves.Remove(move);
        //                futureMoves.Add(move.Clone(Math.Max(move.GetThrust() - 2, 0)));
        //            }
        //        }
        //    }
        //}

        private void LogState(int round)
        {
            var currentMissions = _shipRegistrator.GroupBy(x => x.Captain.CurrentMission.GetType()).Select(x => $"{x.Key.Name}: {x.Count()}");
            var log = string.Join(", ", currentMissions);
            DebugLog.AddLog(round, $"[COORDIN] {log}");
        }
    }
}
