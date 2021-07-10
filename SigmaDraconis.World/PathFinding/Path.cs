namespace SigmaDraconis.World.PathFinding
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;

    [ProtoContract]
    public class Path
    {
        // Parameterless private constructor for deserialization
        private Path() { }

        public Path(Vector2i start, Vector2i end, Stack<PathNode> nodes)
        {
            this.StartPosition = start;
            this.EndPosition = end;

            this.RemainingNodes = new Queue<PathNode>();
            foreach (var node in nodes)
            {
                this.RemainingNodes.Enqueue(node);
            }
        }

        [ProtoMember(1)]
        public Vector2i StartPosition { get; set; }

        [ProtoMember(2)]
        public Vector2i EndPosition { get; set; }

        [ProtoMember(3)]
        public PathNode CurrentNode { get; set; }

        [ProtoMember(4)]
        private List<PathNode> remainingNodesAsList;

        public Queue<PathNode> RemainingNodes { get; set; }

        [ProtoBeforeSerialization]
        private void BeforeSerialize()
        {
            this.remainingNodesAsList = this.RemainingNodes.ToList();
        }

        [ProtoAfterDeserialization]
        private void AfterDeserialize()
        {
            if (this.RemainingNodes == null) this.RemainingNodes = new Queue<PathNode>();
            else this.RemainingNodes.Clear();

            if (this.remainingNodesAsList != null)
            {
                foreach (var node in this.remainingNodesAsList)
                {
                    this.RemainingNodes.Enqueue(node);
                }
            }
        }
    }
}
