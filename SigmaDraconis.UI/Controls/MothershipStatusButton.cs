namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;

    public class MothershipStatusButton : TextButton
    {
        private MothershipStatus mothershipStatus;
        private int timeToWake;
        private int timeToArrival;
        
        private Color progressColour;
        private double progressFraction;
        private int flashTimer = 0;
        private bool isUpdateNeeded;

        public MothershipStatusButton(IUIElement parent, int x, int y, int width, int height)
            : base(parent, x, y, width, height, "")
        {
            this.BackgroundColour = Color.Black;
        }

        public void UpdateStatus(MothershipStatus status, int timeToWake, int timeToArrival)
        {
            if (status == this.mothershipStatus && timeToWake == this.timeToWake && timeToArrival == this.timeToArrival && flashTimer == 0 && !this.isUpdateNeeded) return;

            if (this.mothershipStatus == MothershipStatus.PareparingToWake && status == MothershipStatus.ReadyToWakeNow) flashTimer = 56;

            this.mothershipStatus = status;
            this.timeToWake = timeToWake;
            this.timeToArrival = timeToArrival;

            switch (status)
            {
                case MothershipStatus.ColonistIncoming:
                case MothershipStatus.ColonistArriving:
                    this.progressColour = new Color(0, 48, 0);
                    this.progressFraction = (1.0 - (timeToArrival / (Constants.HoursToWakeColonist * 3600.0))).Clamp(0.0, 1.0);
                    break;
                case MothershipStatus.PareparingToWake:
                    this.progressColour = new Color(0, 32, 48);
                    this.progressFraction = (1.0 - (timeToWake / (Constants.HoursBetweenColonistWakes * 3600.0))).Clamp(0.0, 1.0);
                    break;
                case MothershipStatus.ReadyToWakeNow:
                    this.progressColour = new Color(0, 32 + (flashTimer * 4), 0);
                    this.progressFraction = 1.0;
                    break;
                case MothershipStatus.NoMoreColonists:
                    this.progressFraction = 0.0;
                    break;
            }

            this.Text = LanguageManager.Get<StringsForMothershipStatus>(status);
            this.TextColour = status == MothershipStatus.ReadyToWakeNow ? UIColour.DefaultText : UIColour.LightGrayText;
            this.WordSpacing = this.Text.Length > 20 ? 5 : 7;
            if (flashTimer > 0) flashTimer--;
            this.IsContentChangedSinceDraw = true;
            this.isUpdateNeeded = false;
        }

        protected override void HandleLanguageChange()
        {
            this.isUpdateNeeded = true;
            base.HandleLanguageChange();
        }

        protected override void DrawBackgroud(Rectangle area)
        {
            this.spriteBatch.Draw(this.texture, area, this.BackgroundColour);
            if (this.progressFraction > 0) this.spriteBatch.Draw(this.texture, new Rectangle(area.X, area.Y, (int)(area.Width * this.progressFraction), area.Height), this.progressColour);
        }

    }
}
