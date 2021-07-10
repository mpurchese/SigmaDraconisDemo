namespace Draconis.UI
{
    using Shared;
    using Microsoft.Xna.Framework;
    using System.Linq;

    public class TextLabel : UIElementBase
    {
        private string text;
        private Color colour;
        private bool hasShadow;
        private TextAlignment textAlign = TextAlignment.TopLeft;
        private int wordSpacing = 7;

        // Used to monitor for changes that will need a renderer update
        private int renderX;
        private int renderY;

        public string Text
        {
            get { return this.text; }
            set { if (value != this.text) { this.text = value; this.IsContentChangedSinceDraw = true; } }
        }

        public bool HasShadow
        {
            get { return this.hasShadow; }
            set { if (value != this.hasShadow) { this.hasShadow = value; this.IsContentChangedSinceDraw = true; } }
        }

        public Color Colour
        {
            get { return this.colour; }
            set { if (value != this.colour) { this.colour = value; this.IsContentChangedSinceDraw = true; } }
        }

        public TextAlignment TextAlign
        {
            get { return this.textAlign; }
            set { if (value != this.textAlign) { this.textAlign = value; this.IsContentChangedSinceDraw = true; } }
        }

        public int WordSpacing
        {
            get { return this.wordSpacing; }
            set { if (value != this.wordSpacing) { this.wordSpacing = value; this.IsContentChangedSinceDraw = true; } }
        }

        public TextRenderer TextRenderer { get; private set; }

        public TextLabel(TextRenderer textRenderer, IUIElement parent, int x, int y, int width, int height, string text, Color colour, TextAlignment alignment = TextAlignment.TopCentre, bool hasShadow = false) : base(parent, x, y, width, height)
        {
            this.colour = colour;
            this.textAlign = alignment;
            this.Text = text;
            this.hasShadow = hasShadow;
            this.TextRenderer = textRenderer;
        }

        public TextLabel(IUIElement parent, int x, int y, string text = "") : base(parent, x, y, 0, 0)
        {
            this.colour = UIStatics.DefaultTextColour;
            this.Text = text;
            this.TextRenderer = UIStatics.TextRenderer;
        }

        public TextLabel(IUIElement parent, int x, int y, string text, Color colour, bool hasShadow = false) : base(parent, x, y, 0, 0)
        {
            this.colour = colour;
            this.Text = text;
            this.hasShadow = hasShadow;
            this.TextRenderer = UIStatics.TextRenderer;
        }

        public TextLabel(IUIElement parent, int x, int y, int width, int height, string text, Color colour, TextAlignment alignment = TextAlignment.TopCentre, bool hasShadow = false) : base(parent, x, y, width, height)
        {
            this.colour = colour;
            this.textAlign = alignment;
            this.Text = text;
            this.hasShadow = hasShadow;
            this.TextRenderer = UIStatics.TextRenderer;
        }

        protected override void DrawContent()
        {
            if (this.IsVisible && (this.IsContentChangedSinceDraw || this.renderX != this.RenderX || this.renderY != this.RenderY)) this.UpdateRenderer();
            if (this.text == "") return;
            this.TextRenderer.Draw(this.Id);
            base.DrawContent();
        }

        private void UpdateRenderer()
        {
            this.renderX = this.RenderX;
            this.renderY = this.RenderY;
            var x = this.renderX;
            var y = this.renderY;

            var textWidth = text != null ? text.Length * this.TextRenderer.LetterSpace : 0;
            if (this.wordSpacing != 7 && !string.IsNullOrEmpty(this.text)) textWidth += this.text.Count(c => c == ' ') * (this.wordSpacing - 7);

            if ((this.textAlign == TextAlignment.TopCentre || this.textAlign == TextAlignment.MiddleCentre || this.textAlign == TextAlignment.BottomCentre) && this.W > 0)
            {
                x += (this.W - textWidth) / 2;
            }
            else if ((this.textAlign == TextAlignment.TopRight || this.textAlign == TextAlignment.MiddleRight || this.textAlign == TextAlignment.BottomRight) && this.W > 0)
            {
                x += this.W - textWidth;
            }

            if ((this.textAlign == TextAlignment.MiddleLeft || this.textAlign == TextAlignment.MiddleCentre || this.textAlign == TextAlignment.MiddleRight) && this.W > 0)
            {
                y += (this.H - this.TextRenderer.LetterHeight) / 2;
            }
            else if ((this.textAlign == TextAlignment.BottomLeft || this.textAlign == TextAlignment.BottomCentre || this.textAlign == TextAlignment.BottomRight) && this.W > 0)
            {
                y += this.H - this.TextRenderer.LetterHeight;
            }

            this.TextRenderer.SetText(this.Id, this.text, new Vector2i(x, y), this.colour, null, this.hasShadow, this.wordSpacing);
            this.IsContentChangedSinceDraw = false;
        }
    }
}
