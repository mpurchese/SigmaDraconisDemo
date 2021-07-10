namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.Shared;
    using Draconis.UI;

    public class SpeedDisplay : DisplayBoxBase
    {
        private readonly TextLabel label;

        private int speed = 0;

        public int Speed
        {
            get => this.speed;
            set 
            {
                if (this.speed != value)
                { 
                    this.speed = value;
                    this.label.Text = $"{value}%";
                    this.label.Colour = GetLabelColour(value);
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public SpeedDisplay(IUIElement parent, int x, int y) : base(parent, x, y, 52)
        {
            this.label = new TextLabel(this, Scale(22), 0, Scale(28), this.H, "0%", UIColour.DefaultText, TextAlignment.MiddleCentre, true);
            this.AddChild(this.label);
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\SpeedDial");
            base.LoadContent();
        }

        public override void ApplyLayout()
        {
            this.label.X = Scale(22);
            this.label.W = Scale(28);
            this.label.H = this.H;
            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void DrawContentInner()
        {
            if (this.texture != null)
            {
                var ty = 0;
                if (UIStatics.Scale == 150) ty = 28;
                else if (UIStatics.Scale == 100) ty = 49;

                var tx = Scale(20 * Mathf.Clamp(this.speed / 5, 0, 20));

                var rDest = new Rectangle(this.X + (Parent?.RenderX ?? 0) + 1, this.Y + (Parent?.RenderY ?? 0) + Scale(2), Scale(20), Scale(14));
                var rSource = new Rectangle(tx, ty, Scale(20), Scale(14));
                this.spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
            }

            this.IsContentChangedSinceDraw = false;
        }

        private static Color GetLabelColour(int speed)
        {
            if (speed > 66) return UIColour.GreenText;
            if (speed > 33) return UIColour.YellowText;
            if (speed > 0) return UIColour.OrangeText;
            return UIColour.RedText;
        }
    }
}
