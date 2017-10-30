using System;
using BotMarfu.core.Moves;
using Halite2.hlt;

namespace BotMarfu.core.Missions
{
    class MissionVoid : IMission
    {
        public bool CanExecute(GameMap map, Ship ship)
        {
            return false;
        }

        public Move Execute(GameMap map, Ship ship)
        {
            return NullMove.Null;
        }

        public static readonly IMission Null = new MissionVoid();
    }
}
