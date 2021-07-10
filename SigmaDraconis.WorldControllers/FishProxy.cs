namespace SigmaDraconis.WorldControllers
{
    using System;
    using System.Collections.Generic;
    using Draconis.Shared;
    using World;
    using World.Zones;

    internal class FishProxy
    {
        private Queue<Vector2f> routePlan = new Queue<Vector2f>();
        private int routePlanLength = 0;
        private static Random random = new Random();

        public bool IsFadingIn { get; set; }
        public bool IsFadingOut { get; set; }
        public int AnimationFrame { get; set; }
        public float Alpha { get; set; } = 1f;
        public float Angle { get; set; }
        public Vector2f Position { get; set; }

        public void Update()
        {
            if (this.routePlan == null) this.routePlan = new Queue<Vector2f>();

            if (this.routePlanLength == 0 && !this.IsFadingOut)
            {
                var success = false;
                for (var minSharpness = 0.005f; minSharpness <= 0.025f; minSharpness += 0.005f)
                {
                    if (this.PlanCurve(minSharpness))
                    {
                        success = true;
                        break;
                    }
                }

                if (!success) this.IsFadingOut = true;
            }

            if (this.routePlanLength > 0)
            {
                var next = this.routePlan.Dequeue();
                this.routePlanLength--;

                // Optimised way of doing this: this.Angle = (next - this.Position).Angle();
                var ax = next.X - this.Position.X;
                var ay = next.Y - this.Position.Y;
                this.Angle = Mathf.Atan2(ax, -ay);
                if (ax < 0) this.Angle += (Mathf.PI * 2f);

                var frame = 65 - Mathf.Clamp((int)(this.Angle * 32 / Mathf.PI) + 1, 1, 64);
                this.AnimationFrame = frame;
                this.Position = next;
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

        private bool PlanCurve(float minSharpness)
        {
            this.routePlan.Clear();
            this.routePlanLength = 0;

            // Normal distribution
            var u1 = 1f - (float)random.NextDouble();
            var u2 = 1f - (float)random.NextDouble();
            var randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
            var sharpness = Mathf.Max(minSharpness, 0.012f + (0.006f * randStdNormal));
            sharpness *= random.Next(2) == 0 ? 1 : -1;

            var length = random.Next((int)(Mathf.PI / Mathf.Abs(sharpness)));
            if (length > 100 && sharpness < 0.01f) length = 100;

            var success = true;
            var a = this.Angle;
            var p = this.Position.Clone();
            var sa = Mathf.Sin(a) * 0.01f;
            var ca = Mathf.Cos(a) * 0.01f;
            for (int i = 0; i < length; i++)
            {
                p = new Vector2f(p.X + sa, p.Y - ca);
                this.routePlan.Enqueue(p);
                this.routePlanLength++;

                if (i % 5 == 0)   // Optimisation
                {
                    var tileIndex = (int)(p.X + 0.5f) + ((int)(p.Y + 0.5f) * World.Width * 3);
                    if (!ZoneManager.WaterZone.Nodes.ContainsKey(tileIndex))
                    {
                        success = false;
                        break;
                    }

                    a += sharpness * 5;
                    if (a > Mathf.PI * 2f) a -= Mathf.PI * 2f;
                    else if (a < 0) a += Mathf.PI * 2f;
                    sa = Mathf.Sin(a) * 0.01f;
                    ca = Mathf.Cos(a) * 0.01f;
                }
            }

            if (!success)
            {
                // Out of bounds
                this.routePlan.Clear();
                this.routePlanLength = 0;
            }

            return success;
        }
    }
}
