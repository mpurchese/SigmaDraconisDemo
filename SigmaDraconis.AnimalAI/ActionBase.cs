namespace SigmaDraconis.AnimalAI
{
    using ProtoBuf;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.Zones;
    using WorldInterfaces;

    [ProtoContract]
    [ProtoInclude(10, typeof(ActionWalk))]
    [ProtoInclude(11, typeof(ActionWait))]
    [ProtoInclude(12, typeof(ActionSleep))]
    [ProtoInclude(13, typeof(ActionEat))]
    public abstract class ActionBase
    {
        public IAnimal Animal { get; private set; }

        [ProtoMember(1)]
        private readonly int animalId;

        [ProtoMember(2)]
        public bool IsFinished { get; protected set; }

        [ProtoMember(3)]
        public bool IsFailed { get; protected set; }

        private bool isTileBlockApplied;

        // Deserialisation ctor
        protected ActionBase() { }

        public ActionBase(IAnimal animal)
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

        /// <summary>
        /// Rotates animal towards a given angle.  Returns true once the angle is reached.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        protected bool RotateToAngle(float target, out float delta, float rate = Mathf.PI / 60f)
        {
            delta = 0f;

            while (this.Animal.Rotation < 0) this.Animal.Rotation += Mathf.PI * 2f;
            while (this.Animal.Rotation > Mathf.PI * 2f) this.Animal.Rotation -= Mathf.PI * 2f;
            while (target < 0) target += Mathf.PI * 2f;
            while (target > Mathf.PI * 2f) target -= Mathf.PI * 2f;

            this.Animal.FacingDirection = DirectionHelper.GetDirectionFromAngle(this.Animal.Rotation);

            if (this.Animal.Rotation.ApproxEquals(target, 0.001f)) return true;

            // Get difference between current and target angles, in the range -PI to +PI
            delta = Mathf.AngleBetween(this.Animal.Rotation, target);

            // Limit rotation speed
            var delta2 = delta.Clamp(-rate, rate);

            this.Animal.Rotation += delta2;
            this.Animal.Rotation = this.Animal.Rotation % (Mathf.PI * 2f);

            return this.Animal.Rotation.ApproxEquals(target, 0.001f);
        }

        protected void ApplyTileBlock()
        {
            if (this.isTileBlockApplied) return;
            PathFinderBlockManager.AddBlock(this.Animal.Id, this.Animal.MainTile, Direction.N, TileBlockType.Animal);
            PathFinderBlockManager.AddBlock(this.Animal.Id, this.Animal.MainTile, Direction.E, TileBlockType.Animal);
            PathFinderBlockManager.AddBlock(this.Animal.Id, this.Animal.MainTile, Direction.S, TileBlockType.Animal);
            PathFinderBlockManager.AddBlock(this.Animal.Id, this.Animal.MainTile, Direction.W, TileBlockType.Animal);
            PathFinderBlockManager.AddBlock(this.Animal.Id, this.Animal.MainTile, Direction.NW, TileBlockType.Animal);
            PathFinderBlockManager.AddBlock(this.Animal.Id, this.Animal.MainTile, Direction.NE, TileBlockType.Animal);
            PathFinderBlockManager.AddBlock(this.Animal.Id, this.Animal.MainTile, Direction.SW, TileBlockType.Animal);
            PathFinderBlockManager.AddBlock(this.Animal.Id, this.Animal.MainTile, Direction.SE, TileBlockType.Animal);
            ZoneManager.AnimalZone.UpdateNode(this.Animal.MainTileIndex);
            EventManager.RaiseEvent(EventType.Zone, null);
            this.isTileBlockApplied = true;
        }

        protected void ReleaseTileBlock()
        {
            PathFinderBlockManager.RemoveBlocks(this.Animal.Id);
            ZoneManager.AnimalZone.UpdateNode(this.Animal.MainTileIndex);
            EventManager.RaiseEvent(EventType.Zone, null);
            this.isTileBlockApplied = false;
        }
    }
}
