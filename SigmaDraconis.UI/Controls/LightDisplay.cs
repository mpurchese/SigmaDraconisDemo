namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class LightDisplay : DisplayBoxBase
    {
        private readonly TextLabel label;
        private int percent = 0;

        public void SetValue(int value, Color colour)
        {
            if (this.percent != value || colour != this.label.Colour)
            { 
                this.percent = value;
                this.label.Text = $"{value}%";
                this.label.Colour = colour;
                this.IsContentChangedSinceDraw = true;
            }
        }

        public LightDisplay(IUIElement parent, int x, int y) : base(parent, x, y, 52)
        {
            this.label = new TextLabel(this, Scale(18), 0, Scale(30), this.H, "--%", UIColour.DefaultText, TextAlignment.MiddleCentre, true);
            this.AddChild(this.label);
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\LightSmall");
            base.LoadContent();
        }

        public override void ApplyLayout()
        {
            this.label.X = Scale(18);
            this.label.W = Scale(30);
            this.label.H = this.H;
            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void DrawContentInner()
        {
            if (this.texture != null)
            {
                var tx = 0;
                if (UIStatics.Scale == 150) tx = 32;
                else if (UIStatics.Scale == 100) tx = 56;

                var rDest = new Rectangle(this.X + (Parent?.RenderX ?? 0) + 1, this.Y + (Parent?.RenderY ?? 0) + 1, Scale(16), Scale(16));
                var rSource = new Rectangle(tx, 0, Scale(16), Scale(16));
                this.spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
            }

            this.IsContentChangedSinceDraw = false;
        }
    }
}
