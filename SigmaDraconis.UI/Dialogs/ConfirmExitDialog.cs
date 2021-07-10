namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;

    public class ConfirmExitDialog : DialogBase
    {
        private readonly HorizontalStack buttonStack;
        private readonly TextArea textArea;
        private readonly TextButton exitButton;
        private readonly TextButton saveButton;
        private readonly TextButton cancelButton;

        public event EventHandler<EventArgs> CancelClick;
        public event EventHandler<EventArgs> SaveClick;
        public event EventHandler<EventArgs> ExitClick;

        public ConfirmExitDialog(IUIElement parent)
            : base(parent, Scale(320), Scale(140), StringsForDialogTitles.ConfirmExit)
        {
            this.IsVisible = false;

            var text = LanguageManager.Get<StringsForDialogText>(StringsForDialogText.ConfirmExit);
            var requiredSize = TextArea.GetRequiredSize(text);
            this.W = Scale(Math.Max(340, requiredSize.X + 32));
            this.H = Scale(requiredSize.Y + 68);
            this.textArea = new TextArea(this, Scale(8), Scale(22), this.W - Scale(16), Scale(requiredSize.Y + 12), UIColour.DefaultText, new Color(0, 0, 0, 0));
            this.AddChild(this.textArea);
            this.textArea.SetText(text);

            this.titleLabel.W = this.W;

            this.buttonStack = new HorizontalStack(this, 0, this.H - Scale(30), this.W, Scale(20), TextAlignment.MiddleCentre) { Spacing = 8 };
            this.AddChild(this.buttonStack);

            this.saveButton = new TextButton(this.buttonStack, 0, 0, Scale(100), Scale(20), LanguageHelper.GetForButton(StringsForButtons.Save)) { TextColour = UIColour.GreenText };
            this.saveButton.MouseLeftClick += this.OnSaveClick;
            this.buttonStack.AddChild(this.saveButton);

            this.exitButton = new TextButton(this.buttonStack, 0, 0, Scale(100), Scale(20), LanguageHelper.GetForButton(StringsForButtons.Exit)) { TextColour = UIColour.RedText };
            this.exitButton.MouseLeftClick += this.OnExitClick;
            this.buttonStack.AddChild(this.exitButton);

            this.cancelButton = new TextButton(this.buttonStack, 0, 0, Scale(100), Scale(20), LanguageHelper.GetForButton(StringsForButtons.Cancel));
            this.cancelButton.MouseLeftClick += this.OnCancelClick;
            this.buttonStack.AddChild(this.cancelButton);
        }

        protected override void HandleEscapeKey()
        {
            this.CancelClick?.Invoke(this, new EventArgs());
            base.HandleEscapeKey();
        }

        protected override void HandleLanguageChange()
        {
            var text = LanguageManager.Get<StringsForDialogText>(StringsForDialogText.ConfirmExit);
            var requiredSize = TextArea.GetRequiredSize(text);
            this.W = Scale(Math.Max(340, requiredSize.X + 32));
            this.textArea.W = this.W - Scale(16);
            this.textArea.H = Scale(requiredSize.Y + 4);
            this.textArea.SetText(text);

            this.titleLabel.W = this.W;

            this.exitButton.Text = LanguageHelper.GetForButton(StringsForButtons.Exit);
            this.saveButton.Text = LanguageHelper.GetForButton(StringsForButtons.Save);
            this.cancelButton.Text = LanguageHelper.GetForButton(StringsForButtons.Cancel);

            this.buttonStack.W = this.W;
            this.buttonStack.LayoutInvalidated = true;

            base.HandleLanguageChange();
        }

        private void OnExitClick(object sender, MouseEventArgs e)
        {
            this.ExitClick?.Invoke(this, new EventArgs());
        }

        private void OnSaveClick(object sender, MouseEventArgs e)
        {
            this.SaveClick?.Invoke(this, new EventArgs());
        }

        private void OnCancelClick(object sender, MouseEventArgs e)
        {
            this.CancelClick?.Invoke(this, new EventArgs());
        }
    }
}
