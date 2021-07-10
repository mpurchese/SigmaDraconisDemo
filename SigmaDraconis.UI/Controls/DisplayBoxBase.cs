namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public abstract class DisplayBoxBase : UIElementBase
    {
        private Texture2D pixelTexture;

        private Color backgroundColour = new Color(0, 0, 0, 64);
        private Color borderColour1 = new Color(92, 92, 92, 255);
        private Color borderColour2 = new Color(64, 64, 64, 255);

        private readonly int unscaledWidth;

        public DisplayBoxBase(IUIElement parent, int x, int y, int unscaledWidth)
            : base(parent, x, y, Scale(unscaledWidth - 2) + 2, Scale(16) + 3)
        {
            this.unscaledWidth = unscaledWidth;
        }

        public override void LoadContent()
        {
            this.pixelTexture = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color = new Color[1] { Color.White };
            this.pixelTexture.SetData(color);

            base.LoadContent();
        }

        public override void ApplyScale()
        {
            this.W = Scale(this.unscaledWidth - 2) + 2;
            this.H = Scale(16) + 3;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void DrawContent()
        {
            if (this.pixelTexture != null)
            {
                this.spriteBatch.Begin();

                // Background
                var rDest = new Rectangle(this.X + (Parent?.RenderX ?? 0), this.Y + (Parent?.RenderY ?? 0), this.W, this.H);
                spriteBatch.Draw(this.pixelTexture, rDest, this.backgroundColour);

                // Borders
                spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Y, rDest.Width, 1), this.borderColour1);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Y, 1, rDest.Height), this.borderColour1);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Bottom - 1, rDest.Width, 1), this.borderColour2);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.Right - 1, rDest.Y, 1, rDest.Height), this.borderColour2);

                this.DrawContentInner();

                this.spriteBatch.End();
            }

            this.IsContentChangedSinceDraw = false;
        }

        protected virtual void DrawContentInner()
        {
        }
    }
}
