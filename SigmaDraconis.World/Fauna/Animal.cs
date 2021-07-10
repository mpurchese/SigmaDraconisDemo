namespace SigmaDraconis.World.Fauna
{
    using Shared;
    using Draconis.Shared;
    using ProtoBuf;
    using WorldInterfaces;

    [ProtoContract]
    [ProtoInclude(100, typeof(Bug))]
    [ProtoInclude(101, typeof(Colonist))]
    [ProtoInclude(102, typeof(FlyingInsect))]
    [ProtoInclude(103, typeof(WaterAnimal))]
    [ProtoInclude(104, typeof(Tortoise))]
    [ProtoInclude(105, typeof(Bird1))]
    [ProtoInclude(106, typeof(Bird2))]
    [ProtoInclude(107, typeof(SnowTortoise))]
    public abstract class Animal : Thing, IAnimal
    {
        [ProtoMember(1)]
        public Direction FacingDirection { get; set; }

        [ProtoMember(2)]
        public bool IsMoving { get; set; }

        [ProtoMember(4)]
        public Vector2f PositionOffset { get; protected set; }

        [ProtoMember(5)]
        public int AnimationFrame { get; set; }

        [ProtoMember(7)]
        public bool IsYoung { get; set; }

        [ProtoMember(8)]
        public int PrevTileIndex { get; private set; }

        [ProtoMember(10)]
        public int WaitTimer { get; set; }

        [ProtoMember(12)]
        public float CurrentSpeed { get; set; }

        [ProtoMember(15)]
        public bool IsDead { get; protected set; }

        [ProtoMember(19)]
        public float Rotation { get; set; }

        [ProtoMember(20)]
        public Vector2f Position { get; set; }

        public Vector2f RenderPos { get; set; }

        [ProtoMember(21)]
        public float? MovingAngle { get; set; }

        [ProtoMember(22)]
        public bool IsResting { get; set; }

        [ProtoMember(23)]
        public bool IsFadingIn { get; set; }

        [ProtoMember(24)]
        public bool IsFadingOut { get; set; }

        [ProtoMember(25)]
        public bool IsEating { get; set; }

        [ProtoMember(26)]
        public bool IsWaiting { get; set; }

        public int? PrevRenderRow { get; set; }
        public int? RenderRow { get; protected set; }

        public virtual bool IsHungry => false;
        public virtual bool IsThirsty => false;
        public virtual bool IsTired => false;

        public virtual float Acceleration => 0.02f;

        public Animal() : base(ThingType.None)
        {
        }

        public Animal(ThingType type) : base(type)
        {
        }

        public Animal(ThingType type, ISmallTile tile) : base(type, tile, 1)
        {
            this.PositionOffset = new Vector2f();
            this.Position = new Vector2f(tile.X, tile.Y);
        }

        public override Vector2f GetWorldPosition()
        {
            return this.MainTile.CentrePosition + this.PositionOffset;
        }

        public virtual void BeginEating() { }
        public virtual void FinishEating() { }
        public virtual void BeginResting() { }
        public virtual void FinishResting() { }

        public virtual void UpdateRenderRow()
        {
            this.RenderRow = this.mainTile.Row;
        }

        public virtual void UpdateRenderPos()
        {
            var cx = (10.6666667 * this.Position.X) + (10.6666667 * this.Position.Y);
            var cy = (5.3333333 * this.Position.Y) - (5.3333333 * this.Position.X);
            this.RenderPos = new Vector2f((float)cx + 10.667f, (float)cy + 16f);

            var tile = World.GetSmallTile((int)(this.Position.X + 0.5f), (int)(this.Position.Y + 0.5f));
            if (tile != null && tile != this.mainTile)
            {
                this.SetPosition(tile);
                World.UpdateThingPosition(this);  // Updates World.ThingsByRow
            }

            if (tile != null)
            {
                if (this.ThingType != ThingType.Colonist) this.RenderRow = tile.Row;   // Colonist renderer updates render row

                var offset = this.Position - this.MainTile.TerrainPosition;
                this.PositionOffset.X = (offset.X + offset.Y) * 10.66667f;
                this.PositionOffset.Y = (offset.Y - offset.X) * 5.33333f;
            }
        }

        protected virtual void RaiseRendererUpdateEvent()
        {
            EventManager.RaiseEvent(EventType.Animal, EventSubType.Updated, this);
        }
    }
}
