namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class StatusBarIconButton : IconButton
    {
        protected static Texture2D dotTexture;

        public StatusBarIconButton(IUIElement parent, int x, int y, string texturePath)
            : base(parent, x, y, texturePath, 1f, true)
        {
            this.BorderColour2 = new Color(92, 92, 92);
        }

        protected override void DrawContent()
        {
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, UIStatics.Graphics.Viewport.Width, UIStatics.Graphics.Viewport.Height, 0, 0, 1);
            this.effect.Parameters["xViewProjection"].SetValue(projection);

            var borderColour = this.IsMouseOver && this.IsEnabled ? new Color(128, 128, 128, 255) : this.BorderColour2;
            if (this.IsSelected)
            {
                borderColour = this.BorderColourSelected;
            }
            else if (this.IsHighlighted)
            {
                var c = 127 + (int)(128 * Math.Sin(3.142 * DateTime.UtcNow.Millisecond / 1000.0));
                borderColour = new Color(c, c, 0);
            }

            var r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var rSource = this.GetTextureSourceRect();

            // Icon
            if (this.IsHighlighted) this.effect.CurrentTechnique = this.effect.Techniques["MonoBrightTechnique"];
            else if (this.IsEnabled) this.effect.CurrentTechnique = this.effect.Techniques["SimpleTechnique"];
            else this.effect.CurrentTechnique = this.effect.Techniques["MonoDarkTechnique"];

            spriteBatch.Begin(effect: this.effect);
            spriteBatch.Draw(this.texture, r, rSource, Color.White);
            spriteBatch.End();

            // Borders
            spriteBatch.Begin();
            spriteBatch.Draw(this.pixelTexture, new Rectangle(r.X, r.Y, r.Width, 1), borderColour);
            spriteBatch.Draw(this.pixelTexture, new Rectangle(r.X, r.Y, 1, r.Height), borderColour);
            spriteBatch.Draw(this.pixelTexture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), borderColour);
            spriteBatch.Draw(this.pixelTexture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), borderColour);

            if (this.IsHighlighted)
            {
                // Glow effect
                var borderColour2 = new Color(borderColour.R / 2, borderColour.G / 2, borderColour.B / 2);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(r.X - 1, r.Y - 1, r.Width + 2, 1), borderColour);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(r.X - 1, r.Y - 1, 1, r.Height + 2), borderColour);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(r.X - 1, r.Bottom, r.Width + 2, 1), borderColour);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(r.Right, r.Y - 1, 1, r.Height + 2), borderColour);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(r.X + 1, r.Y + 1, r.Width - 2, 1), borderColour2);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(r.X + 1, r.Y + 1, 1, r.Height - 2), borderColour2);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(r.X + 1, r.Bottom - 2, r.Width - 2, 1), borderColour2);
                spriteBatch.Draw(this.pixelTexture, new Rectangle(r.Right - 2, r.Y + 1, 1, r.Height - 2), borderColour2);
            }

            spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
