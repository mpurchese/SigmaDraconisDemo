namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Language;

    public class NetworkWaterDisplay : UIElementBase
    {
        private readonly TextLabel currentLevelLabel;
        private readonly TextLabel waterUseLabel;
        private readonly TextLabel waterGenLabel;
        private Texture2D colourTexture;

        private string waterGenFormat;
        private string waterUseFormat;
        private string waterLevelFormat;

        private decimal currentWater;
        private decimal maxWater;
        private float waterGenTotal;
        private float waterUseTotal;

        public NetworkWaterDisplay(IUIElement parent, int x, int y)
            : base(parent, x, y, Scale(240) + 2, Scale(12) + 3)
        {
            var colour = UIColour.WaterDisplay;

            this.waterGenFormat = GetString(StringsForWaterDisplay.WaterGenFormat);
            this.waterUseFormat = GetString(StringsForWaterDisplay.WaterUseFormat);
            this.waterLevelFormat = GetString(StringsForWaterDisplay.WaterLevelFormat);

            this.currentLevelLabel = new TextLabel(this, Scale(60), 0, this.W - Scale(120), this.H, "", colour, TextAlignment.MiddleCentre, true);
            this.AddChild(this.currentLevelLabel);

            this.waterUseLabel = new TextLabel(this, 0, 1, Scale(56), this.H, string.Format(this.waterUseFormat, 0f), colour, TextAlignment.MiddleRight, true);
            this.AddChild(this.waterUseLabel);

            this.waterGenLabel = new TextLabel(this, this.W - Scale(56), 1, Scale(56), this.H, string.Format(this.waterGenFormat, 0f), colour, TextAlignment.MiddleLeft, true);
            this.AddChild(this.waterGenLabel);
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\WaterBar");
            this.colourTexture = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\WaterBarColour");
            base.LoadContent();
        }

        public void SetValues(decimal currentWater, decimal maxWater, float waterGenTotal, float waterUseTotal)
        {
            if (currentWater == this.currentWater && maxWater == this.maxWater && waterGenTotal == this.waterGenTotal && waterUseTotal == this.waterUseTotal) return;

            this.currentWater = currentWater;
            this.maxWater = maxWater;
            this.waterGenTotal = waterGenTotal;
            this.waterUseTotal = waterUseTotal;

            this.waterGenLabel.Text = string.Format(this.waterGenFormat, waterGenTotal);
            this.waterUseLabel.Text = string.Format(this.waterUseFormat, waterUseTotal);
            this.currentLevelLabel.Text = string.Format(this.waterLevelFormat, this.currentWater, this.maxWater);

            this.IsContentChangedSinceDraw = true;
        }

        public override void ApplyScale()
        {
            this.W = Scale(240) + 2;
            this.H = Scale(12) + 3;
            this.IsContentChangedSinceDraw = true;
        }

        public override void ApplyLayout()
        {
            this.currentLevelLabel.X = Scale(60);
            this.currentLevelLabel.W = this.W - Scale(120);
            this.currentLevelLabel.H = this.H;
            this.waterUseLabel.W = Scale(56);
            this.waterUseLabel.H = this.H;
            this.waterGenLabel.X = this.W - Scale(56);
            this.waterGenLabel.W = Scale(56);
            this.waterGenLabel.H = this.H;
            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void DrawContent()
        {
            if (this.texture != null)
            {
                var ty = 0;
                if (UIStatics.Scale == 150) ty = 28;
                else if (UIStatics.Scale == 100) ty = 50;

                var rDest = new Rectangle(this.X + (Parent?.RenderX ?? 0) + Scale(60), this.Y + (Parent?.RenderY ?? 0), this.W - Scale(120), this.H);
                var rSource = new Rectangle(0, ty, this.W - Scale(120), this.H);
                this.spriteBatch.Begin();
                this.spriteBatch.Draw(this.texture, rDest, rSource, Color.White);

                if (this.currentWater > 0)
                {
                    var fraction = this.currentWater < this.maxWater ? this.currentWater / this.maxWater : 1M;
                    var width = (int)(fraction * Scale(100));
                    rDest = new Rectangle(this.X + (Parent?.RenderX ?? 0) + Scale(70) + 1, this.Y + (Parent?.RenderY ?? 0) + 1, width, Scale(12) + 1);
                    rSource = new Rectangle(0, 0, (int)(fraction * 198), 25);
                    this.spriteBatch.Draw(this.colourTexture, rDest, rSource, Color.White);
                }

                this.spriteBatch.End();
            }

            this.IsContentChangedSinceDraw = false;
        }

        protected override void HandleLanguageChange()
        {
            this.waterGenFormat = GetString(StringsForWaterDisplay.WaterGenFormat);
            this.waterUseFormat = GetString(StringsForWaterDisplay.WaterUseFormat);
            this.waterLevelFormat = GetString(StringsForWaterDisplay.WaterLevelFormat);

            base.HandleLanguageChange();
        }

        protected static string GetString(StringsForWaterDisplay key)
        {
            return LanguageManager.Get<StringsForWaterDisplay>(key);
        }
    }
}
