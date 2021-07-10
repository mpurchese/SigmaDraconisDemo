namespace SigmaDraconis.UI
{
    using System;
    using Draconis.UI;
    using Language;

    public class TextButtonWithLanguage : TextButton
    {
        private readonly StringsForButtons textId;

        private readonly bool autoWidth;
        private readonly bool autoCenter;

        public TextButtonWithLanguage(IUIElement parent, int x, int y, int width, int height, StringsForButtons textId)
            : base(parent, x, y, width, height, LanguageManager.Get<StringsForButtons>(textId))
        {
            this.textId = textId;
        }

        public TextButtonWithLanguage(IUIElement parent, int y, StringsForButtons textId, int heightUnscaled = 18)
            : base(parent, 0, y, 100, Scale(heightUnscaled), LanguageManager.Get<StringsForButtons>(textId))
        {
            this.textId = textId;
            this.W = Math.Min(this.Parent.W, Scale((this.Text.Length * 7) + 36));
            this.X = (this.Parent.W - this.W) / 2;
            this.autoWidth = true;
            this.autoCenter = true;
        }

        protected override void HandleLanguageChange()
        {
            this.Text = LanguageManager.Get<StringsForButtons>(this.textId);

            if (this.autoWidth)
            {
                this.W = Math.Min(this.Parent.W, Scale((this.Text.Length * 7) + 36));
                if (this.autoCenter) this.X = (this.Parent.W - this.W) / 2;
            }

            base.HandleLanguageChange();
        }
    }
}
