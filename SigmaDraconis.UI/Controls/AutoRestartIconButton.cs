namespace SigmaDraconis.UI
{
    using System;
    using Draconis.UI;
    using Microsoft.Xna.Framework;

    public class AutoRestartIconButton : IconButton
    {
        private bool isOn;

        public bool IsOn
        {
            get { return this.isOn; }
            set { if (this.isOn != value) { this.isOn = value; this.IsContentChangedSinceDraw = true; } }
        }

        public AutoRestartIconButton(IUIElement parent, int x, int y)
            : base(parent, x, y, "Textures\\Icons\\AutoRestart", 1f, true)
        {
        }

        protected override void SetWidthAndHeight()
        {
            this.W = (int)Math.Round(this.texture.Width * UIStatics.Scale * (this.multiscaleTexture ? 2 / 9f : 1f) / 200f);
            this.H = (int)Math.Round(this.texture.Height * UIStatics.Scale * (this.multiscaleTexture ? 1 / 2f : 1f) / 100f);
        }

        protected override Rectangle? GetTextureSourceRect()
        {
            if (UIStatics.Scale == 200) return new Rectangle(this.IsOn ? 140 : 100, 0, 40, 40);
            if (UIStatics.Scale == 150) return new Rectangle(this.IsOn ? 70 : 40, 0, 30, 30);
            return new Rectangle(this.IsOn ? 20 : 0, 0, 20, 20);
        }
    }
}
