namespace SigmaDraconis.World.Buildings
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using WorldInterfaces;
    using Shared;

    [ProtoContract]
    public class Table : Building, ITable
    {
        [ProtoMember(1)]
        protected Dictionary<int, int> colonistsByAccessTile;

        [ProtoMember(2)]
        public bool HasKekNE { get; protected set; }

        [ProtoMember(3)]
        public bool HasKekSE { get; protected set; }

        [ProtoMember(4)]
        public bool HasKekSW { get; protected set; }

        [ProtoMember(5)]
        public bool HasKekNW { get; protected set; }

        public bool RequiresAccessNow => this.IsReady;

        public Table() : base(ThingType.None)
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public Table(ISmallTile smallTile, ThingType thingType) : base(thingType, smallTile, 1)
        {
            this.colonistsByAccessTile = new Dictionary<int, int>();
        }

        public void AddKek(Direction direction)
        {
            switch(direction)
            {
                case Direction.NE: this.HasKekNE = true; break;
                case Direction.SE: this.HasKekSE = true; break;
                case Direction.SW: this.HasKekSW = true; break;
                case Direction.NW: this.HasKekNW = true; break;
            }

            this.UpdateAnimationFrame();
        }

        public void RemoveKek(Direction direction)
        {
            switch (direction)
            {
                case Direction.NE: this.HasKekNE = false; break;
                case Direction.SE: this.HasKekSE = false; break;
                case Direction.SW: this.HasKekSW = false; break;
                case Direction.NW: this.HasKekNW = false; break;
            }

            this.UpdateAnimationFrame();
        }

        private void UpdateAnimationFrame()
        {
            var frame = 1;
            if (this.HasKekNE) frame++;
            if (this.HasKekSE) frame += 2;
            if (this.HasKekSW) frame += 4;
            if (this.HasKekNW) frame += 8;

            this.AnimationFrame = frame;
        }

        public override void Update()
        {
            if (World.WorldTime.FrameNumber % 600 == 0)
            {
                if (this.HasKekNE && !this.colonistsByAccessTile.ContainsKey(this.MainTile.TileToNE.Index)) this.RemoveKek(Direction.NE);
                if (this.HasKekNW && !this.colonistsByAccessTile.ContainsKey(this.MainTile.TileToNW.Index)) this.RemoveKek(Direction.NW);
                if (this.HasKekSW && !this.colonistsByAccessTile.ContainsKey(this.MainTile.TileToSW.Index)) this.RemoveKek(Direction.SW);
                if (this.HasKekSE && !this.colonistsByAccessTile.ContainsKey(this.MainTile.TileToSE.Index)) this.RemoveKek(Direction.SE);
            }

            base.Update();
        }

        public IEnumerable<ISmallTile> GetAllAccessTiles()
        {
            for (int i = 4; i <= 7; i++)   // NE, SE, SW, NW
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallOrDoorToDirection(direction)) continue;   // Can't work here
                yield return tile;
            }
        }

        public IEnumerable<ISmallTile> GetAccessTiles(int? colonistId = null)
        {
            if (!this.IsReady) yield break;

            this.CleanupColonistAssignments();
            var result = new List<ISmallTile>(4);
            for (int i = 4; i <= 7; i++)   // NE, SE, SW, NW
            {
                var direction = (Direction)i;
                var tile = this.mainTile.GetTileToDirection(direction);
                if (tile == null || !tile.CanWorkInTile || this.MainTile.HasWallOrDoorToDirection(direction)) continue;   // Can't work here
                if (tile.ThingsPrimary.Any(t => t is IColonist c && (colonistId == null || c.Id != colonistId) && !c.IsMoving && c.ActivityType != ColonistActivityType.Lab)) continue;   // Blocked by another colonist
                if (colonistId.HasValue && this.colonistsByAccessTile.ContainsKey(tile.Index) && this.colonistsByAccessTile[tile.Index] != colonistId) continue;  // Assigned to someone else
                yield return tile;
            }
        }

        public List<int> GetOtherColonists(int colonistId)
        {
            this.CleanupColonistAssignments();
            return this.colonistsByAccessTile.Values.Where(v => v != colonistId).ToList();
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

        private void CleanupColonistAssignments()
        {
            if (this.colonistsByAccessTile == null) this.colonistsByAccessTile = new Dictionary<int, int>();
            foreach (var id in this.colonistsByAccessTile.Keys.ToList())
            {
                if (World.GetThing(this.colonistsByAccessTile[id]) is IColonist c && (c.ActivityType == ColonistActivityType.Relax || c.ActivityType == ColonistActivityType.DrinkKek) && !c.IsDead) continue;
                this.colonistsByAccessTile.Remove(id);
            }
        }
    }
}
