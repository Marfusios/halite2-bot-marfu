using System.Collections.Generic;
using System.Linq;
using Halite2.hlt;

namespace BotMarfu.core.Headquarter
{
    class General
    {
        private bool _twoPlayers;

        public double SettlerRatio { get; private set; }
        public double AttackerRatio { get; private set; }
        public double DefenderRatio { get; private set; }

        public int InitialSettlersCount { get; private set; } = 3;
        public int DefenderMaxRounds { get; } = 10;
        public int NearestPlanetCount { get; private set; } = 2;
        public double NearestPlanetMaxDistanceRatio { get; private set; } = 1.8;
        public int EnemyCheckRadius { get; private set; } = 3;
        public int EnemyCheckRadiusForNew { get; private set; } = 5;
        public int KillersPerEnemyShip { get; private set; } = 2;

        public void AdjustInitialStrategy(GameMap map)
        {
            _twoPlayers = map.GetAllPlayers().Count <= 2;

            if (_twoPlayers)
            {
                InitialSettlersCount = 3;
                NearestPlanetCount = 2;
                SettlerRatio = 0.6;
                AttackerRatio = 1;
                DefenderRatio = 1;

                return;
            }

            InitialSettlersCount = 10;
            NearestPlanetCount = 2;
            SettlerRatio = 0.9;
            AttackerRatio = 1;
            DefenderRatio = 1;
        }

        public void AdjustGlobalStrategy(GameMap map, int round)
        {
            if(round % 5 != 0)
                return;
            if (round < 50)
                return;

            var winningRatio = ComputeWinningRationPerPlayer(map);
            if (winningRatio.Count() <= 1)
                return;

            if(_twoPlayers)
                AdjustGlobalStrategy2Players(map, winningRatio, round);
            else
                AdjustGlobalStrategy4Players(map, winningRatio, round);
        }

        private static Dictionary<int, int> ComputeWinningRationPerPlayer(GameMap map)
        {
            return map
                .GetAllPlayers()
                .ToDictionary(
                    x => x.GetId(),
                    y => y.GetShips().Count + map.GetAllPlanets().Count(x => x.Value.GetOwner() == y.GetId()) * 10
                )
                .OrderByDescending(x => x.Value)
                .ToDictionary(x => x.Key, y => y.Value);
        }

        private void AdjustGlobalStrategy2Players(GameMap map, Dictionary<int, int> winningRatio, int round)
        {
            var first = winningRatio.First();
            //var second = winningRatio.ElementAt(1);
            //var me = winningRatio[map.GetMyPlayerId()];

            if (first.Key == map.GetMyPlayerId())
            {
                //DebugLog.AddLog(round, $"[GENERAL] 2 - adjusting strategy for winning. Me: {me}, Second: {second.Value}");
                SettlerRatio = 0.4;
                AttackerRatio = 1;
            }
            else
            {
                //DebugLog.AddLog(round, $"[GENERAL] 2 - adjusting strategy for loosing. First: {first.Value}, Me: {me}");
                SettlerRatio = 0.7;
                AttackerRatio = 1;
            }
        }

        private void AdjustGlobalStrategy4Players(GameMap map, Dictionary<int, int> winningRatio, int round)
        {
            var first = winningRatio.First();
            //var second = winningRatio.ElementAt(1);
            //var me = winningRatio[map.GetMyPlayerId()];

            if (first.Key == map.GetMyPlayerId())
            {
                //DebugLog.AddLog(round, $"[GENERAL] 4 - adjusting strategy for winning. Me: {me}, Second: {second.Value}");
                SettlerRatio = 0.6;
                AttackerRatio = 1;
            }
            else
            {
                //DebugLog.AddLog(round, $"[GENERAL] 4 - adjusting strategy for loosing. First: {first.Value}, Me: {me}");
                SettlerRatio = 0.8;
                AttackerRatio = 1;
            }
        }
    }
}
