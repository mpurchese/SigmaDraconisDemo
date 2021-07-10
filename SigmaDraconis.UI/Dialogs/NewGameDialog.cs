namespace SigmaDraconis.UI
{
    using System;
    using System.Text;
    using Microsoft.Xna.Framework;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Settings;
    using Shared;

    public class NewGameDialog : DialogBase
    {
        private readonly HorizontalStack buttonStack;
        private readonly IconButton moderateClimateButton;
        private readonly IconButton harshClimateButton;
        private readonly TextButton startButton;
        private readonly TextButton cancelButton;
        private readonly TextArea textArea;
        private ButtonBase selectedButton;
        private ClimateType selectedClimateType = ClimateType.Normal;

        public event EventHandler<EventArgs> Cancel;
        public event EventHandler<NewGameEventArgs> NewGame;

        public NewGameDialog(IUIElement parent)
            : base(parent, Scale(320), Scale(290), StringsForDialogTitles.NewGame)
        {
            this.IsVisible = false;

            this.buttonStack = new HorizontalStack(this, 0, Scale(20), this.W, Scale(150), TextAlignment.MiddleCentre) { Spacing = 8 };
            this.AddChild(this.buttonStack);

            this.moderateClimateButton = new IconButton(this.buttonStack, 0, 0, "Textures\\Menu\\Climate1", 0.5f);
            this.moderateClimateButton.MouseLeftClick += this.OnModerateClimateClick;
            this.buttonStack.AddChild(this.moderateClimateButton);
            this.harshClimateButton = new IconButton(this.buttonStack, 0, 0, "Textures\\Menu\\Climate2", 0.5f);
            this.harshClimateButton.MouseLeftClick += this.OnHarshClimateClick;
            this.buttonStack.AddChild(this.harshClimateButton);

            this.textArea = new TextArea(this, Scale(8), Scale(176), this.W - Scale(16), Scale(80), UIColour.DefaultText, new Color(0, 0, 0, 128));
            this.AddChild(this.textArea);

            this.startButton = new TextButton(this, (this.W / 2) - Scale(60), this.H - Scale(26), Scale(120), Scale(20), "") { TextColour = UIColour.GreenText, BackgroundColour = UIColour.ButtonBackgroundDark };
            this.startButton.MouseLeftClick += this.OnStartClick;
            this.AddChild(this.startButton);

            this.cancelButton = new TextButton(this, this.W - Scale(88), this.H - Scale(26), Scale(80), Scale(20), "") { TextColour = UIColour.RedText, BackgroundColour = UIColour.ButtonBackgroundDark };
            this.cancelButton.MouseLeftClick += this.OnCancelClick;
            this.AddChild(this.cancelButton);

            this.selectedButton = this.moderateClimateButton;
        }

        public override void Show()
        {
            this.SetSelectedButton(this.moderateClimateButton);
            this.UpdateContents();
            base.Show();
        }

        protected override void HandleLanguageChange()
        {
            this.UpdateContents();
            base.HandleLanguageChange();
        }

        private void UpdateContents()
        {
            var isSnowLocked = true;    // DEMO

            var text1 = GetText(StringsForDialogText.NewGame) + "||" + GetText(StringsForDialogText.NewGameClimate1);
            var text2 = GetText(StringsForDialogText.NewGame) + "||" + GetText(StringsForDialogText.NewGameClimate2) + "||" 
                + GetText(isSnowLocked ? StringsForDialogText.NewGameClimate2a : StringsForDialogText.NewGameClimate2b);
            var requiredSize1 = TextArea.GetRequiredSize(text1);
            var requiredSize2 = TextArea.GetRequiredSize(text2);
            var requiredSize = new Vector2i(Math.Max(requiredSize1.X, requiredSize2.X), Math.Max(requiredSize1.Y, requiredSize2.Y));

            this.W = Scale(Math.Max(requiredSize.X + 32, 624));
            this.H = Scale(requiredSize.Y + 222);

            this.titleLabel.W = this.W;

            this.textArea.W = this.W - Scale(16);
            this.textArea.H = Scale(requiredSize.Y + 12);
            this.textArea.SetText(this.selectedClimateType == ClimateType.Snow ? text2 : text1, true);
            this.textArea.SetLastLineColour(this.selectedClimateType == ClimateType.Snow ? (isSnowLocked ? UIColour.RedText : UIColour.OrangeText) : UIColour.DefaultText);

            this.buttonStack.W = this.W;

            this.startButton.X = (this.W / 2) - Scale(60);
            this.startButton.Y = this.H - Scale(26);
            this.startButton.Text = LanguageHelper.GetForButton(StringsForButtons.Start);
            this.startButton.IsEnabled = this.selectedClimateType == ClimateType.Normal || !isSnowLocked;

            this.cancelButton.X = this.W - Scale(88);
            this.cancelButton.Y = this.H - Scale(26);
            this.cancelButton.Text = LanguageHelper.GetForButton(StringsForButtons.Cancel);

            this.buttonStack.LayoutInvalidated = true;

            this.harshClimateButton.IsEnabled = !isSnowLocked;

            this.UpdateHorizontalPosition();
            this.UpdateVerticalPosition();
        }

        protected override void HandleEscapeKey()
        {
            this.Cancel?.Invoke(this, new EventArgs());
            base.HandleEscapeKey();
        }

        protected override void HandleEnterOrSpaceKey()
        {
            this.selectedButton.TriggerClickEvent();
            base.HandleEnterOrSpaceKey();
        }

        protected override void HandleDownKey()
        {
            if (this.selectedButton == this.moderateClimateButton) this.SetSelectedButton(this.startButton);
            else if (this.selectedButton == this.harshClimateButton) this.SetSelectedButton(this.cancelButton);
            base.HandleDownKey();
        }

        protected override void HandleUpKey()
        {
            if (this.selectedButton == this.startButton) this.SetSelectedButton(this.moderateClimateButton);
            else if (this.selectedButton == this.cancelButton) this.SetSelectedButton(this.harshClimateButton);
            base.HandleDownKey();
        }

        protected override void HandleRightKey()
        {
            if (this.selectedButton == this.moderateClimateButton) this.SetSelectedButton(this.harshClimateButton);
            else if (this.selectedButton == this.harshClimateButton) this.SetSelectedButton(this.startButton);
            else if (this.selectedButton == this.startButton) this.SetSelectedButton(this.cancelButton);
            else if (this.selectedButton == this.cancelButton) this.SetSelectedButton(this.moderateClimateButton);
            base.HandleDownKey();
        }

        protected override void HandleLeftKey()
        {
            if (this.selectedButton == this.moderateClimateButton) this.SetSelectedButton(this.cancelButton);
            else if (this.selectedButton == this.harshClimateButton) this.SetSelectedButton(this.moderateClimateButton);
            else if (this.selectedButton == this.startButton) this.SetSelectedButton(this.harshClimateButton);
            else if (this.selectedButton == this.cancelButton) this.SetSelectedButton(this.startButton);
            base.HandleDownKey();
        }

        private void SetSelectedButton(ButtonBase button)
        {
            this.moderateClimateButton.IsSelected = button == this.moderateClimateButton;
            this.harshClimateButton.IsSelected = button == this.harshClimateButton;
            this.startButton.IsSelected = button == this.startButton;
            this.cancelButton.IsSelected = button == this.cancelButton;
            this.selectedButton = button;

            if (button == this.harshClimateButton) this.selectedClimateType = ClimateType.Snow;
            else if (button == this.moderateClimateButton) this.selectedClimateType = ClimateType.Normal;

            var isSnowLocked = true;
            var sb = new StringBuilder(GetText(StringsForDialogText.NewGame));
            sb.Append("||");
            if (this.selectedClimateType == ClimateType.Snow)
            {
                sb.Append(GetText(StringsForDialogText.NewGameClimate2));
                sb.Append("||");
                sb.Append(GetText(isSnowLocked ? StringsForDialogText.NewGameClimate2a : StringsForDialogText.NewGameClimate2b));
            }
            else sb.Append(GetText(StringsForDialogText.NewGameClimate1));

            this.textArea.SetText(sb.ToString());
            this.textArea.SetLastLineColour(this.selectedClimateType == ClimateType.Snow ? (isSnowLocked ? UIColour.RedText : UIColour.OrangeText) : UIColour.DefaultText);

            this.startButton.IsEnabled = this.selectedClimateType == ClimateType.Normal || !isSnowLocked;
            this.harshClimateButton.IsEnabled = !isSnowLocked;
        }

        private void OnModerateClimateClick(object sender, MouseEventArgs e)
        {
            this.SetSelectedButton(this.moderateClimateButton);
        }

        private void OnHarshClimateClick(object sender, MouseEventArgs e)
        {
            this.SetSelectedButton(this.harshClimateButton);
        }

        private void OnStartClick(object sender, MouseEventArgs e)
        {
            this.NewGame?.Invoke(this, new NewGameEventArgs(64, this.selectedClimateType));
        }

        private void OnCancelClick(object sender, MouseEventArgs e)
        {
            this.Cancel?.Invoke(this, new EventArgs());
        }

        private static string GetText(StringsForDialogText id)
        {
            return LanguageManager.Get<StringsForDialogText>(id);
        }
    }
}
