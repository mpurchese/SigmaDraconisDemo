namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using Language;

    public class TextLabelAutoScaling : TextLabel
    {
        private readonly bool isLanguageConfigured;
        private readonly object textId;

        public TextLabelAutoScaling(IUIElement parent, int x, int y, string text, Color colour, bool hasShadow = false)
            : base(parent, Scale(x), Scale(y), text, colour, hasShadow)
        {
        }

        public TextLabelAutoScaling(IUIElement parent, int x, int y, int width, int height, string text, Color colour, TextAlignment alignment = TextAlignment.TopCentre, bool hasShadow = false)
            : base(parent, Scale(x), Scale(y), Scale(width), Scale(height), text, colour, alignment, hasShadow)
        {
        }

        public TextLabelAutoScaling(IUIElement parent, int x, int y, object textId, Color colour, bool hasShadow = false)
            : base(parent, Scale(x), Scale(y), LanguageManager.Get(textId.GetType(), textId), colour, hasShadow)
        {
            this.isLanguageConfigured = true;
            this.textId = textId;
        }

        public TextLabelAutoScaling(IUIElement parent, int x, int y, int width, int height, object textId, Color colour, TextAlignment alignment = TextAlignment.TopCentre, bool hasShadow = false)
            : base(parent, Scale(x), Scale(y), Scale(width), Scale(height), LanguageManager.Get(textId.GetType(), textId), colour, alignment, hasShadow)
        {
            this.isLanguageConfigured = true;
            this.textId = textId;
        }

        protected override void HandleLanguageChange()
        {
            if (isLanguageConfigured) this.Text = LanguageManager.Get(this.textId.GetType(), this.textId);
            base.HandleLanguageChange();
        }
    }
}
