namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Draconis.UI;

    public class PickerIconButton : IconButton
    {
        public PickerIconButton(IUIElement parent, int x, int y, string texturePath)
            : base(parent, x, y, texturePath, 1f, true)
        {
            this.BorderColour2 = new Color(92, 92, 92);
        }

        protected override void DrawContent()
        {
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, UIStatics.Graphics.Viewport.Width, UIStatics.Graphics.Viewport.Height, 0, 0, 1);
            this.effect.Parameters["xViewProjection"].SetValue(projection);

            var borderColour = this.BorderColour2;
            if (this.IsSelected)
            {
                borderColour = this.BorderColourSelected;
            }
            else if (this.IsHighlighted)
            {
                var c = 127 + (int)(128 * Math.Sin(3.142 * DateTime.UtcNow.Millisecond / 1000.0));
                borderColour = new Color(c, c, 0);
            }
            else if (this.IsMouseOver && this.IsEnabled)
            {
                borderColour = new Color(128, 128, 128, 255);
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
            this.effect.CurrentTechnique = this.effect.Techniques[this.IsEnabled ? "SimpleTechnique" : "MonoDarkTechnique"];
            spriteBatch.Begin(effect: this.effect);
            spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
            spriteBatch.End();

            // Borders
            spriteBatch.Begin();
            spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Y, rDest.Width, 1), borderColour);
            spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Y, 1, rDest.Height), borderColour);
            spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Bottom - 1, rDest.Width, 1), borderColour);
            spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.Right - 1, rDest.Y, 1, rDest.Height), borderColour);
            spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
