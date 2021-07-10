namespace SigmaDraconis.AnimalAI
{
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Shared;
    using ProtoBuf;
    using World;
    using World.PathFinding;
    using WorldInterfaces;

    [ProtoContract]
    [ProtoInclude(100, typeof(ActivityWait))]
    [ProtoInclude(101, typeof(ActivityWalk))]
    [ProtoInclude(102, typeof(ActivitySleep))]
    [ProtoInclude(103, typeof(ActivityEat))]
    [ProtoInclude(104, typeof(ActivityLeaveClosedTile))]
    public abstract class ActivityBase
    {
        public IAnimal Animal { get; private set; }

        [ProtoMember(1)]
        protected int animalId;

        [ProtoMember(2)]
        public ActionBase CurrentAction { get; protected set; }

        [ProtoMember(3)]
        public bool IsFinished { get; protected set; }

        // Deserialisation ctor
        protected ActivityBase() { }

        public ActivityBase(IAnimal animal)
        {
            this.Animal = animal;
            this.animalId = animal.Id;
        }

        [ProtoAfterDeserialization]
        public virtual void AfterDeserialization()
        {
            this.Animal = World.GetThing(this.animalId) as IAnimal;
        }

        public virtual void Update()
        {
        }

        protected void BuildWalkActionForFacingTarget(Direction direction, Vector2f offset = null)
        {
            var node = new PathNode(this.Animal.MainTile.X, this.Animal.MainTile.Y, Direction.None, Direction.None);
            var nodeStack = new Stack<PathNode>();
            nodeStack.Push(node);
            nodeStack.Push(node);
            var path = new Path(this.Animal.MainTile.TerrainPosition, this.Animal.MainTile.TerrainPosition, nodeStack);
            this.CurrentAction = new ActionWalk(this.Animal, path, offset ?? Vector2f.Zero, direction);
        }

        protected void ResetMovement()
        {
            this.Animal.IsMoving = false;
            var tile = World.GetSmallTile((int)(this.Animal.Position.X + 0.5f), (int)(this.Animal.Position.Y + 0.5f));
            if (tile != this.Animal.MainTile)
            {
                this.Animal.SetPosition(tile);
                var offset1 = this.Animal.Position - tile.TerrainPosition;
                this.Animal.PositionOffset.X = (offset1.X + offset1.Y) * 10.66667f;
                this.Animal.PositionOffset.Y = (offset1.Y - offset1.X) * 5.33333f;
            }
        }

        protected static Vector2f FindPositionOffsetAvoidingPointBlocker(ISmallTile endTile)
        {
            var endOffset = Vector2f.Zero;
            var endTilePointBlocker = endTile.ThingsPrimary.FirstOrDefault(t => t.Definition.TileBlockModel == TileBlockModel.Point);
            if (endTilePointBlocker is IPositionOffsettable po && po.PositionOffset.Length() > 0.05f)
            {
                if (po.PositionOffset.X < -Mathf.Abs(po.PositionOffset.Y)) endOffset = new Vector2f(0.25f, 0f);
                else if (po.PositionOffset.X > Mathf.Abs(po.PositionOffset.Y)) endOffset = new Vector2f(-0.25f, 0f);
                else if (po.PositionOffset.Y < 0) endOffset = new Vector2f(0f, 0.25f);
                else endOffset = new Vector2f(0f, -0.25f);
            }
            else
            {
                var r = Rand.Next(4);
                switch (r)
                {
                    case 0: endOffset = new Vector2f(0.25f, 0f); break;
                    case 1: endOffset = new Vector2f(-0.25f, 0f); break;
                    case 2: endOffset = new Vector2f(0f, 0.25f); break;
                    case 3: endOffset = new Vector2f(0f, -0.25f); break;
                }
            }

            return endOffset;
        }
    }
}
