namespace Draconis.UI.Internal
{
    using Microsoft.Xna.Framework;

    internal class ScrollbarIconButton : IconButton
    {
        public ScrollbarIconButton(IUIElement parent, int x, int y, string texturePath)
            : base(parent, x, y, texturePath, 1f, true)
        {
            this.BorderColour2 = new Color(92, 92, 92);
        }

        protected override void DrawContent()
        {
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, UIStatics.Graphics.Viewport.Width, UIStatics.Graphics.Viewport.Height, 0, 0, 1);
            this.effect.Parameters["xViewProjection"].SetValue(projection);

            var borderColour1 = this.BorderColour1;
            var borderColour2 = this.BorderColour2;
            if (this.IsMouseOver && this.IsEnabled)
            {
                borderColour1 = this.BorderColourMouseOver;
                borderColour2 = this.BorderColourMouseOver;
            }

            var rDest = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var rSource = this.GetTextureSourceRect();

            // Icon
            this.effect.CurrentTechnique = this.effect.Techniques[this.IsEnabled ? "SimpleTechnique" : "MonoDarkTechnique"];
            spriteBatch.Begin(effect: this.effect);
            spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
            spriteBatch.End();

            // Borders
            spriteBatch.Begin();
            spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Y, rDest.Width, 1), borderColour1);
            spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Y, 1, rDest.Height), borderColour1);
            spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Bottom - 1, rDest.Width, 1), borderColour2);
            spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.Right - 1, rDest.Y, 1, rDest.Height), borderColour2);

            spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
