namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Draconis.UI;
    using Language;

    public class TickBoxTextButton : TextButton
    {
        private readonly Icon tickBoxIcon = null;
        private readonly bool isLanguageConfigured;
        private readonly object textId;

        private bool isTicked;
        public bool IsTicked { get { return this.isTicked; } set { if (this.isTicked != value) { this.isTicked = value; this.IsContentChangedSinceDraw = true; } } }

        public TickBoxTextButton(IUIElement parent, int x, int y, int width, int height, string text)
            : base(parent, x, y, width, height, text)
        {
            this.textLabel.TextAlign = TextAlignment.MiddleLeft;
            this.textLabel.X = Scale(20);
            this.tickBoxIcon = new Icon("Textures\\Icons\\TickBox", 2);
        }

        public TickBoxTextButton(IUIElement parent, int y, object textId)
            : base(parent, 0, y, 100, Scale(20), LanguageManager.Get(textId.GetType(), textId))
        {
            this.isLanguageConfigured = true;

            this.textId = textId;
            this.textLabel.TextAlign = TextAlignment.MiddleLeft;
            this.textLabel.X = Scale(20);

            this.W = Math.Min(this.Parent.W, Scale((this.Text.Length * 7) + 30));
            this.X = (this.Parent.W - this.W) / 2;

            this.tickBoxIcon = new Icon("Textures\\Icons\\TickBox", 2);
        }

        public override void LoadContent()
        {
            base.LoadContent();
            this.tickBoxIcon.LoadContent();
        }

        protected override void DrawContent()
        {
            if (this.texture == null) this.CreateTexture();

            var r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);
            var borderColour1 = this.IsSelected ? this.BorderColourSelected : (this.isMouseOver && this.IsEnabled ? this.BorderColourMouseOver : this.BorderColour1);
            var borderColour2 = this.IsSelected ? this.BorderColourSelected : (this.isMouseOver && this.IsEnabled ? this.BorderColourMouseOver : this.BorderColour2);

            this.spriteBatch.Begin();

            // Background
            this.spriteBatch.Draw(this.texture, r, this.BackgroundColour);

            // Borders
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, r.Width, 1), borderColour1);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Y, 1, r.Height), borderColour1);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), borderColour2);
            this.spriteBatch.Draw(this.texture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), borderColour2);

            // Tick box
            this.tickBoxIcon.Draw(this.spriteBatch, this.RenderX + Scale(2), this.RenderY + (this.H - Scale(16)) / 2, this.isTicked ? 1 : 0);

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        protected override void HandleLanguageChange()
        {
            if (isLanguageConfigured)
            {
                this.Text = LanguageManager.Get(this.textId.GetType(), this.textId);
                this.W = Math.Min(this.Parent.W, Scale((this.Text.Length * 7) + 30));
                this.X = (this.Parent.W - this.W) / 2;
            }

            base.HandleLanguageChange();
        }
    }
}
