namespace SigmaDraconis.UI
{
    using System;
    using System.Timers;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using Draconis.Shared;
    using Draconis.UI;

    using Language;
    
    // Don't inherit from DialogBase as don't want to support F11
    public class ConfirmSettingsDialog : Dialog
    {
        private readonly Timer timer;
        private int timeToRevert = 10;
        private readonly TextButton okButton;
        private readonly TextButton cancelButton;
        private readonly TextLabel label2;
        private Texture2D pixelTexture2;

        public event EventHandler<EventArgs> OkClick;
        public event EventHandler<EventArgs> CancelClick;

        public ConfirmSettingsDialog(IUIElement parent)
            : base(parent, Scale(340), Scale(140), GetString(StringsForConfirmSettingsDialog.Title))
        {
            this.IsVisible = false;

            UIHelper.AddTextLabel(this, 0, 28, 340, UIColour.DefaultText, GetString(StringsForConfirmSettingsDialog.IsOK));
            this.label2 = UIHelper.AddTextLabel(this, 0, 44, 340, UIColour.DefaultText);

            this.okButton = new TextButtonWithLanguage(this, (this.W * 1 / 4) - Scale(40), this.H - Scale(30), Scale(100), Scale(20), StringsForButtons.OK) { TextColour = UIColour.GreenText };
            this.okButton.MouseLeftClick += this.OnOkClick;
            this.AddChild(this.okButton);

            this.cancelButton = new TextButtonWithLanguage(this, (this.W * 3 / 4) - Scale(60), this.H - Scale(30), Scale(100), Scale(20), StringsForButtons.Cancel);
            this.cancelButton.MouseLeftClick += this.OnCancelClick;
            this.AddChild(this.cancelButton);

            this.timer = new Timer(1000) { AutoReset = false };
            this.timer.Elapsed += this.OnTimerElapsed;
        }

        public override void LoadContent()
        {
            this.pixelTexture2 = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color2 = new Color[1] { new Color(0, 0, 0, 64) };
            this.pixelTexture2.SetData(color2);

            base.LoadContent();
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

        public void StartTimer()
        {
            this.timeToRevert = 10;
            this.label2.Text = LanguageManager.Get(typeof(StringsForConfirmSettingsDialog), StringsForConfirmSettingsDialog.RevertingIn, 10);
            this.UpdateHorizontalPosition();
            this.UpdateVerticalPosition();
            this.timer.Stop();
            this.timer.Start();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            this.timeToRevert--;
            this.label2.Text = LanguageManager.Get(typeof(StringsForConfirmSettingsDialog), StringsForConfirmSettingsDialog.RevertingIn, this.timeToRevert);
            if (this.timeToRevert <= 0)
            {
                this.OnCancelClick(null, null);
            }
            else if (this.IsVisible)
            {
                this.timer.Start();
            }
        }

        protected override void DrawBaseLayer()
        {
            Rectangle r2 = new Rectangle(0, 0, this.W, Scale(14));
            Rectangle r3 = new Rectangle(this.ScreenX + Scale(12), this.ScreenY + Scale(24), this.W - Scale(24), this.W - Scale(60));

            spriteBatch.Begin();
            spriteBatch.Draw(this.pixelTexture2, r2, Color.White);
            spriteBatch.Draw(this.pixelTexture, r3, Color.White);
            spriteBatch.End();
        }

        public override void HandleKeyRelease(Keys key)
        {
            if (this.IsVisible)
            {
                if (key == Keys.Escape)
                {
                    this.timer.Stop();
                    this.CancelClick?.Invoke(this, new EventArgs());
                }
                else if (key == Keys.Enter)
                {
                    this.timer.Stop();
                    this.OkClick?.Invoke(this, new EventArgs());
                }
            }

            base.HandleKeyRelease(key);
        }

        protected override void HandleLanguageChange()
        {
            this.titleLabel.Text = GetString(StringsForConfirmSettingsDialog.Title);
            base.HandleLanguageChange();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "label2")]  // Children are disposed in base class
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.timer.Stop();
                base.Dispose(true);
            }
        }

        private void OnOkClick(object sender, MouseEventArgs e)
        {
            this.timer.Stop();
            this.OkClick?.Invoke(this, new EventArgs());
        }

        private void OnCancelClick(object sender, MouseEventArgs e)
        {
            this.timer.Stop();
            this.CancelClick?.Invoke(this, new EventArgs());
        }

        private static string GetString(object value)
        {
            return LanguageManager.Get(typeof(StringsForConfirmSettingsDialog), value);
        }
    }
}
