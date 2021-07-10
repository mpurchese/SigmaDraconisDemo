namespace Draconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public class IconButton : ButtonBase, IButton
    {
        private readonly float scale;

        protected Effect effect;
        protected Texture2D pixelTexture;
        protected string texturePath;
        protected bool multiscaleTexture;
        protected Color backgroundColour = new Color(0, 0, 0, 0);

        // Use if we want to make the button one pixel smaller, as we can't scale textures with an odd size
        public bool OnePixelLessX { get; set; }
        public bool OnePixelLessY { get; set; }

        public Color BackgroundColour
        {
            get { return this.backgroundColour; }
            set
            {
                if (value != this.backgroundColour)
                {
                    this.backgroundColour = value;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public IconButton(IUIElement parent, int x, int y, string texturePath, float scale = 1f, bool multiscaleTexture = false) : base(parent, x, y, 0, 0)
        {
            this.scale = scale;
            this.texturePath = texturePath;
            this.multiscaleTexture = multiscaleTexture;
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>(this.texturePath);

            this.effect = UIStatics.Content.Load<Effect>("Effects\\SimpleEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(this.texture);

            this.pixelTexture = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color = new Color[1] { Color.White };
            this.pixelTexture.SetData(color);

            this.SetWidthAndHeight();
            base.LoadContent();
        }

        public void SetTexture(string texturePath)
        {
            this.texturePath = texturePath;
            if (this.isContentLoaded)
            {
                this.texture = UIStatics.Content.Load<Texture2D>(this.texturePath);
                this.effect.Parameters["xTexture"].SetValue(this.texture);
            }
        }

        public override void ApplyScale()
        {
            this.SetWidthAndHeight();
        }

        protected virtual void SetWidthAndHeight()
        {
            this.W = (int)Math.Round(this.texture.Width * UIStatics.Scale * this.scale * (this.multiscaleTexture ? 2 / 9f : 1f) / 100f) - (this.OnePixelLessX ? 1 : 0);
            this.H = (int)Math.Round(this.texture.Height * UIStatics.Scale * this.scale * (this.multiscaleTexture ? 1 / 2f : 1f) / 100f) - (this.OnePixelLessY ? 1 : 0);
        }

        protected virtual Rectangle? GetTextureSourceRect()
        {
            if (!this.multiscaleTexture) return null;
            var w = this.W + (this.OnePixelLessX ? 1 : 0);
            if (UIStatics.Scale == 200) return new Rectangle(w * 5 / 4, 0, this.W, this.H);
            if (UIStatics.Scale == 150) return new Rectangle(w * 2 / 3, 0, this.W, this.H);
            return new Rectangle(0, 0, this.W, this.H);
        }

        protected override void DrawContent()
        {
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, UIStatics.Graphics.Viewport.Width, UIStatics.Graphics.Viewport.Height, 0, 0, 1);
            this.effect.Parameters["xViewProjection"].SetValue(projection);

            var borderColour1 = this.BorderColour1;
            var borderColour2 = this.BorderColour2;
            if (this.IsSelected)
            {
                borderColour1 = this.BorderColourSelected;
                borderColour2 = this.BorderColourSelected;
            }
            else if (this.IsHighlighted)
            {
                var c = 127 + (int)(128 * Math.Sin(3.142 * DateTime.UtcNow.Millisecond / 1000.0));
                borderColour1 = new Color(c, c, 0);
                borderColour2 = new Color(c, c, 0);
            }
            else if (this.IsMouseOver && this.IsInteractive)
            {
                borderColour1 = this.BorderColourMouseOver;
                borderColour2 = this.BorderColourMouseOver;
            }

            var rDest = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var rSource = this.GetTextureSourceRect();

            // Background
            if (this.backgroundColour.A > 0)
            {
                this.spriteBatch.Begin();
                this.spriteBatch.Draw(this.pixelTexture, rDest, this.backgroundColour);
                this.spriteBatch.End();
            }

            // Icon
            this.effect.CurrentTechnique = this.effect.Techniques[this.IsEnabled ? "SimpleTechnique" : "MonoTechnique"];
            spriteBatch.Begin(effect: this.effect);
            spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
            spriteBatch.End();

            // Borders
            spriteBatch.Begin();
            spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Y, rDest.Width, 1), borderColour1);
            spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Y, 1, rDest.Height), borderColour1);
            spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Bottom - 1, rDest.Width, 1), borderColour2);
            spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.Right - 1, rDest.Y, 1, rDest.Height), borderColour2);

            if (this.IsHighlighted)
            {
                // Glow effect
                var borderColour3 = new Color(borderColour1.R / 2, borderColour1.G / 2, borderColour1.B / 2);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X - 1, rDest.Y - 1, rDest.Width + 2, 1), borderColour3);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X - 1, rDest.Y - 1, 1, rDest.Height + 2), borderColour3);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X - 1, rDest.Bottom, rDest.Width + 2, 1), borderColour3);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.Right, rDest.Y - 1, 1, rDest.Height + 2), borderColour3);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X + 1, rDest.Y + 1, rDest.Width - 2, 1), borderColour3);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X + 1, rDest.Y + 1, 1, rDest.Height - 2), borderColour3);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X + 1, rDest.Bottom - 2, rDest.Width - 2, 1), borderColour3);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.Right - 2, rDest.Y + 1, 1, rDest.Height - 2), borderColour3);
            }

            spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.pixelTexture != null) this.pixelTexture.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
