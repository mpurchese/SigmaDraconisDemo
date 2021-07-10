namespace SigmaDraconis.UI
{
    using System;
    using Draconis.UI;

    public abstract class PanelLeft : PanelBase
    {
        public PanelLeft(IUIElement parent, int y, int width, int height, string title)
            : base(parent, -width, y, width, height, title)
        {
            this.AnchorLeft = true;
            this.AnchorRight = false;
        }

        public override void Update()
        {
            if (this.lastUpdateTime.AddMilliseconds(50) < DateTime.UtcNow)
            {
                this.lastUpdateTime = DateTime.UtcNow;
            }

            if (this.isShown && this.X < Scale(8))
            {
                this.X = Math.Min(Scale(8), this.X + (int)((DateTime.UtcNow - this.lastUpdateTime).TotalMilliseconds * this.speedMultiplier));
            }
            else if (!this.isShown && this.X > -this.W)
            {
                this.X = Math.Max(-this.W, this.X - (int)((DateTime.UtcNow - this.lastUpdateTime).TotalMilliseconds * this.speedMultiplier));
                if (this.X == -this.W)
                {
                    this.IsVisible = false;
                }
            }
            else if (!this.isShown && this.IsVisible && this.X <= -this.W)
            {
                // Not sure how we get here, but this fixes a UI bug discovered in 0.0.10
                this.IsVisible = false;
            }

            base.Update();
        }
    }
}
