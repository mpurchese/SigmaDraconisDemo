namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Draconis.UI;

    public abstract class PanelBottom : PanelBase
    {
        public PanelBottom(IUIElement parent, int width, int height, string title)
            : base(parent, (int)((parent.W - width) * 0.5), parent.H, width, height, title)
        {
            this.AnchorTop = false;
            this.AnchorBottom = true;
            this.backgroundColour = new Color(0, 0, 0, 128);
        }

        public override void Update()
        {
            if (this.lastUpdateTime.AddMilliseconds(50) < DateTime.UtcNow)
            {
                this.lastUpdateTime = DateTime.UtcNow;
            }

            if (this.X != (int)((this.Parent.W - this.W) * 0.5))
            {
                this.X = (int)((this.Parent.W - this.W) * 0.5);
            }

            if (this.Y < this.Parent.H - this.H - Scale(26)) this.Y = this.Parent.H - this.H - Scale(26);

            if (this.isShown && this.Y > this.Parent.H - this.H - Scale(26))
            {
                this.Y = Math.Max(this.Parent.H - this.H - Scale(26), this.Y - (int)((DateTime.UtcNow - this.lastUpdateTime).TotalMilliseconds * this.speedMultiplier));
            }
            else if (!this.isShown && this.Y < this.Parent.H)
            {
                this.Y = Math.Min(this.Parent.H, this.Y + (int)((DateTime.UtcNow - this.lastUpdateTime).TotalMilliseconds * this.speedMultiplier));
                if (this.Y == this.Parent.H)
                {
                    this.IsVisible = false;
                }
            }

            base.Update();
        }
    }
}
