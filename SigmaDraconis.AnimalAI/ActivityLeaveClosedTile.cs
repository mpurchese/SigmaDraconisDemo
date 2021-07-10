namespace SigmaDraconis.AnimalAI
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World.Zones;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    public class ActivityLeaveClosedTile : ActivityBase
    {
        // Deserialisation ctor
        protected ActivityLeaveClosedTile() { }

        public ActivityLeaveClosedTile(IAnimal animal) : base(animal)
        {
            ISmallTile closest = null;
            var minDistance = 999f;
            foreach (var tile in animal.MainTile.AdjacentTiles8.Where(t => ZoneManager.AnimalZone.ContainsNode(t.Index) && t.ThingsAll.All(u => u.TileBlockModel.In(TileBlockModel.None, TileBlockModel.Point) == true)))
            {
                var distance = (tile.TerrainPosition - animal.Position).Length();
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

            var direction = DirectionHelper.GetDirectionFromAdjacentPositions(animal.MainTile.X, animal.MainTile.Y, closest.X, closest.Y);
            var node1 = new PathNode(this.Animal.MainTile.X, this.Animal.MainTile.Y, direction, DirectionHelper.Reverse(direction));
            var node2 = new PathNode(closest.X, closest.Y, Direction.None, Direction.None);
            var nodeStack = new Stack<PathNode>();
            nodeStack.Push(node2);
            nodeStack.Push(node1);
            var path = new Path(this.Animal.MainTile.TerrainPosition, closest.TerrainPosition, nodeStack);
            this.CurrentAction = new ActionWalk(this.Animal, path, Vector2f.Zero, direction);
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
