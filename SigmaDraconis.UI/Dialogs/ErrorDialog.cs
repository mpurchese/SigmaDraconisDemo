namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;

    // Don't inherit from DialogBase as have custom title
    public class ErrorDialog : Dialog
    {
        private readonly TextArea textArea;
        private readonly TextButton continueButton;

        public event EventHandler<EventArgs> ContinueClick;

        public ErrorDialog(IUIElement parent)
            : base(parent, Scale(500), Scale(120), GetString(StringsForErrorDialog.DefaultTitle))
        {
            this.IsVisible = false;

            var text = GetString(StringsForErrorDialog.DefaultMessage);
            var requiredSize = TextArea.GetRequiredSize(text);
            this.W = Scale(requiredSize.X + 24);
            this.H = Scale(requiredSize.Y + 68);
            this.textArea = new TextArea(this, Scale(8), Scale(22), Scale(requiredSize.X + 8), Scale(requiredSize.Y + 12), UIColour.DefaultText, new Color(0, 0, 0, 128));
            this.AddChild(this.textArea);
            this.textArea.SetText(text);

            this.continueButton = new TextButton(this, (this.W / 2) - Scale(50), this.H - Scale(30), Scale(100), Scale(20), LanguageHelper.GetForButton(StringsForButtons.Continue));
            this.continueButton.MouseLeftClick += this.OnContinueClick;
            this.AddChild(this.continueButton);

            this.ResetMessage();
        }

        public void SetMessage(string title, string message)
        {
            this.Title = title;

            var requiredSize = TextArea.GetRequiredSize(message);
            this.W = Scale(requiredSize.X + 24);
            this.textArea.W = Scale(requiredSize.X + 8);
            this.textArea.SetText(message);
            this.titleLabel.W = this.W;
            this.continueButton.X = (this.W - this.continueButton.W) / 2;
            this.UpdateHorizontalPosition();
        }

        public void ResetMessage()
        {
            this.SetMessage(GetString(StringsForErrorDialog.DefaultTitle), GetString(StringsForErrorDialog.DefaultMessage));
        }

        protected override void HandleLanguageChange()
        {
            var continueStr = LanguageHelper.GetForButton(StringsForButtons.Continue);
            var continueWidth = Scale(((continueStr.Length * 7) + 36).Clamp(100, 300));
            this.continueButton.Text = continueStr;
            this.continueButton.X = (this.W - continueWidth) / 2;
            this.continueButton.W = continueWidth;

            base.HandleLanguageChange();
        }

        private void OnContinueClick(object sender, MouseEventArgs e)
        {
            this.ContinueClick?.Invoke(this, new EventArgs());
        }

        private static string GetString(object value)
        {
            return LanguageManager.Get(typeof(StringsForErrorDialog), value);
        }
    }
}
