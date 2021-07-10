namespace SigmaDraconis.WorldControllers
{
    using Draconis.Shared;
    using Shared;
    using World;

    internal class Bird1Proxy
    {
        private int animationCounter;
        private const float sharpness = 0.01f;

        public bool IsFadingIn { get; set; }
        public bool IsFadingOut { get; set; }
        public int AnimationFrame { get; set; }
        public float Alpha { get; set; } = 1f;
        public float Angle { get; set; }
        public float Height { get; set; }
        public Vector2f Position { get; set; }
        public Direction Direction { get; set; }
        public int Turning { get; set; }

        public Bird1Proxy(float alpha, bool isFadingIn, float angle, float height, int animationFrame, Vector2f position, Direction direction, int turning)
        {
            this.Alpha = alpha;
            this.IsFadingIn = isFadingIn;
            this.Angle = angle;
            this.Height = height;
            this.AnimationFrame = animationFrame;
            this.Position = position;
            this.Direction = direction;
            this.Turning = turning;
        }

        public void Update()
        {
            var a = this.Angle;
            var px = this.Position.X;
            var py = this.Position.Y;
            var sa = Mathf.Sin(a) * 0.02f;
            var ca = Mathf.Cos(a) * 0.02f;
            this.Position = new Vector2f(px + sa, py - ca);

            var newDirection = this.Direction;

            switch (this.Turning)
            {
                case 0:
                    // Flying straight
                    if (this.AnimationFrame < 169)  // Flapping wings
                    {
                        if (this.Height < 300f) this.Height += 0.1f;
                        if (this.Height > 250f && (this.AnimationFrame - 14) % 20 == 0 && Rand.Next(10) == 0)
                        {
                            // Transition to glide
                            this.UpdateAnimationForGlide();
                        }
                        else this.UpdateAnimationForFlapping();
                    }
                    else
                    {
                        if (this.Height > 150f) this.Height -= 0.1f;
                        if (this.Height <= 200f) this.UpdateAnimationForFlapping();
                        else
                        {
                            this.UpdateAnimationForGlide();
                            var r1 = Rand.Next(2400);
                            if (r1 == 0) this.Turning = -1;
                            else if (r1 == 1) this.Turning = 1;
                        }
                    }
                    break;
                case -1:
                    // Turning left
                    this.Angle -= sharpness;
                    if (this.Angle < 0f)
                    {
                        this.Angle += Mathf.PI * 2f;
                        newDirection = Direction.NW;
                    }
                    else if (this.Angle < Mathf.PI / 4f && this.Angle + sharpness >= Mathf.PI / 4f) newDirection = Direction.N;
                    else if (this.Angle < Mathf.PI / 2f && this.Angle + sharpness >= Mathf.PI / 2f) newDirection = Direction.NE;
                    else if (this.Angle < 3f * Mathf.PI / 4f && this.Angle + sharpness >= 3f * Mathf.PI / 4f) newDirection = Direction.E;
                    else if (this.Angle < Mathf.PI && this.Angle + sharpness >= Mathf.PI) newDirection = Direction.SE;
                    else if (this.Angle < 5f * Mathf.PI / 4f && this.Angle + sharpness >= 5f * Mathf.PI / 4f) newDirection = Direction.S;
                    else if (this.Angle < 3f * Mathf.PI / 2f && this.Angle + sharpness >= 3f * Mathf.PI / 2f) newDirection = Direction.SW;
                    else if (this.Angle < 7f * Mathf.PI / 4f && this.Angle + sharpness >= 7f * Mathf.PI / 4f) newDirection = Direction.W;

                    if (this.Height > 150f) this.Height -= 0.1f;
                    this.UpdateAnimationForGlide();
                    break;
                case 1:
                    // Turning right
                    this.Angle += sharpness;
                    if (this.Angle > Mathf.PI * 2f)
                    {
                        this.Angle -= Mathf.PI * 2f;
                        newDirection = Direction.NW;
                    }
                    else if (this.Angle > Mathf.PI / 4f && this.Angle - sharpness <= Mathf.PI / 4f) newDirection = Direction.N;
                    else if (this.Angle > Mathf.PI / 2f && this.Angle - sharpness <= Mathf.PI / 2f) newDirection = Direction.NE;
                    else if (this.Angle > 3f * Mathf.PI / 4f && this.Angle - sharpness <= 3f * Mathf.PI / 4f) newDirection = Direction.E;
                    else if (this.Angle > Mathf.PI && this.Angle - sharpness <= Mathf.PI) newDirection = Direction.SE;
                    else if (this.Angle > 5f * Mathf.PI / 4f && this.Angle - sharpness <= 5f * Mathf.PI / 4f) newDirection = Direction.S;
                    else if (this.Angle > 3f * Mathf.PI / 2f && this.Angle - sharpness <= 3f * Mathf.PI / 2f) newDirection = Direction.SW;
                    else if (this.Angle > 7f * Mathf.PI / 4f && this.Angle - sharpness <= 7f * Mathf.PI / 4f) newDirection = Direction.W;

                    if (this.Height > 150f) this.Height -= 0.1f;
                    this.UpdateAnimationForGlide();
                    break;
            }

            if (newDirection != this.Direction)
            {
                this.Direction = newDirection;
                if (this.Height < 160f || Rand.Next(2) == 0) this.Turning = 0;
            }
            else if (this.Turning == 0 && !this.IsFadingOut && !this.IsFadingIn && World.WorldTime.FrameNumber % 79 == 0)
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
            if (this.AnimationFrame >= 169)
            {
                // Transition from glide
                switch (this.Direction)
                {
                    case Direction.NW: this.AnimationFrame = 14; break;
                    case Direction.N: this.AnimationFrame = 34; break;
                    case Direction.NE: this.AnimationFrame = 54; break;
                    case Direction.E: this.AnimationFrame = 74; break;
                    case Direction.SE: this.AnimationFrame = 94; break;
                    case Direction.S: this.AnimationFrame = 114; break;
                    case Direction.SW: this.AnimationFrame = 134; break;
                    case Direction.W: this.AnimationFrame = 154; break;
                }

                return;
            }

            this.animationCounter++;
            if (this.animationCounter >= 2)
            {
                switch (this.Direction)
                {
                    case Direction.NW: this.AnimationFrame = this.AnimationFrame >= 28 ? 9 : this.AnimationFrame + 1; break;
                    case Direction.N: this.AnimationFrame = this.AnimationFrame >= 48 ? 29 : this.AnimationFrame + 1; break;
                    case Direction.NE: this.AnimationFrame = this.AnimationFrame >= 68 ? 49 : this.AnimationFrame + 1; break;
                    case Direction.E: this.AnimationFrame = this.AnimationFrame >= 88 ? 69 : this.AnimationFrame + 1; break;
                    case Direction.SE: this.AnimationFrame = this.AnimationFrame >= 108 ? 89 : this.AnimationFrame + 1; break;
                    case Direction.S: this.AnimationFrame = this.AnimationFrame >= 128 ? 109 : this.AnimationFrame + 1; break;
                    case Direction.SW: this.AnimationFrame = this.AnimationFrame >= 148 ? 129 : this.AnimationFrame + 1; break;
                    case Direction.W: this.AnimationFrame = this.AnimationFrame >= 168 ? 149 : this.AnimationFrame + 1; break;
                }

                this.animationCounter = 0;
            }
        }

        private void UpdateAnimationForGlide()
        {
            if (this.Turning == 0)
            {
                switch (this.Direction)
                {
                    case Direction.NW: this.AnimationFrame = 169; break;
                    case Direction.N: this.AnimationFrame = 178; break;
                    case Direction.NE: this.AnimationFrame = 187; break;
                    case Direction.E: this.AnimationFrame = 196; break;
                    case Direction.SE: this.AnimationFrame = 205; break;
                    case Direction.S: this.AnimationFrame = 214; break;
                    case Direction.SW: this.AnimationFrame = 223; break;
                    case Direction.W: this.AnimationFrame = 232; break;
                }
            }
            else this.AnimationFrame = (int)(this.Angle * 11.4591559f).Clamp(0, 71) + 169;
        }
    }
}
