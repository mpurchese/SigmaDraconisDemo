namespace SigmaDraconis.World.Fauna
{
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;
    using ProtoBuf;

    [ProtoContract]
    public class Fish : WaterAnimal
    {
        protected Fish() : base(ThingType.Fish)
        {
        }

        public Fish(ISmallTile tile) : base(ThingType.Fish, tile)
        {
            this.AnimationFrame = 1;
        }

        public void Init()
        {
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
