using System;
using System.Collections.Generic;
using BotMarfu.core.Missions;
using BotMarfu.core.Ships;
using Halite2.hlt;

namespace BotMarfu.core.Headquarter
{
    class Coordinator
    {
        private readonly Random _random = new Random();
        private readonly GameMap _map;
        private readonly General _general;
        private readonly ShipRegistrator _shipRegistrator;

        private int _createdShips = 0;

        public Coordinator(GameMap map, General general)
        {
            Validations.ValidateInput(map, nameof(GameMap));
            Validations.ValidateInput(general, nameof(General));

            _general = general;
            _map = map;
            _shipRegistrator = new ShipRegistrator();
        }

        public Move[] DoCommands()
        {
            var player = _map.GetMyPlayer();
            var ships = player.GetShips();

            _shipRegistrator.UpdateRegistration(ships);

            var commands = new List<Move>();
            foreach (var shipRegistration in _shipRegistrator)
            {
                var state = shipRegistration.State;
                var captain = shipRegistration.Captain;
                switch (state)
                {
                    case ShipRegistrator.ShipState.New:
                        _createdShips++;
                        captain.AssignMission(GenerateMissionForNewShip());
                        break;
                    case ShipRegistrator.ShipState.Destroyed:
                        captain.AssignMission(MissionVoid.Null);
                        HandleDestroyedShip(captain);
                        break;
                }
                commands.Add(captain.ExecuteCommand(this));
            }

            return commands.ToArray();
        }

        public IMission GiveMeNewMission(ShipCaptain shipCaptain)
        {
            return GenerateMissionForNewShip();
        }

        private IMission GenerateMissionForNewShip()
        {
            if(_createdShips <= _general.InitialSettlersCount)
                return new SettlerMission();

            var random = _random.NextDouble();
            if(random <= _general.SettlerRatio)
                return new SettlerMission();
            if (random <= _general.AttackerRatio)
                return new AttackerMission();
            if (random <= _general.DefenderRatio)
                return new DefenderMission();

            return MissionVoid.Null;
        }

        private void HandleDestroyedShip(ShipCaptain captain)
        {
            throw new System.NotImplementedException();
        }
    }
}
