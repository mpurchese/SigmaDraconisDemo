namespace SigmaDraconis.AI
{
    using System.Linq;
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.Zones;
    using WorldInterfaces;

    [ProtoContract]
    [ProtoInclude(10, typeof(ActionDrink))]
    [ProtoInclude(11, typeof(ActionEat))]
    [ProtoInclude(12, typeof(ActionFarmHarvest))]
    [ProtoInclude(13, typeof(ActionResearch))]
    [ProtoInclude(14, typeof(ActionSleep))]
    [ProtoInclude(15, typeof(ActionWait))]
    [ProtoInclude(16, typeof(ActionWalk))]
    [ProtoInclude(17, typeof(ActionCook))]
    [ProtoInclude(18, typeof(ActionPickupFromStack))]
    [ProtoInclude(19, typeof(ActionDropoff))]
    [ProtoInclude(20, typeof(ActionDeconstruct))]
    [ProtoInclude(21, typeof(ActionRelax))]
    [ProtoInclude(22, typeof(ActionRest))]
    [ProtoInclude(24, typeof(ActionConstruct))]
    [ProtoInclude(25, typeof(ActionConstructRoof))]
    [ProtoInclude(26, typeof(ActionRepair))]
    [ProtoInclude(27, typeof(ActionPickupFromNetwork))]
    [ProtoInclude(28, typeof(ActionHarvestFruit))]
    [ProtoInclude(29, typeof(ActionGeology))]
    [ProtoInclude(30, typeof(ActionFarmPlant))]
    [ProtoInclude(31, typeof(ActionPickupKek))]
    public abstract class ActionBase
    {
        public IColonist Colonist { get; private set; }

        [ProtoMember(1)]
        private readonly int colonistId;

        [ProtoMember(2)]
        public bool IsFinished { get; protected set; }

        [ProtoMember(3)]
        public bool IsFailed { get; protected set; }

        private bool isTileBlockApplied;

        // Deserialisation ctor
        protected ActionBase() { }

        public ActionBase(IColonist colonist)
        {
            this.Colonist = colonist;
            this.colonistId = colonist.Id;
        }

        [ProtoAfterDeserialization]
        public virtual void AfterDeserialization()
        {
            this.Colonist = World.GetThing(this.colonistId) as IColonist;
        }

        public virtual void Update()
        {
        }

        /// <summary>
        /// Rotates colonist towards a given angle.  Returns true once the angle is reached.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        protected bool RotateToAngle(float target, out float delta)
        {
            delta = 0f;

            while (this.Colonist.Rotation < 0) this.Colonist.Rotation += Mathf.PI * 2f;
            while (this.Colonist.Rotation > Mathf.PI * 2f) this.Colonist.Rotation -= Mathf.PI * 2f;
            while (target < 0) target += Mathf.PI * 2f;
            while (target > Mathf.PI * 2f) target -= Mathf.PI * 2f;

            this.Colonist.FacingDirection = DirectionHelper.GetDirectionFromAngle(this.Colonist.Rotation);

            if (this.Colonist.Rotation.ApproxEquals(target, 0.001f)) return true;

            // Get difference between current and target angles, in the range -PI to +PI
            delta = Mathf.AngleBetween(this.Colonist.Rotation, target);

            // Limit rotation speed
            var delta2 = delta.Clamp(-Mathf.PI / 60f, Mathf.PI / 60f);

            this.Colonist.Rotation += delta2;
            this.Colonist.Rotation = this.Colonist.Rotation % (Mathf.PI * 2f);

            return this.Colonist.Rotation.ApproxEquals(target, 0.001f);
        }

        protected void OpenDoorIfExists()
        {
            // Working through a door?  Door should be opened.
            foreach (var door in this.Colonist.MainTile.ThingsAll.OfType<IDoor>().Where(x => x.State == DoorState.Unlocked))
            {
                if (door.MainTileIndex == this.Colonist.MainTileIndex)
                {
                    if (door.Direction == this.Colonist.FacingDirection) door.Open();
                }
                else
                {
                    if (door.Direction == DirectionHelper.Reverse(this.Colonist.FacingDirection)) door.Open();
                }
            }
        }

        protected void StopMovement()
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

        protected void ApplyTileBlock()
        {
            if (this.isTileBlockApplied) return;
            PathFinderBlockManager.AddBlock(this.Colonist.Id, this.Colonist.MainTile, Direction.N, TileBlockType.All, true);
            PathFinderBlockManager.AddBlock(this.Colonist.Id, this.Colonist.MainTile, Direction.E, TileBlockType.All, true);
            PathFinderBlockManager.AddBlock(this.Colonist.Id, this.Colonist.MainTile, Direction.S, TileBlockType.All, true);
            PathFinderBlockManager.AddBlock(this.Colonist.Id, this.Colonist.MainTile, Direction.W, TileBlockType.All, true);
            PathFinderBlockManager.AddBlock(this.Colonist.Id, this.Colonist.MainTile, Direction.NW, TileBlockType.All, true);
            PathFinderBlockManager.AddBlock(this.Colonist.Id, this.Colonist.MainTile, Direction.NE, TileBlockType.All, true);
            PathFinderBlockManager.AddBlock(this.Colonist.Id, this.Colonist.MainTile, Direction.SW, TileBlockType.All, true);
            PathFinderBlockManager.AddBlock(this.Colonist.Id, this.Colonist.MainTile, Direction.SE, TileBlockType.All, true);
            ZoneManager.HomeZone.UpdateNode(this.Colonist.MainTileIndex);
            ZoneManager.GlobalZone.UpdateNode(this.Colonist.MainTileIndex);
            EventManager.RaiseEvent(EventType.Zone, null);
            this.isTileBlockApplied = true;
        }

        protected void ReleaseTileBlock()
        {
            PathFinderBlockManager.RemoveBlocks(this.Colonist.Id);
            ZoneManager.HomeZone.UpdateNode(this.Colonist.MainTileIndex);
            ZoneManager.GlobalZone.UpdateNode(this.Colonist.MainTileIndex);
            EventManager.RaiseEvent(EventType.Zone, null);
            this.isTileBlockApplied = false;
        }

        protected virtual Vector2f GetToolOffset()
        {
            switch (this.Colonist.FacingDirection)
            {
                case Direction.N:
                    return new Vector2f(0, -2.8f);
                case Direction.NE:
                    return new Vector2f(3.9f, -1.95f);
                case Direction.E:
                    return new Vector2f(5.6f, 0f);
                case Direction.SE:
                    return new Vector2f(3.9f, 1.95f);
                case Direction.S:
                    return new Vector2f(0, 2.8f);
                case Direction.SW:
                    return new Vector2f(-3.9f, 1.95f);
                case Direction.W:
                    return new Vector2f(-5.6f, 0f);
                case Direction.NW:
                    return new Vector2f(-3.9f, -1.95f);
            }

            return new Vector2f();
        }
    }
}
