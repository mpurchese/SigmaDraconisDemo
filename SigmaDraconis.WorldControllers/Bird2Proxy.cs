namespace SigmaDraconis.WorldControllers
{
    using Draconis.Shared;
    using Shared;
    using World;

    internal class Bird2Proxy
    {
        private int animationCounter;

        public bool IsFadingIn { get; set; }
        public bool IsFadingOut { get; set; }
        public int AnimationFrame { get; set; }
        public float Alpha { get; set; } = 1f;
        public float Angle { get; set; }
        public Vector2f Position { get; set; }
        public Direction Direction { get; set; }

        public Bird2Proxy(float alpha, bool isFadingIn, float angle, int animationFrame, Vector2f position, Direction direction)
        {
            this.Alpha = alpha;
            this.IsFadingIn = isFadingIn;
            this.Angle = angle;
            this.AnimationFrame = animationFrame;
            this.Position = position;
            this.Direction = direction;
        }

        public void Update()
        {
            var a = this.Angle;
            var px = this.Position.X;
            var py = this.Position.Y;
            var sa = Mathf.Sin(a) * 0.025f;
            var ca = Mathf.Cos(a) * 0.025f;
            this.Position = new Vector2f(px + sa, py - ca);

            // Flying straight
            this.UpdateAnimationForFlapping();

            if (!this.IsFadingOut && !this.IsFadingIn && World.WorldTime.FrameNumber % 79 == 0)
            {
                if (px < 0 || py < 0 || px >= World.Width * 3 || py >= World.Width * 3) this.IsFadingOut = true;
                else
                {
                    var tileIndex = (int)(px + 0.5f) + ((int)(py + 0.5f) * World.Width * 3);
                    if (World.GetSmallTile(tileIndex) == null) this.IsFadingOut = true;
                }
            }

            if (this.IsFadingIn)
            {
                this.Alpha += 0.01f;
                if (this.Alpha >= 1.0f)
                {
                    this.Alpha = 1.0f;
                    this.IsFadingIn = false;
                }
            }
            else if (this.IsFadingOut)
            {
                this.Alpha -= 0.01f;
                if (this.Alpha <= 0.0f)
                {
                    this.Alpha = 0.0f;
                    this.IsFadingOut = false;
                }
            }
        }

        private void UpdateAnimationForFlapping()
        {
            this.animationCounter++;
            if (this.animationCounter >= 2)
            {
                this.AnimationFrame = this.Direction == Direction.W ? (this.AnimationFrame >= 20 ? 1 : this.AnimationFrame + 1) : (this.AnimationFrame >= 40 ? 21 : this.AnimationFrame + 1);
                this.animationCounter = 0;
            }
        }
    }
}
