using Halite2.hlt;

namespace BotMarfu.core
{
    public class NullMove : Move
    {
        public NullMove() : base(MoveType.Noop, null)
        {
        }

        public static Move Null = new NullMove();
    }
}
