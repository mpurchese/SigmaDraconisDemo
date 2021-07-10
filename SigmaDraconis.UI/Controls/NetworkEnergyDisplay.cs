namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.UI;
    using Language;
    using Shared;

    public class NetworkEnergyDisplay : UIElementBase
    {
        protected Effect effect;
        protected TextLabel label;

        protected Energy currentEnergy;
        protected Energy maxEnergy;
        protected Energy energyGen;
        protected Energy energyUse;

        public NetworkEnergyDisplay(IUIElement parent, int x, int y)
            : base(parent, x, y, Scale(170), Scale(12) + 1)
        {
            this.currentEnergy = Energy.FromKwH(8.0);
            this.maxEnergy = Energy.FromKwH(8.0);

            this.label = new TextLabel(this, 0, -1, this.W, this.H, "", UIColour.YellowText, hasShadow: true);
            this.AddChild(this.label);
        }

        public void SetValues(Energy currentEnergy, Energy maxEnergy, Energy energyGen, Energy energyUse)
        {
            if (currentEnergy == this.currentEnergy && maxEnergy == this.maxEnergy && energyGen == this.energyGen && energyUse == this.energyUse) return;

            this.currentEnergy = currentEnergy;
            this.maxEnergy = maxEnergy;
            this.energyGen = energyGen;
            this.energyUse = energyUse;
            this.IsContentChangedSinceDraw = true;
        }

        public void SetValues(Energy currentEnergy)
        {
            if (currentEnergy == this.currentEnergy) return;

            this.currentEnergy = currentEnergy;
            this.IsContentChangedSinceDraw = true;
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Misc\\EnergyBar");

            this.effect = UIStatics.Content.Load<Effect>("Effects\\SimpleEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(this.texture);
            this.effect.CurrentTechnique = this.effect.Techniques["SimpleTechnique"];

            base.LoadContent();
        }

        public override void Update()
        {
            this.label.Text = $"{this.currentEnergy.KWh:N1}/{this.maxEnergy.KWh:N1} {LanguageHelper.KWh}";
            base.Update();
        }

        public override void ApplyScale()
        {
            this.W = Scale(170);
            this.H = Scale(12) + 1;
            this.IsContentChangedSinceDraw = true;
        }

        public override void ApplyLayout()
        {
            this.label.W = this.W;
            this.label.H = this.H;
            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void DrawContent()
        {
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, UIStatics.Graphics.Viewport.Width, UIStatics.Graphics.Viewport.Height, 0, 0, 1);
            this.effect.Parameters["xViewProjection"].SetValue(projection);

            if (this.currentEnergy > 0)
            {
                var ws = 171.0;
                if (UIStatics.Scale == 200) ws = 342.0;
                else if (UIStatics.Scale == 150) ws = 256.0;
                var width = (int)((this.currentEnergy / this.maxEnergy) * ws);
                var rDest = new Rectangle(this.ScreenX, this.ScreenY, width, this.H);
                var rSource = new Rectangle(0, 0, (int)(this.currentEnergy / this.maxEnergy * 171.0), 10);

                this.spriteBatch.Begin(effect: this.effect);
                this.spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
                this.spriteBatch.End();
            }

            this.IsContentChangedSinceDraw = false;
        }
    }
}
