namespace SigmaDraconis.World.Fauna
{
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;
    using ProtoBuf;

    [ProtoContract]
    [ProtoInclude(100, typeof(RedBug))]
    [ProtoInclude(101, typeof(BlueBug))]
    public class Bug : Animal
    {
        private float prevPositionX = 0;
        private float prevPositionY = 0;
        private int animationStep = 1;
        private float animationStepFrac = 0;

        public override float Acceleration => 0.1f;

        public Bug() : base(ThingType.None)
        {
        }

        public Bug(ThingType type) : base(type)
        {
        }

        public Bug(ThingType type, ISmallTile tile) : base(type, tile)
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

        private bool UpdateAnimationFrame()
        {
            this.animationStepFrac += this.CurrentSpeed * 12f;
            if (this.animationStepFrac > 1f)
            {
                this.animationStepFrac -= 1f;
                this.animationStep++;
                if (this.animationStep > 8) this.animationStep = 1;
            }

            var f = ((6 + (int)((this.Rotation * 24f / Mathf.PI) + 0.5f)) % 48) * 8;
            f += this.animationStep;

            if (f == this.AnimationFrame) return false;

            this.AnimationFrame = f;

            return true;
        }
    }
}
