namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class FlowDiagramWaterBox : FlowDiagramBoxBase
    {
        private double quantity = 0;

        public double Quantity
        {
            get => this.quantity;
            set
            {
                if (this.quantity != value)
                {
                    this.quantity = value;
                    this.UpdateLabel();
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public FlowDiagramWaterBox(IUIElement parent, double quantity) : base(parent, 0, 0, Scale(60) + 2)
        {
            this.label = new TextLabel(this, Scale(30), 0, Scale(32), this.H, "", UIColour.DefaultText, TextAlignment.MiddleCentre, true);
            this.AddChild(this.label);

            this.quantity = quantity;
            this.UpdateLabel();
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\FlowDiagramWater");
            base.LoadContent();
        }

        public override void ApplyScale()
        {
            this.W = Scale(60) + 2;
            this.H = Scale(16) + 3;
            this.IsContentChangedSinceDraw = true;
        }

        public override void ApplyLayout()
        {
            this.label.X = Scale(30);
            this.label.W = Scale(32);
            this.label.H = this.H;
            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void DrawIcon()
        {
            var rSource = this.GetTextureSourceRect();
            var rDest = new Rectangle(this.RenderX + Scale(2), this.RenderY, rSource.Value.Width, rSource.Value.Height);
            spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
        }

        private void UpdateLabel()
        {
            this.label.IsVisible = true;
            this.label.Text = this.quantity.ToString("N1");
            this.IsContentChangedSinceDraw = true;
        }

        private Rectangle? GetTextureSourceRect()
        {
            if (UIStatics.Scale == 200) return new Rectangle(0, 46, 50, 35);
            if (UIStatics.Scale == 150) return new Rectangle(0, 19, 43, 27);
            return new Rectangle(0, 0, 30, 19);
        }
    }
}
