namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;
    using Language;
    using Settings;

    public class DialogBase : Dialog
    {
        private readonly StringsForDialogTitles titleStringId;

        public DialogBase(IUIElement parent, int w, int h, StringsForDialogTitles titleStringId) : base(parent, w, h, LanguageManager.Get<StringsForDialogTitles>(titleStringId))
        {
            this.titleStringId = titleStringId;
        }

        public override void Update()
        {
            if (this.IsVisible && this.Parent is ModalBackgroundBox && this.backgroundColour.A != UIStatics.BackgroundAlpha)
            {
                this.backgroundColour = new Color(0, 0, 0, UIStatics.BackgroundAlpha);
                this.IsContentChangedSinceDraw = true;
            }

            base.Update();
        }

        public override void HandleKeyRelease(Keys key)
        {
            if (SettingsManager.GetKeysForAction("ToggleFullScreen").Contains(key.ToString()))
            {
                if (GameScreen.Instance.IsVisible) GameScreen.Instance.ToggleFullScreen();
                else if (MenuScreen.Instance.IsVisible) MenuScreen.Instance.ToggleFullScreen();
            }
            else if (this.IsVisible)
            {
                switch (key)
                {
                    case Keys.Escape: this.HandleEscapeKey(); break;
                    case Keys.Enter:
                    case Keys.Space:
                        this.HandleEnterOrSpaceKey(); break;
                    case Keys.Up: this.HandleUpKey(); break;
                    case Keys.Down: this.HandleDownKey(); break;
                    case Keys.Left: this.HandleLeftKey(); break;
                    case Keys.Right: this.HandleRightKey(); break;
                }
            }

            base.HandleKeyRelease(key);
        }

        protected virtual void HandleEscapeKey()
        {
        }

        protected virtual void HandleEnterOrSpaceKey()
        {
        }

        protected virtual void HandleUpKey()
        {
        }

        protected virtual void HandleDownKey()
        {
        }

        protected virtual void HandleLeftKey()
        {
        }

        protected virtual void HandleRightKey()
        {
        }

        protected override void HandleLanguageChange()
        {
            this.titleLabel.Text = LanguageManager.Get<StringsForDialogTitles>(this.titleStringId);
            this.titleLabel.W = this.W;

            base.HandleLanguageChange();
        }
    }
}
