namespace SigmaDraconis.UI
{
    using System;
    using Draconis.UI;

    public abstract class PanelRight : PanelBase
    {
        public PanelRight(IUIElement parent, int y, int width, int height, string title)
            : base(parent, parent.W, y, width, height, title)
        {
            this.AnchorLeft = false;
            this.AnchorRight = true;
        }

        public override void Update()
        {
            if (this.lastUpdateTime.AddMilliseconds(50) < DateTime.UtcNow)
            {
                this.lastUpdateTime = DateTime.UtcNow;
            }

            if (this.X < this.Parent.W - this.W - Scale(8))
            {
                // Used when switch to fullscreen
                this.X = this.Parent.W - this.W - Scale(8);
            }
            else if (this.isShown && this.X > this.Parent.W - this.W - Scale(8))
            {
                this.X = Math.Max(this.Parent.W - this.W - Scale(8), this.X - (int)((DateTime.UtcNow - this.lastUpdateTime).TotalMilliseconds * this.speedMultiplier));
            }
            else if (!this.isShown && this.X < this.Parent.W)
            {
                this.X = Math.Min(this.Parent.W, this.X + (int)((DateTime.UtcNow - this.lastUpdateTime).TotalMilliseconds * this.speedMultiplier));
                if (this.X == this.Parent.W)
                {
                    this.IsVisible = false;
                }
            }
            else this.IsVisible = this.IsShown;

            base.Update();
        }
    }
}
