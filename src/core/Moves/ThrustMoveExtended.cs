using Halite2.hlt;

namespace BotMarfu.core.Moves
{
    public class ThrustMoveExtended : ThrustMove
    {
        public ThrustMoveExtended(ThrustMove move, Position futurePosition, double angleRadians) : base(move.GetShip(), move.GetAngle(), move.GetThrust())
        {
            Validations.ValidateInput(move, nameof(move));
            Validations.ValidateInput(futurePosition, nameof(futurePosition));

            FuturePosition = futurePosition;
            AngleRadians = angleRadians;
        }

        public double AngleRadians { get; }
        public Position FuturePosition { get; }

        public ThrustMoveExtended Clone(int newThrust)
        {
            return new ThrustMoveExtended(
                new ThrustMove(GetShip(), GetAngle(), newThrust),
                NavigationExtended.ComputeNewPosition(GetShip(), newThrust, AngleRadians),
                AngleRadians
            );
        }

        public ThrustMoveExtended Clone(int newThrust, int newAngle)
        {
            var newAngleRad = Util.AngleDegToRadClipped(newAngle);
            return new ThrustMoveExtended(
                new ThrustMove(GetShip(), newAngle, newThrust),
                NavigationExtended.ComputeNewPosition(GetShip(), newThrust, newAngleRad),
                newAngleRad
                );
        }
    }
}
