namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class BatteryPanel : BuildingPanel, IThingPanel
    {
        private readonly ProgressBar batteryProgressBar;
        private readonly TextLabel batteryLabel1;
        private readonly TextLabel batteryLabel2;

        public BatteryPanel(IUIElement parent, int y)
            : base(parent, y)
        {
            var storageStr = GetString(StringsForThingPanels.Storage) + ": ";
            var onlineStr = LanguageManager.Get<BuildingDisplayStatus>(BuildingDisplayStatus.Online);
            var label1X = (this.W - (UIStatics.TextRenderer.LetterSpace * (storageStr.Length + onlineStr.Length))) / 2;
            var label2X = label1X + (UIStatics.TextRenderer.LetterSpace * storageStr.Length);
            this.batteryLabel1 = new TextLabel(this, label1X, Scale(25), storageStr, UIColour.DefaultText);
            this.batteryLabel2 = new TextLabel(this, label2X, Scale(25), onlineStr, UIColour.GreenText);
            this.batteryProgressBar = new ProgressBar(this, Scale(60), Scale(40), Scale(200), Scale(4)) { BarColour = UIColour.StorageBar };
            this.AddChild(this.batteryLabel1);
            this.AddChild(this.batteryLabel2);
            this.AddChild(this.batteryProgressBar);
        }

        public override void Update()
        {
            if (this.IsBuildingUiVisible)
            {
                var battery = this.building as IBattery;

                this.batteryProgressBar.IsVisible = true;
                this.batteryLabel1.IsVisible = true;
                this.batteryLabel2.IsVisible = true;

                this.batteryProgressBar.Fraction = battery.ChargeLevel.KWh / battery.ChargeCapacity.KWh;
            }
            else
            {
                this.batteryProgressBar.IsVisible = false;
                this.batteryLabel1.IsVisible = false;
                this.batteryLabel2.IsVisible = false;
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            var storageStr = GetString(StringsForThingPanels.Storage) + ": ";
            var onlineStr = LanguageManager.Get<BuildingDisplayStatus>(BuildingDisplayStatus.Online);
            this.batteryLabel1.X = (this.W - (UIStatics.TextRenderer.LetterSpace * (storageStr.Length + onlineStr.Length))) / 2;
            this.batteryLabel2.X = this.batteryLabel1.X + (UIStatics.TextRenderer.LetterSpace * storageStr.Length);
            this.batteryLabel1.Text = storageStr;
            this.batteryLabel2.Text = onlineStr;

            base.HandleLanguageChange();
        }
    }
}
