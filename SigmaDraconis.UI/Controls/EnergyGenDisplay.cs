namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Language;

    public class EnergyGenDisplay : DisplayBoxBase
    {
        private readonly TextLabel label;
        private static string formatString;

        private readonly string iconName;
        private double? energyGen;

        public double? EnergyGen
        {
            get => this.energyGen;
            set 
            {
                if (this.energyGen != value)
                { 
                    this.energyGen = value;
                    this.label.Text = string.Format(formatString, value);
                    this.label.Colour = value > 0 ? UIColour.GreenText : UIColour.RedText;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\" + this.iconName);
            base.LoadContent();
        }

        public EnergyGenDisplay(IUIElement parent, int x, int y, int unscaledWidth, string iconName) : base(parent, x, y, unscaledWidth)
        {
            this.iconName = iconName;
            if (string.IsNullOrEmpty(formatString)) formatString = "+{0:N1} " + LanguageManager.Get<StringsForUnits>(StringsForUnits.kW);

            this.label = new TextLabel(this, Scale(18), 0, this.W - Scale(18), this.H, string.Format(formatString, 0), UIColour.DefaultText, TextAlignment.MiddleCentre, true);
            this.AddChild(this.label);
        }

        public override void ApplyLayout()
        {
            this.label.X = Scale(18);
            this.label.W = this.W - Scale(18);
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
                if (UIStatics.Scale == 150) tx = 13;
                else if (UIStatics.Scale == 200) tx = 32;

                var size = Scale(12) + 1;

                var rDest = new Rectangle(this.X + (Parent?.RenderX ?? 0) + Scale(2) + 1, this.Y + (Parent?.RenderY ?? 0) + Scale(2) + 1, size, size);
                var rSource = new Rectangle(tx, 0, size, size);
                this.spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
            }

            this.IsContentChangedSinceDraw = false;
        }

        protected override void HandleLanguageChange()
        {
            formatString = "+{0:N1} " + LanguageManager.Get<StringsForUnits>(StringsForUnits.kW);
            base.HandleLanguageChange();
        }
    }
}
