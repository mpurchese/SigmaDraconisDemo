namespace SigmaDraconis.World.PathFinding
{
    using ProtoBuf;
    using Shared;

    [ProtoContract]
    public class PathNode
    {
        // Parameterless private constructor for deserialization
        private PathNode() { }

        public PathNode(int x, int y, Direction forwardDirection, Direction reverseDirection)
        {
            this.X = x;
            this.Y = y;
            this.ForwardDirection = forwardDirection;
            this.ReverseDirection = reverseDirection;
        }

        [ProtoMember(1)]
        public int X { get; set; }

        [ProtoMember(2)]
        public int Y { get; set; }

        [ProtoMember(3)]
        public Direction ForwardDirection { get; set; }

        [ProtoMember(4)]
        public Direction ReverseDirection;
    }
}
