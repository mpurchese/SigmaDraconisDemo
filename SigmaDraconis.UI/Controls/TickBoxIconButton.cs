namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class TickBoxIconButton : ButtonBase
    {
        private readonly Icon mainIcon = null;
        private readonly Icon tickBoxIcon = null;

        protected bool isTicked;
        protected int iconFrame;
        protected Color backgroundColour;

        public bool IsTicked
        {
            get
            {
                return this.isTicked;
            }
            set
            {
                if (this.isTicked != value)
                {
                    this.isTicked = value;
                    this.IsContentChangedSinceDraw = true;
                    this.backgroundColour = value ? UIColour.GreenText * .2f : UIColour.RedText * 0.2f;
                }
            }
        }

        public int IconFrame
        {
            get
            {
                return this.iconFrame;
            }
            set
            {
                if (this.iconFrame != value)
                {
                    this.iconFrame = value;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public TickBoxIconButton(IUIElement parent, int x, int y, int w, int h, Icon icon, int iconFrame, bool isRadio = false)
            : base(parent, x, y, w, h)
        {
            this.backgroundColour = UIColour.RedText * .2f;
            this.iconFrame = iconFrame;
            this.mainIcon = icon;
            this.tickBoxIcon = new Icon(isRadio ? UIStatics.RadioBoxIconPath : UIStatics.TickBoxIconPath, 2);
        }

        public override void LoadContent()
        {
            base.LoadContent();
            this.mainIcon.LoadContent();
            this.tickBoxIcon.LoadContent();
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.texture.SetData(new Color[1] { Color.White });
        }

        protected override void DrawContent()
        {
            if (this.texture == null) return;

            var rDest = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var borderColour1 = this.IsSelected ? this.BorderColourSelected : (this.isMouseOver && this.IsEnabled ? this.BorderColourMouseOver : this.BorderColour1);
            var borderColour2 = this.IsSelected ? this.BorderColourSelected : (this.isMouseOver && this.IsEnabled ? this.BorderColourMouseOver : this.BorderColour2);

            this.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            this.spriteBatch.Draw(this.texture, rDest, this.backgroundColour);

            // Icon
            this.mainIcon.Draw(this.spriteBatch, this.RenderX + Scale(18), this.RenderY + (this.H / 2) - Scale(10), this.iconFrame);

            // Borders
            rDest = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            this.spriteBatch.Draw(this.texture, new Rectangle(rDest.X, rDest.Y, rDest.Width, 1), borderColour1);
            this.spriteBatch.Draw(this.texture, new Rectangle(rDest.X, rDest.Y, 1, rDest.Height), borderColour1);
            this.spriteBatch.Draw(this.texture, new Rectangle(rDest.X, rDest.Bottom - 1, rDest.Width, 1), borderColour2);
            this.spriteBatch.Draw(this.texture, new Rectangle(rDest.Right - 1, rDest.Y, 1, rDest.Height), borderColour2);

            // Tick box
            this.tickBoxIcon.Draw(this.spriteBatch, this.RenderX + Scale(2), this.RenderY + Scale(3), this.isTicked ? 1 : 0);

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
