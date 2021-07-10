namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class StatusBarBox : UIElementBase
    {
        private Color backgroundColour = new Color(0, 0, 0, 64);
        private Color topEdgeColour = new Color(96, 96, 96);
        private Color bottomEdgeColour = new Color(64, 64, 64);

        public StatusBarBox(IUIElement parent, int x, int y, int w, int h)
            : base(parent, x, y, w, h)
        {
        }

        public override void LoadContent()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.texture.SetData(new Color[1] { Color.White });
            base.LoadContent();
        }

        protected override void DrawContent()
        {
            if (this.texture == null) return;

            var r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);

            this.spriteBatch.Begin();

            // Borders
            this.spriteBatch.Draw(this.texture, r, this.backgroundColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, r.Width, 1), this.topEdgeColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, 1, r.Height), this.topEdgeColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), this.bottomEdgeColour);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), this.bottomEdgeColour);

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
