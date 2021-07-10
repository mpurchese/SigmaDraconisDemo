namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using World;

    public class GameOverDialog : DialogBase
    {
        private readonly TextArea textArea;
        private readonly TextButton exitToMenuButton;
        private readonly TextButton exitToDesktopButton;

        public event EventHandler<EventArgs> ExitToWindowsClick;
        public event EventHandler<EventArgs> ExitToMenuClick;

        public GameOverDialog(IUIElement parent)
            : base(parent, Scale(560), Scale(180), StringsForDialogTitles.GameOver)
        {
            this.IsVisible = false;
            this.titleLabel.Colour = UIColour.RedText;

            var text = LanguageManager.Get<StringsForDialogText>(StringsForDialogText.GameOver, 0);
            var requiredSize = TextArea.GetRequiredSize(text);
            this.W = Scale(requiredSize.X + 32);
            this.H = Scale(requiredSize.Y + 68);
            this.textArea = new TextArea(this, Scale(8), Scale(22), Scale(requiredSize.X + 16), Scale(requiredSize.Y + 12), UIColour.DefaultText, new Color(0, 0, 0, 128));
            this.AddChild(this.textArea);
            this.textArea.SetText(text);
            this.titleLabel.W = this.W;

            this.exitToMenuButton = new TextButtonWithLanguage(this, (this.W / 2) - Scale(220), this.H - Scale(30), Scale(200), Scale(20), StringsForButtons.MainMenu) { IsSelected = true };
            this.exitToDesktopButton = new TextButtonWithLanguage(this, (this.W / 2) + Scale(20), this.H - Scale(30), Scale(200), Scale(20), StringsForButtons.ExitToDesktop);
            this.exitToMenuButton.MouseLeftClick += this.OnExitToMenuClick;
            this.exitToDesktopButton.MouseLeftClick += this.OnExitClick;
            this.AddChild(this.exitToMenuButton);
            this.AddChild(this.exitToDesktopButton);
        }

        public void UpdateText()
        {
            var text = LanguageManager.Get<StringsForDialogText>(StringsForDialogText.GameOver, World.WorldTime.TotalHoursPassed);
            var requiredSize = TextArea.GetRequiredSize(text);
            this.W = Scale(requiredSize.X + 32);
            this.H = Scale(requiredSize.Y + 60);
            this.textArea.W = Scale(requiredSize.X + 16);
            this.textArea.H = Scale(requiredSize.Y + 4);
            this.textArea.SetText(text);
            this.titleLabel.W = this.W;

            this.exitToMenuButton.X = (this.W / 2) - Scale(220);
            this.exitToDesktopButton.X = (this.W / 2) + Scale(20);
            this.exitToMenuButton.Y = this.H - Scale(26);
            this.exitToDesktopButton.Y = this.H - Scale(26);
        }

        protected override void HandleEnterOrSpaceKey()
        {
            if (this.exitToMenuButton.IsSelected) this.ExitToMenuClick?.Invoke(this, new EventArgs());
            else this.ExitToWindowsClick?.Invoke(this, new EventArgs());
            base.HandleEnterOrSpaceKey();
        }

        protected override void HandleLeftKey()
        {
            this.exitToMenuButton.IsSelected = !this.exitToMenuButton.IsSelected;
            this.exitToDesktopButton.IsSelected = !this.exitToDesktopButton.IsSelected;
            base.HandleLeftKey();
        }

        protected override void HandleRightKey()
        {
            this.exitToMenuButton.IsSelected = !this.exitToMenuButton.IsSelected;
            this.exitToDesktopButton.IsSelected = !this.exitToDesktopButton.IsSelected;
            base.HandleRightKey();
        }

        protected override void HandleUpKey()
        {
            this.exitToMenuButton.IsSelected = !this.exitToMenuButton.IsSelected;
            this.exitToDesktopButton.IsSelected = !this.exitToDesktopButton.IsSelected;
            base.HandleUpKey();
        }

        protected override void HandleDownKey()
        {
            this.exitToMenuButton.IsSelected = !this.exitToMenuButton.IsSelected;
            this.exitToDesktopButton.IsSelected = !this.exitToDesktopButton.IsSelected;
            base.HandleDownKey();
        }

        private void OnExitToMenuClick(object sender, MouseEventArgs e)
        {
            this.ExitToMenuClick?.Invoke(this, new EventArgs());
        }

        private void OnExitClick(object sender, MouseEventArgs e)
        {
            this.ExitToWindowsClick?.Invoke(this, new EventArgs());
        }
    }
}
