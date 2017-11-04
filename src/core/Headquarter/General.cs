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
        public double AttackerFarRatio { get; private set; } = 1;
        public double DefenderRatio { get; private set; } = 1;

        public int InitialSettlersCount { get; private set; } = 3;
        public int DefenderMaxRounds { get; } = 10;
        public int NearestPlanetCount { get; private set; } = 3;
        public double NearestPlanetMaxDistanceRatio { get; private set; } = 1.7;
        public int EnemyCheckRadius { get; private set; } = 1;
        public int EnemyCheckRadiusForNew { get; private set; } = 3;
        public int KillersPerEnemyShip { get; private set; } = 2;

        public int NearestPlanetToCenterSkipCount(int round) => round < (_twoPlayers ? 50 : 80) ? NearestPlanetCount - 1 : 0;

        public int BootstrapKillerMisionMaxRange { get; private set; }

        public void AdjustInitialStrategy(GameMap map)
        {
            _twoPlayers = map.GetAllPlayers().Count <= 2;

            if (_twoPlayers)
            {
                InitialSettlersCount = 12;
                NearestPlanetCount = 2;
                SettlerRatio = 0.8;
                AttackerRatio = 1;

                BootstrapKillerMisionMaxRange = 10 * Constants.MAX_SPEED;

                return;
            }

            InitialSettlersCount = 16;
            NearestPlanetCount = 3;
            SettlerRatio = 0.9;
            AttackerRatio = 1;

            BootstrapKillerMisionMaxRange = 8 * Constants.MAX_SPEED;
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
                AttackerRatio = 0.7;
                AttackerFarRatio = 1;
            }
            else
            {
                //DebugLog.AddLog(round, $"[GENERAL] 2 - adjusting strategy for loosing. First: {first.Value}, Me: {me}");
                SettlerRatio = 0.7;
                AttackerRatio = 0.85;
                AttackerFarRatio = 1;
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
                AttackerRatio = 0.85;
                AttackerFarRatio = 1;
            }
            else
            {
                //DebugLog.AddLog(round, $"[GENERAL] 4 - adjusting strategy for loosing. First: {first.Value}, Me: {me}");
                SettlerRatio = 0.8;
                AttackerRatio = 1;
                AttackerFarRatio = 1;
            }
        }
    }
}
