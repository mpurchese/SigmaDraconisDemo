namespace SigmaDraconis.World.Fauna
{
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;
    using ProtoBuf;

    [ProtoContract]
    public class SnowTortoise : Animal
    {
        private float prevPositionX = 0;
        private float prevPositionY = 0;
        private int animationCounter = 0;

        public SnowTortoise() : base(ThingType.SnowTortoise)
        {
        }

        public SnowTortoise(ISmallTile tile) : base(ThingType.SnowTortoise, tile)
        {
        }

        public override void BeginResting()
        {
            this.IsResting = true;
        }

        public override void FinishResting()
        {
            this.IsResting = false;
        }

        public override void AfterAddedToWorld()
        {
            this.UpdateRenderPos();
            base.AfterAddedToWorld();
        }

        protected override void RaiseRendererUpdateEvent()
        {
            EventManager.MovedBugs.AddIfNew(this.Id);
        }

        public void Init()
        {
            this.RenderRow = this.MainTile.Row;
            this.Position = this.MainTile.TerrainPosition;
            this.UpdateRenderPos();
            this.PrevRenderRow = this.RenderRow;
            this.UpdateAnimationFrame();
            EventManager.MovedBugs.AddIfNew(this.Id);
        }

        public override void Update()
        {
            base.Update();

            if (this.UpdateAnimationFrame() || this.Position.X != this.prevPositionX || this.Position.Y != this.prevPositionY)
            {
                EventManager.MovedBugs.AddIfNew(this.Id);
                this.prevPositionX = this.Position.X;
                this.prevPositionY = this.Position.Y;
            }
        }

        private bool UpdateAnimationFrame(bool firstRun = false)
        {
            if (this.animationCounter > 0)
            {
                this.animationCounter--;
                return false;
            }

            this.animationCounter = 3;

            // f is the target animation frame
            var f = 56 - (int)((this.Rotation * 32f / Mathf.PI) + 0.5f);
            if (f < 0) f += 64;

            if (f == this.AnimationFrame) return false;

            if (firstRun)
            {
                this.AnimationFrame = f;
                return true;
            }

            if (f < this.AnimationFrame && f > this.AnimationFrame - 32) this.AnimationFrame--;
            else if (f > this.AnimationFrame && f < this.AnimationFrame + 32) this.AnimationFrame++;
            else if (this.AnimationFrame == 0) this.AnimationFrame = 63;
            else if (this.AnimationFrame == 63) this.AnimationFrame = 0;
            else if (f < this.AnimationFrame) this.AnimationFrame++;
            else this.AnimationFrame--;

            return true;
        }
    }
}
