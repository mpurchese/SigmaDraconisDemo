namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Microsoft.Xna.Framework.Input;

    public class OptionsPanel : PanelRight
    {
        private readonly List<ButtonBase> buttons;

        public TextButton SaveButton { get; private set; }
        public TextButton AchievementsButton { get; private set; }
        public TextButton OptionsButton { get; private set; }
        public TextButton KeyboardButton { get; private set; }
        public TextButton ExitToMainMenuButton { get; private set; }
        public TextButton ExitToDesktopButton { get; private set; }

        public event EventHandler<EventArgs> AchievementsClick;
        public event EventHandler<EventArgs> SaveGameClick;
        public event EventHandler<EventArgs> SettingsClick;
        public event EventHandler<EventArgs> KeyboardControlsClick;
        public event EventHandler<EventArgs> ExitToMainMenuClick;
        public event EventHandler<EventArgs> ExitToDesktopClick;

        public OptionsPanel(IUIElement parent, int y)
            : base(parent, y, Scale(240), Scale(206), GetString(StringsForOptionsPanel.Title))
        {
            this.SaveButton = UIHelper.AddTextButton(this, 20, 24, 200, GetString(StringsForOptionsPanel.SaveGame), 22);
            this.AchievementsButton = UIHelper.AddTextButton(this, 20, 54, 200, GetString(StringsForOptionsPanel.Settings), 22);
            this.OptionsButton = UIHelper.AddTextButton(this, 20, 84, 200, GetString(StringsForOptionsPanel.Settings), 22);
            this.KeyboardButton = UIHelper.AddTextButton(this, 20, 114, 200, GetString(StringsForOptionsPanel.KeyboardControls), 22);
            this.ExitToMainMenuButton = UIHelper.AddTextButton(this, 20, 144, 200, GetString(StringsForOptionsPanel.ExitMainMenu), 22);
            this.ExitToDesktopButton = UIHelper.AddTextButton(this, 20, 174, 200, GetString(StringsForOptionsPanel.ExitDesktop), 22);

            this.SaveButton.IsSelected = true;
            this.buttons = this.Children.OfType<ButtonBase>().ToList();

            this.SaveButton.MouseLeftClick += this.OnSaveButtonClick;
            this.AchievementsButton.MouseLeftClick += this.OnAchievementsButtonClick;
            this.OptionsButton.MouseLeftClick += this.OnOptionsButtonClick;
            this.KeyboardButton.MouseLeftClick += this.OnKeyboardButtonClick;
            this.ExitToMainMenuButton.MouseLeftClick += this.OnExitToMainMenuButtonClick;
            this.ExitToDesktopButton.MouseLeftClick += this.OnExitToDesktopButtonClick;

            foreach (var button in this.buttons)
            {
                button.MouseScrollDown += this.OnButtonMouseScrollDown;
                button.MouseScrollUp += this.OnButtonMouseScrollUp;
            }
        }

        protected override void HandleLanguageChange()
        {
            this.Title = GetString(StringsForOptionsPanel.Title);
            this.AchievementsButton.Text = GetString(StringsForOptionsPanel.Achievements);
            this.SaveButton.Text = GetString(StringsForOptionsPanel.SaveGame);
            this.OptionsButton.Text = GetString(StringsForOptionsPanel.Settings);
            this.KeyboardButton.Text = GetString(StringsForOptionsPanel.KeyboardControls);
            this.ExitToMainMenuButton.Text = GetString(StringsForOptionsPanel.ExitMainMenu);
            this.ExitToDesktopButton.Text = GetString(StringsForOptionsPanel.ExitDesktop);
            base.HandleLanguageChange();
        }

        private void OnButtonMouseScrollDown(object sender, MouseEventArgs e)
        {
            this.MoveDown();
        }

        private void OnButtonMouseScrollUp(object sender, MouseEventArgs e)
        {
            this.MoveUp();
        }

        protected override void OnMouseScrollDown(MouseEventArgs e)
        {
            this.MoveDown();
        }

        protected override void OnMouseScrollUp(MouseEventArgs e)
        {
            this.MoveUp();
        }

        public bool HandleKeyPress(Keys key)
        {
            switch (key)
            {
                case Keys.Down:
                    this.MoveDown();
                    return true;
                case Keys.Up:
                    this.MoveUp();
                    return true;
                case Keys.Space:
                case Keys.Enter:
                    if (key == Keys.Space && !this.IsMouseOver) return false;
                    if (this.SaveButton.IsSelected) this.SaveGameClick?.Invoke(this, new EventArgs());
                    else if (this.AchievementsButton.IsSelected) this.AchievementsClick?.Invoke(this, new EventArgs());
                    else if (this.OptionsButton.IsSelected) this.SettingsClick?.Invoke(this, new EventArgs());
                    else if (this.KeyboardButton.IsSelected) this.KeyboardControlsClick?.Invoke(this, new EventArgs());
                    else if (this.ExitToMainMenuButton.IsSelected) this.ExitToMainMenuClick?.Invoke(this, new EventArgs());
                    else if (this.ExitToDesktopButton.IsSelected) this.ExitToDesktopClick?.Invoke(this, new EventArgs());
                    return true;
            }

            return false;
        }

        public void MoveDown()
        {
            var selectedButtonIndex = this.buttons.FindIndex(b => b.IsSelected);
            if (selectedButtonIndex < 0) return;

            this.buttons[selectedButtonIndex].IsSelected = false;
            if (selectedButtonIndex < this.buttons.Count - 1) this.buttons[selectedButtonIndex + 1].IsSelected = true;
            else this.buttons[0].IsSelected = true;
        }

        public void MoveUp()
        {
            var selectedButtonIndex = this.buttons.FindIndex(b => b.IsSelected);
            if (selectedButtonIndex < 0) return;

            this.buttons[selectedButtonIndex].IsSelected = false;
            if (selectedButtonIndex > 0) this.buttons[selectedButtonIndex - 1].IsSelected = true;
            else this.buttons[this.buttons.Count - 1].IsSelected = true;
        }
        private void OnSaveButtonClick(object sender, MouseEventArgs e)
        {
            this.SaveGameClick?.Invoke(this, new EventArgs());
        }

        private void OnAchievementsButtonClick(object sender, MouseEventArgs e)
        {
            this.AchievementsClick?.Invoke(this, new EventArgs());
        }

        private void OnOptionsButtonClick(object sender, MouseEventArgs e)
        {
            this.SettingsClick?.Invoke(this, new EventArgs());
        }

        private void OnKeyboardButtonClick(object sender, MouseEventArgs e)
        {
            this.KeyboardControlsClick?.Invoke(this, new EventArgs());
        }

        private void OnExitToMainMenuButtonClick(object sender, MouseEventArgs e)
        {
            this.ExitToMainMenuClick?.Invoke(this, new EventArgs());
        }

        private void OnExitToDesktopButtonClick(object sender, MouseEventArgs e)
        {
            this.ExitToDesktopClick?.Invoke(this, new EventArgs());
        }

        private static string GetString(object value)
        {
            return LanguageManager.Get<StringsForOptionsPanel>(value);
        }
    }
}
