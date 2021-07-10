namespace SigmaDraconis.AI
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
    [ProtoInclude(101, typeof(ActivityDrink))]
    [ProtoInclude(102, typeof(ActivityEat))]
    [ProtoInclude(103, typeof(ActivityFarmHarvest))]
    [ProtoInclude(104, typeof(ActivityResearch))]
    [ProtoInclude(105, typeof(ActivityRoam))]
    [ProtoInclude(106, typeof(ActivitySleep))]
    [ProtoInclude(107, typeof(ActivityCook))]
    [ProtoInclude(108, typeof(ActivityHaulFromNetwork))]
    [ProtoInclude(109, typeof(ActivityDeconstruct))]
    [ProtoInclude(110, typeof(ActivityLeaveClosedTile))]
    [ProtoInclude(111, typeof(ActivityLeaveLandingPod))]
    [ProtoInclude(112, typeof(ActivityWait))] 
    [ProtoInclude(113, typeof(ActivityRelax))]
    [ProtoInclude(114, typeof(ActivitySeekSafeTemperature))]
    [ProtoInclude(115, typeof(ActivityRest))]
    [ProtoInclude(117, typeof(ActivityIdleWalk))]
    [ProtoInclude(118, typeof(ActivityConstruct))]
    [ProtoInclude(119, typeof(ActivityRepair))]
    [ProtoInclude(120, typeof(ActivityHaulFromStack))]
    [ProtoInclude(121, typeof(ActivityHaulToNetwork))]
    [ProtoInclude(122, typeof(ActivityHaulToStack))]
    [ProtoInclude(123, typeof(ActivityHarvestFruit))]
    [ProtoInclude(124, typeof(ActivityGeology))]
    [ProtoInclude(125, typeof(ActivityFarmPlant))]
    [ProtoInclude(126, typeof(ActivityPickupKek))]
    [ProtoInclude(127, typeof(ActivityDrinkKek))]
    public abstract class ActivityBase
    {
        private bool isFinished;

        public IColonist Colonist { get; private set; }

        [ProtoMember(1)]
        protected int colonistId;

        [ProtoMember(2)]
        public ActionBase CurrentAction { get; protected set; }

        [ProtoMember(3)]
        public bool IsFinished
        {
            get { return this.isFinished; }
            protected set { this.isFinished = value; if (this.Colonist != null) this.Colonist.IsActivityFinished = value; }
        }

        // Deserialisation ctor
        protected ActivityBase() { }

        public ActivityBase(IColonist colonist, int? buildingId = null, ItemType itemType = ItemType.None)
        {
            this.Colonist = colonist;
            this.colonistId = colonist.Id;
            this.Colonist.TargetBuilingID = buildingId;
            this.Colonist.TargetItemType = itemType;
            if (this.Colonist != null) this.Colonist.IsActivityFinished = false;
        }

        [ProtoAfterDeserialization]
        public virtual void AfterDeserialization()
        {
            this.Colonist = World.GetThing(this.colonistId) as IColonist;
        }

        public virtual void Update()
        {
        }

        protected void BuildWalkActionForFacingTarget(Direction direction, Vector2f offset = null, float endOffsetFlexibility = 0.01f)
        {
            var node = new PathNode(this.Colonist.MainTile.X, this.Colonist.MainTile.Y, Direction.None, Direction.None);
            var nodeStack = new Stack<PathNode>();
            nodeStack.Push(node);
            nodeStack.Push(node);
            var path = new Path(this.Colonist.MainTile.TerrainPosition, this.Colonist.MainTile.TerrainPosition, nodeStack);
            this.CurrentAction = new ActionWalk(this.Colonist, path, offset ?? Vector2f.Zero, direction, endOffsetFlexibility);
        }

        protected void ResetMovement()
        {
            this.Colonist.IsMoving = false;
            var tile = World.GetSmallTile((int)(this.Colonist.Position.X + 0.5f), (int)(this.Colonist.Position.Y + 0.5f));
            if (tile != this.Colonist.MainTile)
            {
                this.Colonist.SetPosition(tile);
                var offset1 = this.Colonist.Position - tile.TerrainPosition;
                this.Colonist.PositionOffset.X = (offset1.X + offset1.Y) * 10.66667f;
                this.Colonist.PositionOffset.Y = (offset1.Y - offset1.X) * 5.33333f;
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
