namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public abstract class FlowDiagramBoxBase : UIElementBase
    {
        protected TextLabel label;
        private Texture2D pixelTexture = null;

        protected bool isEnabled = true;

        public bool IsEnabled
        {
            get => this.isEnabled;
            set
            {
                if (this.label != null && value != this.isEnabled)
                {
                    this.isEnabled = value;
                    this.label.Colour = value ? UIColour.DefaultText : UIColour.GrayText;
                }
            }
        }

        public FlowDiagramBoxBase(IUIElement parent, int x, int y, int w) : base(parent, x, y, w, Scale(16) + 3)
        {
        }

        public override void LoadContent()
        {
            this.pixelTexture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.pixelTexture.SetData(new Color[1] { Color.White });
            base.LoadContent();
        }

        protected override void DrawContent()
        {
            if (!this.isContentLoaded) return;

            var rDest = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);

            this.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            // Background
            this.spriteBatch.Draw(this.pixelTexture, rDest, UIColour.ButtonBackground);

            // Icon
            this.DrawIcon();

            // Borders
            rDest = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            this.spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Y, rDest.Width, 1), UIColour.BorderDark);
            this.spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Y, 1, rDest.Height), UIColour.BorderDark);
            this.spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.X, rDest.Bottom - 1, rDest.Width, 1), UIColour.BorderDark);
            this.spriteBatch.Draw(this.pixelTexture, new Rectangle(rDest.Right - 1, rDest.Y, 1, rDest.Height), UIColour.BorderDark);

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        protected virtual void DrawIcon()
        {
        }
    }
}
