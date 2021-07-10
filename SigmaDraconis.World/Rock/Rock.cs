namespace SigmaDraconis.World.Rocks
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class Rock : Thing, IRotatableThing, IRock
    {
        [ProtoMember(1)]
        public int AnimationFrame { get; private set; }

        [ProtoMember(2)]
        public Direction Direction { get; private set; }

        [ProtoMember(3)]
        protected Dictionary<int, int> colonistsByAccessTile;

        [ProtoMember(4)]
        public ItemType ResourceType { get; private set; }

        public virtual int RecycleTime => this.ThingType == ThingType.RockLarge ? 600 : 150;
        public string CanRecycleReason { get; protected set; }
        public bool RequiresAccessNow => this.RecyclePriority != WorkPriority.Disabled;

        public Rock() : base(ThingType.None)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public Rock(ISmallTile tile, ThingType thingType, ItemType resourceType) : base(thingType, tile, thingType == ThingType.RockLarge ? 2 : 1)
        {
            this.ResourceType = resourceType;
            this.Direction = (Direction)Rand.Next(4) + 4;
            this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public override Dictionary<ItemType, int> GetDeconstructionYield()
        {
            var count = this.ThingType == ThingType.RockLarge ? 10 : 2;
            var result = new Dictionary<ItemType, int>
            {
                { ItemType.Biomass, 0 },
                { ItemType.Metal, 0 },
                { ItemType.IronOre, this.ResourceType == ItemType.IronOre ? count : 0 },
                { ItemType.Coal, this.ResourceType == ItemType.Coal ? count : 0 },
                { ItemType.Stone, this.ResourceType == ItemType.Stone ? count : 0 }
            };

            return result;
        }
        
        public override string GetTextureName(int layer = 1)
        {
            return $"{this.ThingTypeStr}_{this.ResourceType.ToString()}_{this.Direction.ToString()}";
        }

        public override string ToString()
        {
            return $"{this.ShortName} {this.Id}";
        }

        public virtual bool CanRecycle()
        {
            return !this.IsRecycling && this.definition?.CanRecycle == true;
        }

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            var done = new HashSet<int>();
            foreach (var t in this.AllTiles)
            {
                for (int i = 0; i <= 7; i++)
                {
                    var direction = (Direction)i;
                    var tile = t.GetTileToDirection(direction);
                    if (tile == null || done.Contains(t.Index) || !tile.CanPickupFromTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                    done.Add(tile.Index);
                    yield return tile;
                }
            }
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            this.CleanupColonistAssignments();
            if (colonistId.HasValue && this.colonistsByAccessTile.Any(c => c.Value != colonistId)) yield break;
            else if (!colonistId.HasValue && this.colonistsByAccessTile.Any()) yield break;

            var done = new HashSet<int>();
            foreach (var tile in this.AllTiles)
            {
                for (int i = 0; i <= 7; i++)
                {
                    var direction = (Direction)i;
                    var t = tile.GetTileToDirection(direction);
                    if (t == null || done.Contains(t.Index) || this.allTiles.Contains(t)) continue;
                    if (tile.ThingsPrimary.Any(a => a is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && !c.IsRelaxing)) continue;   // Blocked by another colonist
                    if (!t.CanPickupFromTile || tile.HasWallToDirection(direction)) continue;   // Can't work here
                    done.Add(t.Index);
                    yield return t;
                }
            }
        }

        public bool CanAssignColonist(int colonistId, int? tileIndex = null)
        {
            this.CleanupColonistAssignments();
            if (this.colonistsByAccessTile.ContainsValue(colonistId)) return true;
            if (this.colonistsByAccessTile.Any()) return false;

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
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.ActivityType == ColonistActivityType.Deconstruct && !c.IsDead) continue;
                this.colonistsByAccessTile.Remove(id);
            }
        }
    }
}
