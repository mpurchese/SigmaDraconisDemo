namespace SigmaDraconis.World.Fauna
{
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;
    using ProtoBuf;

    [ProtoContract]
    public class Bird1 : Animal, IBird
    {
        [ProtoMember(1)]
        public int Speed { get; set; }

        [ProtoMember(2)]
        public float Angle { get; set; }

        [ProtoMember(3)]
        public float Height { get; set; }

        [ProtoMember(4)]
        public int Turning { get; set; }  // -1 = turning left, +1 = turning right

        protected Bird1() : base(ThingType.Bird1)
        {
        }

        public Bird1(ISmallTile tile) : base(ThingType.Bird1, tile)
        {
        }

        public void Init()
        {
            this.Height = 200;
            switch (this.FacingDirection)
            {
                case Direction.NW:
                    this.AnimationFrame = 169;
                    this.Angle = 0;
                    break;
                case Direction.N:
                    this.AnimationFrame = 178;
                    this.Angle = Mathf.PI / 4f;
                    break;
                case Direction.NE:
                    this.AnimationFrame = 187;
                    this.Angle = Mathf.PI / 2f;
                    break;
                case Direction.E:
                    this.AnimationFrame = 196;
                    this.Angle = 3f * Mathf.PI / 4f;
                    break;
                case Direction.SE:
                    this.AnimationFrame = 205;
                    this.Angle = Mathf.PI;
                    break;
                case Direction.S:
                    this.AnimationFrame = 214;
                    this.Angle = 5f * Mathf.PI / 4f;
                    break;
                case Direction.SW:
                    this.AnimationFrame = 223;
                    this.Angle = 3f * Mathf.PI / 2f;
                    break;
                case Direction.W:
                    this.AnimationFrame = 232;
                    this.Angle = 7f * Mathf.PI / 4f;
                    break;
            }

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
