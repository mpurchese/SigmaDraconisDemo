namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using System;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using World;

    public class RocketLaunchedDialog : DialogBase
    {
        private readonly TextArea textArea;
        private readonly TextButton continueButton;
        private readonly TextButton exitButton;

        public event EventHandler<EventArgs> ExitClick;
        public event EventHandler<EventArgs> ContinueClick;

        public bool WasSnowRegionUnlocked { get; set; }

        public RocketLaunchedDialog(IUIElement parent)
            : base(parent, Scale(620), Scale(200), StringsForDialogTitles.RocketLaunched)
        {
            this.IsVisible = false;
            this.titleLabel.Colour = UIColour.BlueText;

            var text = LanguageManager.Get<StringsForDialogText>(StringsForDialogText.RocketLaunched1, 999) + "||" + LanguageManager.Get<StringsForDialogText>(StringsForDialogText.RocketLaunched2b);
            var requiredSize = TextArea.GetRequiredSize(text);
            if (requiredSize.X < 480) requiredSize.X = 480;
            this.W = Scale(requiredSize.X + 32);
            this.H = Scale(requiredSize.Y + 68);
            this.textArea = new TextArea(this, Scale(8), Scale(22), Scale(requiredSize.X + 16), Scale(requiredSize.Y + 12), UIColour.DefaultText, new Color(0, 0, 0, 128));
            this.AddChild(this.textArea);
            this.textArea.SetText(text);
            this.titleLabel.W = this.W;

            this.continueButton = new TextButtonWithLanguage(this, (this.W / 2) - Scale(220), this.H - Scale(26), Scale(200), Scale(20), StringsForButtons.ContinuePlaying) { TextColour = UIColour.GreenText, IsSelected = true };
            this.exitButton = new TextButtonWithLanguage(this, (this.W / 2) + Scale(20), this.H - Scale(26), Scale(200), Scale(20), StringsForButtons.ExitToMainMenu) { TextColour = UIColour.RedText };
            this.continueButton.MouseLeftClick += this.OnContinueClick;
            this.exitButton.MouseLeftClick += this.OnExitClick;
            this.AddChild(this.continueButton);
            this.AddChild(this.exitButton);
        }

        public void UpdateText()
        {
            var text = LanguageManager.Get<StringsForDialogText>(StringsForDialogText.RocketLaunched1, World.WorldTime.TotalHoursPassed) 
                + "||" + LanguageManager.Get<StringsForDialogText>(this.WasSnowRegionUnlocked ? StringsForDialogText.RocketLaunched2a : StringsForDialogText.RocketLaunched2b);
            var requiredSize = TextArea.GetRequiredSize(text);
            if (requiredSize.X < 480) requiredSize.X = 480;
            this.W = Scale(requiredSize.X + 32);
            this.H = Scale(requiredSize.Y + 60);
            this.textArea.W = Scale(requiredSize.X + 16);
            this.textArea.H = Scale(requiredSize.Y + 4);
            this.textArea.SetText(text);
            this.textArea.SetLastLineColour(UIColour.OrangeText);
            this.titleLabel.W = this.W;

            this.continueButton.X = (this.W / 2) - Scale(220);
            this.exitButton.X = (this.W / 2) + Scale(20);
            this.continueButton.Y = this.H - Scale(26);
            this.exitButton.Y = this.H - Scale(26);
        }

        protected override void HandleEnterOrSpaceKey()
        {
            if (this.continueButton.IsSelected) this.ContinueClick?.Invoke(this, new EventArgs());
            else this.ExitClick?.Invoke(this, new EventArgs());
            base.HandleEnterOrSpaceKey();
        }

        protected override void HandleLeftKey()
        {
            this.continueButton.IsSelected = !this.continueButton.IsSelected;
            this.exitButton.IsSelected = !this.exitButton.IsSelected;
            base.HandleLeftKey();
        }

        protected override void HandleRightKey()
        {
            this.continueButton.IsSelected = !this.continueButton.IsSelected;
            this.exitButton.IsSelected = !this.exitButton.IsSelected;
            base.HandleRightKey();
        }

        protected override void HandleUpKey()
        {
            this.continueButton.IsSelected = !this.continueButton.IsSelected;
            this.exitButton.IsSelected = !this.exitButton.IsSelected;
            base.HandleUpKey();
        }

        protected override void HandleDownKey()
        {
            this.continueButton.IsSelected = !this.continueButton.IsSelected;
            this.exitButton.IsSelected = !this.exitButton.IsSelected;
            base.HandleDownKey();
        }

        private void OnContinueClick(object sender, MouseEventArgs e)
        {
            this.ContinueClick?.Invoke(this, new EventArgs());
        }

        private void OnExitClick(object sender, MouseEventArgs e)
        {
            this.ExitClick?.Invoke(this, new EventArgs());
        }
    }
}
