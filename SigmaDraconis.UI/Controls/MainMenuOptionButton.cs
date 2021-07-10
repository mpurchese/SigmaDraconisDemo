namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Language;

    public class MainMenuOptionButton : UIElementBase
    {
        private readonly double relativeX;
        private readonly double relativeY;
        private double scaleX;
        private double scaleY;
        private double xAdjustment;
        private readonly string textureName;
        private Texture2D notMouseOverTexture;
        private Texture2D mouseOverTexture;
        private Effect effect;

        public MainMenuOptionButton(IUIElement parent, double relativeX, double relativeY, string textureName)
            : base(parent, 0, 0, 0, 0)
        {
            this.relativeX = relativeX;
            this.relativeY = relativeY;
            this.scaleX = 1;
            this.scaleY = 1;
            this.textureName = textureName;
            this.IsInteractive = true;
        }

        public void SetScale(double x, double y, double xAdjustment)
        {
            this.scaleX = x;
            this.scaleY = y;
            this.xAdjustment = xAdjustment;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            this.effect = UIStatics.Content.Load<Effect>("Effects\\SimpleEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(this.texture);

            this.notMouseOverTexture = UIStatics.Content.Load<Texture2D>($"Textures\\Menu\\{LanguageManager.CurrentLanguage}\\{textureName}");
            this.mouseOverTexture = UIStatics.Content.Load<Texture2D>($"Textures\\Menu\\{LanguageManager.CurrentLanguage}\\{textureName}Highlighted");
            this.texture = this.notMouseOverTexture;
        }

        public override void Update()
        {
            if (!this.IsVisible) return;

            this.W = (int)(this.texture.Width * this.scaleX);
            this.H = (int)(this.texture.Height * this.scaleY);
            this.X = (int)((this.Parent.W * this.relativeX) - (this.W * 0.5) + this.xAdjustment);
            this.Y = (int)((this.Parent.H * this.relativeY) - (this.H * 0.5));

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.notMouseOverTexture = UIStatics.Content.Load<Texture2D>($"Textures\\Menu\\{LanguageManager.CurrentLanguage}\\{textureName}");
            this.mouseOverTexture = UIStatics.Content.Load<Texture2D>($"Textures\\Menu\\{LanguageManager.CurrentLanguage}\\{textureName}Highlighted");
            this.texture = this.notMouseOverTexture;
            this.IsContentChangedSinceDraw = true;

            this.UpdateHorizontalPosition();
        }

        protected override void OnMouseEnter()
        {
            this.texture = this.mouseOverTexture;
            this.IsContentChangedSinceDraw = true;
            base.OnMouseEnter();
        }

        protected override void OnMouseLeave()
        {
            this.texture = this.notMouseOverTexture;
            this.IsContentChangedSinceDraw = true;
            base.OnMouseLeave();
        }

        protected override void DrawContent()
        {
            var r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            this.spriteBatch.Begin();
            this.spriteBatch.Draw(this.texture, r, Color.White);
            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
