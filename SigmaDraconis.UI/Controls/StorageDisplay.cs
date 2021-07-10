namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;
    using Language;

    public class StorageDisplay : DisplayBoxBase
    {
        private readonly TextLabel currentLevelLabel;

        private readonly StringsForThingPanels formatStringId;
        private readonly string colourTexturePath;

        private decimal currentCount;
        private decimal capacity;

        public StorageDisplay(IUIElement parent, int x, int y, int unscaledWidth
            , StringsForThingPanels formatStringId = StringsForThingPanels.ResourcesStored
            , string colourTexturePath = "Textures\\Misc\\StorageBarColour") 
            : base(parent, x, y, unscaledWidth)
        {
            this.formatStringId = formatStringId;
            this.colourTexturePath = colourTexturePath;

            this.currentLevelLabel = new TextLabel(this, 0, 0, this.W, this.H, "", UIColour.DefaultText, TextAlignment.MiddleCentre, true);
            this.AddChild(this.currentLevelLabel);
        }

        public void SetCounts(decimal currentCount, decimal capacity)
        {
            if (currentCount == this.currentCount && capacity == this.capacity) return;

            this.currentCount = currentCount;
            this.capacity = capacity;
            this.currentLevelLabel.Text = LanguageManager.Get<StringsForThingPanels>(this.formatStringId, this.currentCount, this.capacity);
            this.IsContentChangedSinceDraw = true;
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>(this.colourTexturePath);
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
            if (this.texture == null || this.currentCount == 0) return;

            var width = (int)((this.currentCount / this.capacity) * (this.W - 2));
            var rDest = new Rectangle(this.X + (Parent?.RenderX ?? 0) + 1, this.Y + (Parent?.RenderY ?? 0) + 1 + Scale(2), width, Scale(12) + 1);
            var rSource = new Rectangle(0, 0, (int)(this.currentCount / this.capacity * 198), 25);
            this.spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
        }
    }
}
