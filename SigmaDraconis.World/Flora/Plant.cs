namespace SigmaDraconis.World.Flora
{
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;
    using ProtoBuf;

    [ProtoContract]
    [ProtoInclude(101, typeof(FruitPlant))]
    [ProtoInclude(102, typeof(Swordleaf))]
    [ProtoInclude(103, typeof(Tree))]
    [ProtoInclude(104, typeof(SmallPlant1))]
    [ProtoInclude(105, typeof(SmallPlant2))]
    [ProtoInclude(106, typeof(SmallPlant3))]
    [ProtoInclude(107, typeof(SmallPlant4))]
    [ProtoInclude(108, typeof(SmallPlant7))]
    [ProtoInclude(109, typeof(SmallPlant8))]
    [ProtoInclude(110, typeof(CoastGrass))]
    [ProtoInclude(111, typeof(SmallPlant10))]
    [ProtoInclude(112, typeof(SmallPlant11))]
    [ProtoInclude(114, typeof(SmallPlant13))]
    [ProtoInclude(115, typeof(BigSpineBush))]
    [ProtoInclude(116, typeof(SmallSpineBush))]
    public abstract class Plant : Thing, IPlant
    {
        protected bool isTexturePositionInvalidated = true;
        protected float age = 1f;
        protected int animationFrame = 1;

        public string CanRecycleReason { get; protected set; }
        public bool RequiresAccessNow => this.HasColonistJob();

        public Plant(ThingType thingType) : base(thingType)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public Plant() : base(ThingType.None)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public Plant(ThingType thingType, ISmallTile mainTile, int size) : base(thingType, mainTile, size)
        {
            this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        [ProtoMember(1)]
        public float Age
        {
            get
            {
                return this.age;
            }

            set
            {
                this.age = value;
                this.isTexturePositionInvalidated = true;
            }
        }

        public Vector2i TextureSize { get; set; }

        public Vector2i RenderSize { get; set; }

        public Vector2f RenderScale { get; set; } = new Vector2f(1, 1);

        [ProtoMember(2)]
        public bool? ImageFlip { get; set; }

        [ProtoMember(3)]
        public double GrowthPercent { get; set; }

        // Used by FlyingInsectController to indicate that a bee is on this plant or flying towards it.
        [ProtoMember(4)]
        public int BeeID { get; set; }

        [ProtoMember(7)]
        public int AnimationFrame
        {
            get
            {
                return this.animationFrame;
            }
            set
            {
                if (this.animationFrame != value)
                {
                    if (this.mainTile != null) EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.AnimationFrame), this.animationFrame, value, this.mainTile.Row, this.ThingType);
                    this.animationFrame = value;
                }
            }
        }

        [ProtoMember(8)]
        public long NextGrowthUpdateFrame { get; set; }

        [ProtoMember(9)]
        public Vector2i RenderPositionOffset { get; set; }

        [ProtoMember(10)]
        protected Dictionary<int, int> colonistsByAccessTile;

        public virtual int GrowthStage => 0;
        public virtual int MaxGrowthStage => 0;
        public virtual bool CanFlower => false;
        public virtual bool IsFlowering => false;
        public virtual bool HasDeadFrame => false;
        public virtual bool IsDead => false;

        public virtual int RecycleTime => this.GetDeconstructionYield()[ItemType.Biomass] * 30;
        public virtual List<int> UpdateGrowth()
        {
            this.age = (int)this.age + 1;
            return null;
        }

        public override string GetTextureName(int layer = 1)
        {
            return $"{this.ThingType.ToString()}_{this.AnimationFrame}";
        }

        public virtual bool CanRecycle()
        {
            return !this.IsRecycling && this.definition?.CanRecycle == true;
        }

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            foreach (var t in this.allTiles)
            {
                for (int i = 4; i <= 7; i++)   // NE, SE, SW, NW
                {
                    var direction = (Direction)i;
                    var tile = t.GetTileToDirection(direction);
                    if (tile == null || !tile.CanWorkInTile || t.HasWallToDirection(direction)) continue;   // Can't work here
                    //if (direction == Direction.N && (this.MainTile.HasWallToDirection(Direction.NW) && this.MainTile.HasWallToDirection(Direction.NE))) continue;
                    //if (direction == Direction.E && (this.MainTile.HasWallToDirection(Direction.NE) && this.MainTile.HasWallToDirection(Direction.SE))) continue;
                    //if (direction == Direction.S && (this.MainTile.HasWallToDirection(Direction.SE) && this.MainTile.HasWallToDirection(Direction.SW))) continue;
                    //if (direction == Direction.W && (this.MainTile.HasWallToDirection(Direction.SW) && this.MainTile.HasWallToDirection(Direction.NW))) continue;
                    yield return tile;
                }

                for (int i = 0; i <= 3; i++)   // E, W, S, N
                {
                    var direction = (Direction)i;
                    var tile = t.GetTileToDirection(direction);
                    if (tile == null || !tile.CanWorkInTile || t.HasWallToDirection(direction)) continue;   // Can't work here
                    //if (direction == Direction.N && (this.MainTile.HasWallToDirection(Direction.NW) && this.MainTile.HasWallToDirection(Direction.NE))) continue;
                    //if (direction == Direction.E && (this.MainTile.HasWallToDirection(Direction.NE) && this.MainTile.HasWallToDirection(Direction.SE))) continue;
                    //if (direction == Direction.S && (this.MainTile.HasWallToDirection(Direction.SE) && this.MainTile.HasWallToDirection(Direction.SW))) continue;
                    //if (direction == Direction.W && (this.MainTile.HasWallToDirection(Direction.SW) && this.MainTile.HasWallToDirection(Direction.NW))) continue;
                    yield return tile;
                }
            }
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            this.CleanupColonistAssignments();
            if (colonistId.HasValue && this.colonistsByAccessTile.Values.Any(v => v != colonistId.Value)) yield break;  // Assigned to someone else

            foreach (var tile in this.GetAllAccessTiles())
            {
                if (tile.ThingsPrimary.Any(t => t is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && !c.IsRelaxing)) continue;   // Blocked by another colonist
                yield return tile;
            }
        }

        public bool CanAssignColonist(int colonistId, int? tileIndex = null)
        {
            this.CleanupColonistAssignments();
            if (this.colonistsByAccessTile.Any(c => c.Value == colonistId && (!tileIndex.HasValue || c.Key == tileIndex.Value))) return true;
            if (this.colonistsByAccessTile.Any(c => c.Value != colonistId)) return false;

            return tileIndex.HasValue
                ? this.GetAccessTiles(colonistId).Any(t => t.Index == tileIndex.Value)
                : this.GetAccessTiles(colonistId).Any();
        }

        public void AssignColonist(int colonistId, int tileIndex)
        {
            if (!this.colonistsByAccessTile.ContainsKey(tileIndex)) this.colonistsByAccessTile.Add(tileIndex, colonistId);
            else this.colonistsByAccessTile[tileIndex] = colonistId;
        }

        protected virtual bool HasColonistJob()
        {
            return this.RecyclePriority != WorkPriority.Disabled;
        }

        private void CleanupColonistAssignments()
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c 
                    && (c.ActivityType == ColonistActivityType.Deconstruct || c.ActivityType == ColonistActivityType.Harvest)
                    && !c.IsDead) continue;
                this.colonistsByAccessTile.Remove(id);
            }
        }
    }
}
