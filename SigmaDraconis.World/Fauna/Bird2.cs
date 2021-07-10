namespace SigmaDraconis.World.Fauna
{
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;
    using ProtoBuf;

    [ProtoContract]
    public class Bird2 : Animal, IBird
    {
        [ProtoMember(1)]
        public int Speed { get; set; }

        [ProtoMember(2)]
        public float Angle { get; set; }

        [ProtoMember(3)]
        public float Height { get; set; }

        [ProtoMember(4)]
        public int Turning { get; set; }  // -1 = turning left, +1 = turning right

        protected Bird2() : base(ThingType.Bird2)
        {
        }

        public Bird2(ISmallTile tile) : base(ThingType.Bird2, tile)
        {
        }

        public void Init()
        {
            this.Height = 200;
            this.AnimationFrame = this.FacingDirection == Direction.W ? 6 : 26;
            this.Angle = (this.FacingDirection == Direction.W ? 7f : 3f) * Mathf.PI / 4f;   // Always facing W for now
            this.RenderRow = this.MainTile.Row;
            this.Position = this.MainTile.TerrainPosition;
            this.UpdateRenderPos();
            this.PrevRenderRow = this.RenderRow;
            this.renderAlpha = 0;
            this.IsFadingIn = true;
            this.affectsPathFinders = false;
            this.RaiseRendererUpdateEvent();
        }

        public override void AfterDeserialization()
        {
            this.UpdateRenderPos();
            this.PrevRenderRow = this.RenderRow;
            this.affectsPathFinders = false;
            base.AfterDeserialization();
        }

        public override void UpdateRenderPos()
        {
            var cx = (10.6666667 * this.Position.X) + (10.6666667 * this.Position.Y);
            var cy = (5.3333333 * this.Position.Y) - (5.3333333 * this.Position.X);
            this.RenderPos = new Vector2f((float)cx + 10.667f, (float)cy + 16f);

            // Don't need to worry which tile we're in, as renderer does all at once
        }
    }
}
