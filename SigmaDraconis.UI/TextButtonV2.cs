namespace SigmaDraconis.UI
{
    using System;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Shared;

    public class TextButtonV2 : TexturedElementV2, IDisposable, IButton
    {
        private bool isMouseOver;

        protected SimpleTextLabel textLabel;
        protected bool isEnabled = true;
        protected Color textColour;
        protected Color textDisabledColour = Color.Gray;

        public string Tag { get; set; }

        public Color BackgroundColour { get; set; } = new Color(0, 0, 0, 64);

        public Color BorderColour { get; set; } = new Color(64, 64, 64);

        public Color HoverBorderColour { get; set; } = new Color(128, 128, 128);

        public Color SelectedBorderColour { get; set; } = new Color(0, 255, 255);

        public bool IsSelected { get; set; }
        
        public string Text
        {
            get
            {
                return this.textLabel.Text;
            }

            set
            {
                this.textLabel.Text = value;
            }
        }

        public bool IsEnabled
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
                    this.IsDrawInvalidated = true;
                }
            }
        }

        public TextAlignment TextAlign
        {
            get
            {
                return this.textLabel.TextAlign;
            }

            set
            {
                this.textLabel.TextAlign = value;
            }
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

        public TextButtonV2(GraphicsDevice graphicsDevice, UIElement parent, int x, int y, int width, int height, string text)
            : base(graphicsDevice, parent, x, y, width, height)
        {
            this.textColour = Color.LightGray;
            this.textLabel = new SimpleTextLabel(this, 0, 0, width, height, text, Color.LightGray, TextAlignment.MiddleCentre) { IsInteractive = false };
            this.Children.Add(textLabel);
        }

        public override void Update()
        {
            if (this.isMouseOver != this.IsMouseOver)
            {
                this.IsDrawInvalidated = true;
                this.isMouseOver = this.IsMouseOver;
            }

            base.Update();
        }

        public override void Draw()
        {
            if (this.texture == null || this.texture.IsDisposed)
            {
                this.texture = new Texture2D(this.graphicsDevice, 1, 1);
                this.texture.SetData(new Color[1] { Color.White });
            }

            if (this.IsVisible && this.texture != null)
            {
                int parentX = Parent == null ? 0 : Parent.ScreenX;
                int parentY = Parent == null ? 0 : Parent.ScreenY;
                Rectangle r = new Rectangle(rectangle.X + parentX, rectangle.Y + parentY, this.Width, this.Height);
                var borderColour = this.IsSelected ? this.SelectedBorderColour : (this.isMouseOver && this.IsEnabled ? this.HoverBorderColour : this.BorderColour);

                this.spriteBatch.Begin();

                // Background
                this.spriteBatch.Draw(this.texture, r, this.BackgroundColour);

                // Borders
                this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, r.Width, 1), borderColour);
                this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, 1, r.Height), borderColour);
                this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), borderColour);
                this.spriteBatch.Draw(this.texture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), borderColour);

                this.spriteBatch.End();

                this.textLabel.Draw();
            }

            this.IsDrawInvalidated = false;
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
