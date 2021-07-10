namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class IconButtonWithOverlay : IconButton
    {
        protected string texturePath2;
        protected Texture2D texture2;
        protected bool isOverlayVisible;

        public bool IsOverlayVisible {
            get { return this.isOverlayVisible; }
            set
            {
                if (this.isOverlayVisible != value)
                {
                    this.isOverlayVisible = value;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public IconButtonWithOverlay(IUIElement parent, int x, int y, string texturePath, string texturePath2)
            : base(parent, x, y, texturePath, 1f, true)
        {
            this.texturePath2 = texturePath2;
        }

        public void SetTextures(string texturePath, string texturePath2)
        {
            this.texturePath = texturePath;
            this.texturePath2 = texturePath2;
            if (this.isContentLoaded)
            {
                this.texture = UIStatics.Content.Load<Texture2D>(this.texturePath);
                this.texture2 = UIStatics.Content.Load<Texture2D>(this.texturePath2);
                this.effect.Parameters["xTexture"].SetValue(this.texture);
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();

            if (this.texture2 == null) this.texture2 = UIStatics.Content.Load<Texture2D>(this.texturePath2);
        }

        protected override void DrawContent()
        {
            base.DrawContent();
            if (this.IsVisible && this.isOverlayVisible && this.texture != null && this.texture2 != null)
            {
                var rSource = this.GetTextureSourceRect();
                Rectangle targetRect = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
                spriteBatch.Begin(effect: this.effect);
                spriteBatch.Draw(this.texture2, targetRect, rSource, Color.White);
                spriteBatch.End();
            }
        }
    }
}
