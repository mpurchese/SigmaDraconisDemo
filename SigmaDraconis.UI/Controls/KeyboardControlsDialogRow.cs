namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    
    internal class KeyboardControlsDialogRow : UIElementBase
    {
        private readonly TextButton nameButton;
        private readonly TextButton keyButton;
        private readonly TextButton changeButton;
        private readonly IconButton clearButton;

        public string Action { get; private set; }
        public string Key { get; private set; }
        public bool IsChanging { get; private set; }

        public event EventHandler<EventArgs> BeginChange;
        public event EventHandler<EventArgs> CancelChange;
        public event EventHandler<EventArgs> Clear;

        public bool IsEnabled
        {
            get
            {
                return this.nameButton.IsEnabled;
            }
            set
            {
                this.nameButton.IsEnabled = value;
                this.keyButton.IsEnabled = value;
                this.changeButton.IsEnabled = value;
                this.clearButton.IsEnabled = value;
                this.clearButton.IsInteractive = value;
                if (this.IsChanging && !value) this.Cancel();
            }
        }

        public KeyboardControlsDialogRow(IUIElement parent, int y, int r, string action, string name, string key) : base(parent, 0, Scale(y), parent.W, Scale(20))
        {
            this.Action = action;
            this.Key = key;

            var x = this.W - Scale(r);   // Right-to left, nameButton is the filler 

            this.clearButton = new IconButton(this, x, 0, "Textures\\Icons\\Delete", 1f, true) { IsEnabled = !string.IsNullOrEmpty(key), IsInteractive = !string.IsNullOrEmpty(key) };
            if (UIStatics.Content != null) clearButton.LoadContent();

            x -= Scale(80);
            this.changeButton = new TextButton(this, x, 0, Scale(76), Scale(20), LanguageHelper.GetForButton(StringsForButtons.Change))
            {
                BackgroundColour = new Color(18, 18, 18, 128),
                BorderColourMouseOver = new Color(192, 192, 192),
                TextColour = UIColour.DarkGreenText
            };

            x -= Scale(130);
            this.keyButton = new TextButton(this, x, 0, Scale(126), Scale(20), key)
            {
                BackgroundColour = new Color(0, 0, 0, 64),
                BorderColour1 = new Color(64, 64, 64),
                TextDisabledColour = UIColour.LightGrayText,
                IsEnabled = false,
            };

            this.nameButton = new TextButton(this, Scale(12), 0, x - Scale(16), Scale(20), name)
            {
                BackgroundColour = new Color(0, 0, 0, 64),
                BorderColour1 = new Color(64, 64, 64),
                TextDisabledColour = UIColour.LightGrayText,
                IsEnabled = false
            };

            this.changeButton.MouseLeftClick += this.OnChangeClick;
            this.clearButton.MouseLeftClick += this.OnClearClick;

            this.AddChild(nameButton);
            this.AddChild(keyButton);
            this.AddChild(changeButton);
            this.AddChild(clearButton);
        }

        public void Cancel()
        {
            this.IsChanging = false;
            this.keyButton.BorderColour1 = UIColour.BorderDark;
            this.keyButton.BorderColour2 = UIColour.BorderDark;
            this.keyButton.Text = this.Key;
            this.changeButton.Text = LanguageHelper.GetForButton(StringsForButtons.Change);
            this.changeButton.TextColour = UIColour.DarkGreenText;
            this.clearButton.IsEnabled = true;
            this.clearButton.IsInteractive = true;
        }

        private void OnChangeClick(object sender, MouseEventArgs e)
        {
            if (this.IsChanging)
            {
                this.IsChanging = false;
                this.CancelChange?.Invoke(this, new EventArgs());
                this.keyButton.BorderColour1 = UIColour.BorderDark;
                this.keyButton.BorderColour2 = UIColour.BorderDark;
                this.keyButton.Text = this.Key;
                this.changeButton.Text = LanguageHelper.GetForButton(StringsForButtons.Change);
                this.changeButton.TextColour = UIColour.DarkGreenText;
                this.clearButton.IsEnabled = true;
                this.clearButton.IsInteractive = true;
            }
            else
            {
                this.IsChanging = true;
                this.BeginChange?.Invoke(this, new EventArgs());
                this.keyButton.BorderColour1 = UIColour.LightBlueText;
                this.keyButton.BorderColour2 = UIColour.LightBlueText;
                this.keyButton.Text = LanguageManager.Get<StringsForKeyboardControlsDialog>(StringsForKeyboardControlsDialog.PressKey);
                this.changeButton.Text = LanguageHelper.GetForButton(StringsForButtons.Cancel);
                this.changeButton.TextColour = UIColour.RedText;
                this.clearButton.IsEnabled = false;
                this.clearButton.IsInteractive = false;
            }
        }

        private void OnClearClick(object sender, MouseEventArgs e)
        {
            this.Clear?.Invoke(this, new EventArgs());
        }
    }
}
