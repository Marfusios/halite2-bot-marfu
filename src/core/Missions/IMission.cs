using Halite2.hlt;

namespace BotMarfu.core.Missions
{
    interface IMission
    {
        bool CanExecute(GameMap map, Ship ship);
        Move Execute(GameMap map, Ship ship);
    }
}
