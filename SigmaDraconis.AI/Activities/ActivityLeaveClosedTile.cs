namespace SigmaDraconis.AI
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityLeaveClosedTile : ActivityBase
    {
        // Deserialisation ctor
        protected ActivityLeaveClosedTile() { }

        public ActivityLeaveClosedTile(IColonist colonist) : base(colonist)
        {
            ISmallTile closest = null;
            var minDistance = 999f;
            foreach (var tile in colonist.MainTile.AdjacentTiles8.Where(t => t.CanWalk))
            {
                var distance = (tile.TerrainPosition - colonist.Position).Length();
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = tile;
                }
            }

            if (closest == null)
            {
                this.IsFinished = true;
                return;
            }

            var direction = DirectionHelper.GetDirectionFromAdjacentPositions(colonist.MainTile.X, colonist.MainTile.Y, closest.X, closest.Y);
            var node1 = new PathNode(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, direction, DirectionHelper.Reverse(direction));
            var node2 = new PathNode(closest.X, closest.Y, Direction.None, Direction.None);
            var nodeStack = new Stack<PathNode>();
            nodeStack.Push(node2);
            nodeStack.Push(node1);
            var path = new Path(this.Colonist.MainTile.TerrainPosition, closest.TerrainPosition, nodeStack);
            this.CurrentAction = new ActionWalk(this.Colonist, path, Vector2f.Zero, direction) { IsEscapingBlock = true };
        }

        public override void Update()
        {
            if (this.CurrentAction?.IsFinished != false)
            {
                this.IsFinished = true;
                return;
            }

            this.CurrentAction.Update();
        }
    }
}
