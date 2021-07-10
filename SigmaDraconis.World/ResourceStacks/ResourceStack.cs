namespace SigmaDraconis.World.ResourceStacks
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class ResourceStack : Thing, IResourceStack
    {
        private int itemCount;

        public ItemType ItemType { get; protected set; }

        [ProtoMember(1)]
        public int ItemCount
        {
            get
            {
                return this.itemCount;
            }
            set
            {
                if (this.itemCount != value)
                {
                    if (this.mainTile != null) EventManager.EnqueueWorldPropertyChangeEvent(this.Id, nameof(this.ItemCount), this.itemCount, value, this.mainTile.Row, this.ThingType);
                    var raiseEventForZoneManager = (this.itemCount == 0) != (value == 0);   // Because empty stacks allow walking
                    this.itemCount = value;
                    if (raiseEventForZoneManager) EventManager.RaiseEvent(EventType.ResourceStack, EventSubType.Updated, this);
                }
            }
        }

        [ProtoMember(2)]
        protected Dictionary<int, int> colonistsByAccessTile;

        [ProtoMember(3)]
        public bool IsReady { get; set; }

        [ProtoMember(5)]
        public WorkPriority HaulPriority { get; set; }

        [ProtoMember(6)]
        public int TargetItemCount { get; set; }

        [ProtoMember(7)]
        private int itemsAddedByColonists;

        public bool RequiresAccessNow => this.IsReady && this.HaulPriority > 0;

        public int MaxItems => Constants.ResourceStackMaxSizes[this.ItemType];

        public int PredictedItemCount { get; set; }

        public override TileBlockModel TileBlockModel => this.ItemCount > 0 ? this.Definition.TileBlockModel : TileBlockModel.None;

        public override bool CanWalk => this.ItemCount == 0;

        protected ResourceStack() : base(ThingType.None)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
        }
       
        public ResourceStack(ThingType thingType, ISmallTile mainTile, int itemCount) : base(thingType, mainTile, 1)
        {
            this.ItemType = Constants.ItemTypesByResourceStackType[thingType];
            this.itemCount = itemCount;
            this.colonistsByAccessTile = new Dictionary<int, int>();
            this.HaulPriority = WorkPriority.Disabled;  // Set to Normal on completion
        }

        public override void AfterDeserialization()
        {
            this.ItemType = Constants.ItemTypesByResourceStackType[this.ThingType];
            base.AfterDeserialization();
        }

        public void AddItem()
        {
            this.ItemCount++;
            this.itemsAddedByColonists++;
        }

        public void TakeItem()
        {
            this.ItemCount--;
            if (this.itemsAddedByColonists > 0) this.itemsAddedByColonists--;
            else
            {
                switch(this.ItemType)
                {
                    case ItemType.Biomass: WorldStats.Increment(WorldStatKeys.OrganicsCollected); break;
                    case ItemType.Coal: WorldStats.Increment(WorldStatKeys.CoalCollected); break;
                    case ItemType.IronOre: WorldStats.Increment(WorldStatKeys.OreCollected); break;
                    case ItemType.Stone: WorldStats.Increment(WorldStatKeys.StoneCollected); break;
                }
            }
        }

        public override void Update()
        {
            if (!this.mainTile.ThingsPrimary.OfType<IStackingArea>().Any())
            {
                this.TargetItemCount = 0;
                if (this.ItemCount == 0)
                {
                    foreach (var blueprint in World.ConfirmedBlueprints.Where(b => b.Value.MainTile == this.MainTile && b.Value.ThingType == this.ThingType))
                    {
                        World.ConfirmedBlueprints.Remove(blueprint.Key);
                        EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Removed, blueprint.Value);
                        break;
                    }

                    World.RemoveThing(this);
                }
            }

            base.Update();
        }

        public override string GetTextureName(int layer = 1)
        {
            var frame = this.itemCount;
            return $"{this.ThingTypeStr}_{frame}";
        }

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            var done = new HashSet<int>();
            for (int i = 0; i <= 7; i++)
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanPickupFromTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                if (direction == Direction.N && (this.MainTile.HasWallToDirection(Direction.NW) || this.MainTile.HasWallToDirection(Direction.NE))) continue;
                if (direction == Direction.E && (this.MainTile.HasWallToDirection(Direction.NE) || this.MainTile.HasWallToDirection(Direction.SE))) continue;
                if (direction == Direction.S && (this.MainTile.HasWallToDirection(Direction.SE) || this.MainTile.HasWallToDirection(Direction.SW))) continue;
                if (direction == Direction.W && (this.MainTile.HasWallToDirection(Direction.SW) || this.MainTile.HasWallToDirection(Direction.NW))) continue;
                done.Add(tile.Index);
                yield return tile;
            }
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            if (!this.IsReady || this.HaulPriority == 0) yield break;

            this.CleanupColonistAssignments();

            for (int i = 0; i <= 7; i++)
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanPickupFromTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                if (direction == Direction.N && (this.MainTile.HasWallToDirection(Direction.NW) || this.MainTile.HasWallToDirection(Direction.NE))) continue;
                if (direction == Direction.E && (this.MainTile.HasWallToDirection(Direction.NE) || this.MainTile.HasWallToDirection(Direction.SE))) continue;
                if (direction == Direction.S && (this.MainTile.HasWallToDirection(Direction.SE) || this.MainTile.HasWallToDirection(Direction.SW))) continue;
                if (direction == Direction.W && (this.MainTile.HasWallToDirection(Direction.SW) || this.MainTile.HasWallToDirection(Direction.NW))) continue;
                if (tile.ThingsPrimary.Any(t => t is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && !c.IsRelaxing)) continue;   // Blocked by another colonist
                if (colonistId.HasValue && this.colonistsByAccessTile.ContainsKey(tile.Index) && this.colonistsByAccessTile[tile.Index] != colonistId) continue;  // Assigned to someone else
                yield return tile;
            }
        }

        public bool CanAssignColonist(int colonistId, int? tileIndex = null)
        {
            this.CleanupColonistAssignments();
            if (!tileIndex.HasValue && this.colonistsByAccessTile.ContainsValue(colonistId)) return true;

            return tileIndex.HasValue
                ? this.GetAccessTiles(colonistId).Any(t => t.Index == tileIndex.Value)
                : this.GetAccessTiles(colonistId).Any();
        }

        public void AssignColonist(int colonistId, int tileIndex)
        {
            if (!this.colonistsByAccessTile.ContainsKey(tileIndex)) this.colonistsByAccessTile.Add(tileIndex, colonistId);
            else this.colonistsByAccessTile[tileIndex] = colonistId;
        }

        private void CleanupColonistAssignments()
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.TargetBuilingID == this.Id && !c.IsActivityFinished) continue;
                this.colonistsByAccessTile.Remove(id);
            }
        }

        public override string ToString()
        {
            return $"{this.ItemType} stack {this.Id}";
        }
    }
}
