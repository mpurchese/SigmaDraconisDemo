namespace SigmaDraconis.World.Fauna
{
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;
    using ProtoBuf;

    [ProtoContract]
    public class Tortoise : Animal
    {
        private float prevPositionX = 0;
        private float prevPositionY = 0;
        private int animationCounter = 0;

        [ProtoMember(1)]
        private int framesSinceSleep;

        [ProtoMember(2)]
        private int framesSinceEat;

        public override bool IsHungry => this.framesSinceEat > 3600 * 3;
        public override bool IsTired => this.framesSinceSleep > 3600 * 6 || (this.framesSinceSleep > 3600 * 5 && this.framesSinceEat < 3600 * 2);

        public Tortoise() : base(ThingType.Tortoise)
        {
        }

        public Tortoise(ISmallTile tile) : base(ThingType.Tortoise, tile)
        {
        }

        public void Init()
        {
            this.RenderRow = this.MainTile.Row;
            this.Position = this.MainTile.TerrainPosition;
            this.UpdateRenderPos();
            this.PrevRenderRow = this.RenderRow;

            this.UpdateAnimationFrame(true);
            EventManager.MovedAnimals.AddIfNew(this.Id);
            this.prevPositionX = this.Position.X;
            this.prevPositionY = this.Position.Y;

            this.framesSinceEat = Rand.Next(3600 * 3);
            this.framesSinceSleep = Rand.Next(3600 * 5);
        }

        public override void AfterAddedToWorld()
        {
            this.UpdateRenderPos();
            this.PrevRenderRow = this.RenderRow;
            base.AfterAddedToWorld();
        }

        public override void BeginEating()
        {
            this.IsEating = true;
        }

        public override void FinishEating()
        {
            this.IsEating = false;
            this.framesSinceEat = 0;
        }

        public override void BeginResting()
        {
            this.IsResting = true;
        }

        public override void FinishResting()
        {
            this.IsResting = false;
            this.framesSinceSleep = 0;
        }

        public override void Update()
        {
            base.Update();

            if (this.UpdateAnimationFrame() || this.Position.X != this.prevPositionX || this.Position.Y != this.prevPositionY)
            {
                EventManager.MovedAnimals.AddIfNew(this.Id);
                this.prevPositionX = this.Position.X;
                this.prevPositionY = this.Position.Y;
            }

            this.framesSinceEat++;
            this.framesSinceSleep++;
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
            if (this.IsResting) f = 64 + (16 * (f / 16));
            else if (this.IsEating) f = 76 + (16 * (f / 16)) + Rand.Next(4);

            if (f == this.AnimationFrame) return false;

            if (firstRun)
            {
                this.AnimationFrame = f;
                return true;
            }

            var f2 = f < 64 ? f : 16 * ((f - 64) / 16);    // f2 is transition frame between rotation and animation.  These are 0, 16, 32, 48.
            if (f >= 64 && this.AnimationFrame == f2) this.AnimationFrame += 72;  // Switch from rotation to animation
            else if (f < 64 && this.AnimationFrame == f2 + 72) this.AnimationFrame -= 72;  // Switch from animation to rotation
            else if (this.AnimationFrame < 64)
            {
                // Rotating
                if (f2 < this.AnimationFrame && f2 > this.AnimationFrame - 32) this.AnimationFrame--;
                else if (f2 > this.AnimationFrame && f2 < this.AnimationFrame + 32) this.AnimationFrame++;
                else if (this.AnimationFrame == 0) this.AnimationFrame = 63;
                else if (this.AnimationFrame == 63) this.AnimationFrame = 0;
                else if (f2 < this.AnimationFrame) this.AnimationFrame++;
                else this.AnimationFrame--;
            }
            else if (f >= 64 && this.AnimationFrame > f) this.AnimationFrame--; // -> Rest
            else if (f >= 64 && this.AnimationFrame < f) this.AnimationFrame++; // -> Eat
            else if (this.AnimationFrame < f2 + 72) this.AnimationFrame++;      // Finish resting
            else if (this.AnimationFrame > f2 + 72) this.AnimationFrame--;      // Finish eating

            return true;
        }
    }
}
