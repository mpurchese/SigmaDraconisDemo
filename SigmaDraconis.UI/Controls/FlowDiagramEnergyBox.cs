namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Language;

    public class FlowDiagramEnergyBox : FlowDiagramBoxBase
    {
        private double amount = 0;

        public double Amount
        {
            get => this.amount;
            set
            {
                if (this.amount != value)
                {
                    this.amount = value;
                    this.UpdateLabel();
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public FlowDiagramEnergyBox(IUIElement parent, double amount) : base(parent, 0, 0, Scale(amount >= 10.0 ? 72 : 70) + 2)
        {
            this.label = new TextLabel(this, Scale(14), 0, Scale(58), this.H, "", UIColour.DefaultText, TextAlignment.MiddleCentre, true);
            this.AddChild(this.label);

            this.amount = amount;
            this.UpdateLabel();
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\FlowDiagramEnergy");
            base.LoadContent();
        }

        public override void ApplyScale()
        {
            this.W = Scale(this.amount >= 10.0 ? 72 : 70) + 2;
            this.H = Scale(16) + 3;
            this.IsContentChangedSinceDraw = true;
        }

        public override void ApplyLayout()
        {
            this.label.X = Scale(14);
            this.label.W = Scale(58);
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
            this.label.Text = $"{this.amount:N1} {LanguageHelper.KWh}";
            this.IsContentChangedSinceDraw = true;
        }

        private Rectangle? GetTextureSourceRect()
        {
            if (UIStatics.Scale == 200) return new Rectangle(0, 46, 28, 35);
            if (UIStatics.Scale == 150) return new Rectangle(0, 19, 21, 27);
            return new Rectangle(0, 0, 14, 19);
        }
    }
}
