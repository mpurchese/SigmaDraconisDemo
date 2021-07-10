namespace SigmaDraconis.UI
{
    using System;

    using Microsoft.Xna.Framework;

    using Draconis.Input;
    using Draconis.Shared;
    using Draconis.UI;

    using Language;

    public class SavedGameDialog : DialogBase
    {
        private readonly TextLabel label1;
        private readonly TextButton continueButton;

        public event EventHandler<EventArgs> ContinueClick;

        public SavedGameDialog(IUIElement parent)
            : base(parent, Scale(300), Scale(100), StringsForDialogTitles.SavedGame)
        {
            this.IsVisible = false;

            this.label1 = new TextLabel(this, 0, Scale(28), this.W, Scale(18), "", UIColour.DefaultText);
            this.AddChild(this.label1);

            this.continueButton = UIHelper.AddTextButton(this, 70, StringsForButtons.Continue);
            this.continueButton.TextColour = UIColour.GreenText;
            this.continueButton.IsSelected = true;
            this.continueButton.MouseLeftClick += this.OnContinueClick;
        }

        protected override void HandleEscapeKey()
        {
            this.ContinueClick?.Invoke(this, new EventArgs());
            base.HandleEscapeKey();
        }

        protected override void HandleEnterOrSpaceKey()
        {
            this.ContinueClick?.Invoke(this, new EventArgs());
            base.HandleEscapeKey();
        }

        public void SetText(string message, Color colour)
        {
            this.label1.Text = message;
            this.label1.Colour = colour;
        }

        private void OnContinueClick(object sender, MouseEventArgs e)
        {
            this.ContinueClick?.Invoke(this, new EventArgs());
        }
    }
}
