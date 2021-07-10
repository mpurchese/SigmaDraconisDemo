namespace Draconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public class TextButton : ButtonBase, IKeyboardHandler
    {
        protected TextLabel textLabel;
        protected Color textColour;
        protected Color textDisabledColour = Color.Gray;

        public Color BackgroundColour { get; set; } = new Color(0, 0, 0, 100);
        public Color BackgroundColourHighlighted { get; set; } = new Color(0, 42, 64, 212);
        public override Color BorderColourSelected { get; set; } = new Color(0, 96, 192, 192);

        public string Text
        {
            get { return this.textLabel.Text; }
            set { this.textLabel.Text = value; }
        }

        public override int W
        {
            get
            {
                return base.W;
            }
            set
            {
                base.W = value;
                if (!this.isApplyingScale) this.textLabel.W = value;
            }
        }

        public override bool IsEnabled
        {
            get
            {
                return this.isEnabled;
            }

            set
            {
                if (this.isEnabled != value)
                {
                    this.isEnabled = value;
                    this.IsInteractive = this.isEnabled;
                    this.IsSelected = this.IsSelected && value;
                    this.textLabel.Colour = this.isEnabled ? this.textColour : this.textDisabledColour;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public TextAlignment TextAlign
        {
            get { return this.textLabel.TextAlign; }
            set { this.textLabel.TextAlign = value; }
        }

        public Color TextColour
        {
            get
            {
                return this.textColour;
            }
            set
            {
                this.textColour = value;
                this.textLabel.Colour = this.isEnabled ? value : textDisabledColour;
            }
        }

        public Color TextDisabledColour
        {
            get
            {
                return this.textDisabledColour;
            }

            set
            {
                this.textDisabledColour = value;
                this.textLabel.Colour = this.isEnabled ? this.textColour : value;
            }
        }

        public int WordSpacing
        {
            get { return this.textLabel.WordSpacing; }
            set { this.textLabel.WordSpacing = value; }
        }

        public TextButton(IUIElement parent, int x, int y, int width, int height, string text)
            : base(parent, x, y, width, height)
        {
            this.IsInteractive = true;
            this.textColour = Color.LightGray;
            this.textLabel = new TextLabel(this, 0, 0, width, height, text, Color.LightGray, TextAlignment.MiddleCentre) { IsInteractive = false };
            this.AddChild(textLabel);
        }

        public override void LoadContent()
        {
            this.CreateTexture();
            base.LoadContent();
        }

        protected void CreateTexture()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.texture.SetData(new Color[1] { Color.White });
        }

        protected override void DrawContent()
        {
            if (this.texture == null) this.CreateTexture();

            var r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var borderColour1 = this.IsSelected ? this.BorderColourSelected : (this.isMouseOver && this.IsEnabled ? this.BorderColourMouseOver : this.BorderColour1);
            var borderColour2 = this.IsSelected ? this.BorderColourSelected : (this.isMouseOver && this.IsEnabled ? this.BorderColourMouseOver : this.BorderColour2);

            this.spriteBatch.Begin();

            // Background
            this.DrawBackgroud(r);

            // Borders
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, r.Width, 1), borderColour1);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, 1, r.Height), borderColour1);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), borderColour2);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), borderColour2);

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        protected virtual void DrawBackgroud(Rectangle area)
        {
            this.spriteBatch.Draw(this.texture, area, this.IsHighlighted ? this.BackgroundColourHighlighted : this.BackgroundColour);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.textLabel != null) this.textLabel.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
