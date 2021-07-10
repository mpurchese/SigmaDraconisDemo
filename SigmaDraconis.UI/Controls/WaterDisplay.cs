namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Language;

    public class WaterDisplay : DisplayBoxBase
    {
        private readonly TextLabel currentLevelLabel;

        private string waterLevelFormat;

        private decimal currentWater;
        private decimal maxWater;

        public WaterDisplay(IUIElement parent, int x, int y, int unscaledWidth) : base(parent, x, y, unscaledWidth)
        {
            this.waterLevelFormat = GetString(StringsForWaterDisplay.WaterLevelFormat);

            this.currentLevelLabel = new TextLabel(this, 0, 0, this.W, this.H, "", UIColour.WaterDisplay, TextAlignment.MiddleCentre, true);
            this.AddChild(this.currentLevelLabel);
        }

        public void SetWater(decimal currentWater, decimal maxWater)
        {
            if (currentWater == this.currentWater && maxWater == this.maxWater) return;

            this.currentWater = currentWater;
            this.maxWater = maxWater;
            this.currentLevelLabel.Text = string.Format(this.waterLevelFormat, this.currentWater, this.maxWater);
            this.IsContentChangedSinceDraw = true;
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\WaterBarColour");
            base.LoadContent();
        }

        public override void ApplyLayout()
        {
            this.currentLevelLabel.W = this.W;
            this.currentLevelLabel.H = this.H;
            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void DrawContentInner()
        {
            if (this.texture == null || this.currentWater == 0) return;

            var width = (int)((this.currentWater / this.maxWater) * (this.W - 2));
            var rDest = new Rectangle(this.X + (Parent?.RenderX ?? 0) + 1, this.Y + (Parent?.RenderY ?? 0) + 1 + Scale(2), width, Scale(12) + 1);
            var rSource = new Rectangle(0, 0, (int)(this.currentWater / this.maxWater * 198), 25);
            this.spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
        }

        protected override void HandleLanguageChange()
        {
            this.waterLevelFormat = GetString(StringsForWaterDisplay.WaterLevelFormat);
            base.HandleLanguageChange();
        }

        protected static string GetString(StringsForWaterDisplay key)
        {
            return LanguageManager.Get<StringsForWaterDisplay>(key);
        }
    }
}
