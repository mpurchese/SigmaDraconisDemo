namespace SigmaDraconis.World.Buildings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    [ProtoInclude(1, typeof(FoodDispenser))]
    [ProtoInclude(2, typeof(WaterDispenser))]
    [ProtoInclude(3, typeof(KekDispenser))]
    public class DispenserBase : Building, IDispenser
    {
        private DispenserStatus dispenserStatus;

        [ProtoMember(10)]
        public DispenserStatus DispenserStatus
        {
            get { return this.dispenserStatus; }
            protected set
            {
                if (this.dispenserStatus != value)
                {
                    this.dispenserStatus = value;
                    // Whether the adjacent tiles are reserved as a access corridors depends on current status of the dispenser
                    if (this.MainTile != null)
                    {
                        foreach (var tile in this.MainTile.AdjacentTiles4) tile.UpdateIsCorridor();
                    }
                }
            }
        }

        [ProtoMember(11)]
        public float DispenserProgress { get; set; }

        [ProtoMember(12)]
        public bool IsDispenserSwitchedOn { get; set; }

        [ProtoMember(19)]
        protected Dictionary<int, int> colonistsByAccessTile;

        [ProtoMember(21)]
        private List<Tuple<int, Direction>> colonistQueueAsList;
        private Queue<Tuple<int, Direction>> colonistQueue;

        public bool RequiresAccessNow => this.IsDispenserSwitchedOn;

        public DispenserBase() : base(ThingType.None)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public DispenserBase(ThingType thingType) : base(thingType)
        {
            this.colonistsByAccessTile = new Dictionary<int, int>();
            this.colonistQueue = new Queue<Tuple<int, Direction>>();
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public DispenserBase(ThingType thingType, ISmallTile mainTile) : base(thingType, mainTile, 1)
        {
            this.colonistsByAccessTile = new Dictionary<int, int>();
            this.colonistQueue = new Queue<Tuple<int, Direction>>();
        }

        public override void BeforeSerialization()
        { 
            this.colonistQueueAsList = this.colonistQueue.ToList();
            base.BeforeSerialization();
        }

        public override void AfterConstructionComplete()
        {
            this.DispenserStatus = DispenserStatus.Standby;
            this.DispenserProgress = 0;
            this.IsDispenserSwitchedOn = true;
            base.AfterConstructionComplete();
        }

        public override void AfterAddedToWorld()
        {
            if (this.IsReady)
            {
                if (this.colonistQueue == null) this.colonistQueue = new Queue<Tuple<int, Direction>>();
                else this.colonistQueue.Clear();

                if (this.colonistQueueAsList != null)
                {
                    foreach (var node in this.colonistQueueAsList)
                    {
                        this.colonistQueue.Enqueue(node);
                    }
                }
            }

            base.AfterAddedToWorld();
        }

        public virtual void UpdateDispenser()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            if (!this.IsDispenserSwitchedOn) yield break;

            for (int i = 4; i <= 7; i++)   // NE, SE, SW, NW
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                yield return tile;
            }
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            if (!this.IsDispenserSwitchedOn) yield break;

            this.CleanupColonistAssignments();
            var result = new List<ISmallTile>(4);
            for (int i = 4; i <= 7; i++)   // NE, SE, SW, NW
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallToDirection(direction)) continue;   // Can't work here
                if (tile.ThingsPrimary.Any(t => t is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && !c.IsRelaxing && c.ActivityType != ColonistActivityType.Lab && (c.Position - c.MainTile.TerrainPosition).Length() < 0.4f)) continue;   // Blocked by another colonist
                if (colonistId.HasValue && this.colonistsByAccessTile.ContainsKey(tile.Index) && this.colonistsByAccessTile[tile.Index] != colonistId) continue;  // Assigned to someone else
                yield return tile;
            }
        }

        public bool CanAssignColonist(int colonistId, int? tileIndex = null)
        {
            return tileIndex.HasValue
                ? this.GetAccessTiles(colonistId).Any(t => t.Index == tileIndex.Value)
                : this.GetAccessTiles(colonistId).Any();
        }

        public void AssignColonist(int colonistId, int tileIndex)
        {
            if (!this.colonistsByAccessTile.ContainsKey(tileIndex)) this.colonistsByAccessTile.Add(tileIndex, colonistId);
            else this.colonistsByAccessTile[tileIndex] = colonistId;
        }

        public int CountColonistAssignments(int? excludingColonistId = null)
        {
            this.CleanupColonistAssignments();
            return this.colonistsByAccessTile.Count(kv => !excludingColonistId.HasValue || kv.Value != excludingColonistId);
        }

        private void CleanupColonistAssignments()
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && c.TargetBuilingID == this.Id) continue;
                this.colonistsByAccessTile.Remove(id);
            }
        }
    }
}
