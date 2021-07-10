namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class ProgressBar : UIElementBase
    {
        private double fraction;
        public double Fraction { get { return this.fraction; } set { if (this.fraction != value) { this.fraction = value; this.IsContentChangedSinceDraw = true; } } }

        public Color BackgroundColour { get; set; }
        public Color BarColour { get; set; }

        public ProgressBar(IUIElement parent, int x, int y, int width, int height)
            : base(parent, x, y, width, height)
        {
            this.BackgroundColour = Color.Black;
            this.BarColour = Color.Yellow;
        }

        public override void LoadContent()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color = new Color[1] { Color.White };
            this.texture.SetData(color);
            base.LoadContent();
        }

        protected override void DrawContent()
        {
            Rectangle r1 = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            Rectangle r2 = new Rectangle(this.RenderX, this.RenderY, (int)(this.W * this.Fraction), this.H);

            spriteBatch.Begin();
            spriteBatch.Draw(texture, r1, this.BackgroundColour);
            spriteBatch.Draw(texture, r2, this.BarColour);
            spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
