using System;
using System.Linq;
using Halite2.hlt;

namespace BotMarfu.core.Moves
{
    public class NavigationExtended
    {
        public static ThrustMoveExtended NavigateShipToDock(
               GameMap gameMap,
               Ship ship,
               Entity dockTarget,
               int maxThrust)
        {
            int maxCorrections = Constants.MAX_NAVIGATION_CORRECTIONS;
            bool avoidObstacles = true;
            double angularStepRad = Math.PI / 180.0;
            Position targetPos = ship.GetClosestPoint(dockTarget);

            return NavigateShipTowardsTarget(gameMap, ship, targetPos, maxThrust, avoidObstacles, maxCorrections, angularStepRad);
        }

        public static ThrustMoveExtended NavigateShipTowardsTarget(
                GameMap gameMap,
                Ship ship,
                Position targetPos,
                int maxThrust,
                bool avoidObstacles,
                int maxCorrections,
                double angularStepRad)
        {
            if (maxCorrections <= 0)
            {
                return null;
            }

            double distance = ship.GetDistanceTo(targetPos);
            double angleRad = ship.OrientTowardsInRad(targetPos);

            if (avoidObstacles && gameMap.ObjectsBetween(ship, targetPos).Any())
            {
                double newTargetDx = Math.Cos(angleRad + angularStepRad) * distance;
                double newTargetDy = Math.Sin(angleRad + angularStepRad) * distance;
                Position newTarget = new Position(ship.GetXPos() + newTargetDx, ship.GetYPos() + newTargetDy);

                return NavigateShipTowardsTarget(gameMap, ship, newTarget, maxThrust, true, (maxCorrections - 1), angularStepRad);
            }

            int thrust;
            if (distance < maxThrust)
            {
                // Do not round up, since overshooting might cause collision.
                thrust = (int)distance;
            }
            else
            {
                thrust = maxThrust;
            }

            var newPosition = ComputeNewPosition(ship, thrust, angleRad);

            int angleDeg = Util.AngleRadToDegClipped(angleRad);

            var move = new ThrustMove(ship, angleDeg, thrust);
            return new ThrustMoveExtended(move, newPosition, angleRad);
        }

        public static Position ComputeNewPosition(Position current, int thrust, double angleRad)
        {
            var oldX = current.GetXPos();
            var oldY = current.GetYPos();
            var newX = oldX + thrust * Math.Cos(angleRad);
            var newY = oldY + thrust * Math.Sin(angleRad);
            return new Position(newX, newY);
        }
    }
}
