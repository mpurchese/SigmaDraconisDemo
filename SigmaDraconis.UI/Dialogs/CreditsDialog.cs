namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;

    public class CreditsDialog : DialogBase
    {
        private readonly TextButton closeButton;
        private readonly TextArea textArea;

        public event EventHandler<EventArgs> CloseClick;

        public CreditsDialog(IUIElement parent)
            : base(parent, Scale(624), Scale(370), StringsForDialogTitles.Credits)
        {
            this.IsVisible = false;

            var text = LanguageManager.Get<StringsForDialogText>(StringsForDialogText.Credits);
            var requiredSize = TextArea.GetRequiredSize(text);
            this.W = Scale(requiredSize.X + 32);
            this.H = Scale(requiredSize.Y + 68);
            this.textArea = new TextArea(this, Scale(8), Scale(22), Scale(requiredSize.X + 16), Scale(requiredSize.Y + 12), UIColour.DefaultText, new Color(0, 0, 0, 128));
            this.AddChild(this.textArea);
            this.textArea.SetText(text);

            this.titleLabel.W = this.W;

            var closeStr = LanguageHelper.GetForButton(StringsForButtons.Close);
            var closeButtonWidth = Scale(((closeStr.Length * 7) + 36).Clamp(100, 300));
            this.closeButton = new TextButton(this, (this.W - closeButtonWidth) / 2, this.H - Scale(28), closeButtonWidth, Scale(20), closeStr) { IsSelected = true };
            this.closeButton.MouseLeftClick += this.OnCloseClick;
            this.AddChild(this.closeButton);

            this.UpdateHorizontalPosition();
            this.UpdateVerticalPosition();
        }

        protected override void HandleLanguageChange()
        {
            var text = LanguageManager.Get<StringsForDialogText>(StringsForDialogText.Credits);
            var requiredSize = TextArea.GetRequiredSize(text);
            this.W = Scale(requiredSize.X + 32);
            this.H = Scale(requiredSize.Y + 60);
            this.textArea.W = Scale(requiredSize.X + 16);
            this.textArea.H = Scale(requiredSize.Y + 4);
            this.textArea.SetText(text);

            this.titleLabel.W = this.W;

            var closeStr = LanguageHelper.GetForButton(StringsForButtons.Close);
            var closeWidth = Scale(((closeStr.Length * 7) + 42).Clamp(100, 300));
            this.closeButton.Text = closeStr;
            this.closeButton.X = (this.W - closeWidth) / 2;
            this.closeButton.Y = this.H - Scale(28);
            this.closeButton.W = closeWidth;

            base.HandleLanguageChange();
        }

        protected override void HandleEscapeKey()
        {
            this.CloseClick?.Invoke(this, new EventArgs());
            base.HandleEscapeKey();
        }

        protected override void HandleEnterOrSpaceKey()
        {
            this.CloseClick?.Invoke(this, new EventArgs());
            base.HandleEscapeKey();
        }

        private void OnCloseClick(object sender, MouseEventArgs e)
        {
            this.CloseClick?.Invoke(this, new EventArgs());
        }
    }
}
