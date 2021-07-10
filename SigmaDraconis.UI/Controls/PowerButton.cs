namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class PowerButton : ButtonBase, IPowerButton
    {
        protected Texture2D pixelTexture = null;
        protected Texture2D onOffTexture = null;

        private bool isOn;

        public bool IsOn
        {
            get
            {
                return this.isOn;
            }
            set
            {
                if (this.isOn != value)
                {
                    this.isOn = value;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public PowerButton(IUIElement parent, int x, int y)
            : base(parent, x, y, Scale(22), Scale(18))
        {
        }

        public override void LoadContent()
        {
            base.LoadContent();

            this.onOffTexture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\OnOff");

            this.pixelTexture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.pixelTexture.SetData(new Color[1] { Color.White });
        }

        protected virtual Rectangle? GetTextureSourceRect()
        {
            var x = this.isOn ? 0 : 36;
            if (UIStatics.Scale == 200) return new Rectangle(x, 42, 36, 36);
            if (UIStatics.Scale == 150) return new Rectangle(x, 18, 24, 24);
            return new Rectangle(x, 0, 18, 18);
        }

        protected override void DrawContent()
        {
            if (!this.isContentLoaded) return;

            var rDest = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var borderColour1 = this.IsSelected ? this.BorderColourSelected : (this.isMouseOver && this.IsEnabled ? this.BorderColourMouseOver : this.BorderColour1);
            var borderColour2 = this.IsSelected ? this.BorderColourSelected : (this.isMouseOver && this.IsEnabled ? this.BorderColourMouseOver : this.BorderColour2);
            this.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            // Background
            this.spriteBatch.Draw(this.pixelTexture, rDest, UIColour.ButtonBackground);

            // On/off icon
            var rSource = this.GetTextureSourceRect();
            rDest = new Rectangle(this.RenderX + Scale(2), this.RenderY, Scale(18), Scale(18));
            spriteBatch.Draw(this.onOffTexture, rDest, rSource, Color.White);

            // Borders
            rDest = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            this.spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Y, rDest.Width, 1), borderColour1);
            this.spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Y, 1, rDest.Height), borderColour1);
            this.spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Bottom - 1, rDest.Width, 1), borderColour2);
            this.spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.Right - 1, rDest.Y, 1, rDest.Height), borderColour2);

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
