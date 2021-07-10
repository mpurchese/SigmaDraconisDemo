namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class OreScannerPanel : BuildingPanel, IThingPanel
    {
        private readonly PowerButtonWithUsageDisplay powerButton;
        private readonly BuildingStatusControl statusControl;
        private readonly SimpleTooltip powerButtonTooltip;
        private readonly TextLabel statusDetailLabel;
        private double? energyUseRate;

        public OreScannerPanel(IUIElement parent, int y) : base(parent, y, Scale(130))
        {
            this.powerButton = new PowerButtonWithUsageDisplay(this, Scale(222), Scale(16));
            this.AddChild(this.powerButton);
            this.powerButtonTooltip = UIHelper.AddSimpleTooltip(this, this.powerButton, "", GetString(StringsForThingPanels.ClickToTogglePower));

            this.statusControl = new BuildingStatusControl(this, Scale(38), Scale(40), Scale(248), Scale(20), false, true);
            this.AddChild(this.statusControl);

            this.statusDetailLabel = UIHelper.AddTextLabel(this, 0, 68, 320, UIColour.DefaultText);

            this.powerButton.MouseLeftClick += this.OnOnOffButtonClick;
            this.statusControl.AutoRestartChanged += OnAutoRestartButtonClick;
        }

        public override void Update()
        {
            if (this.building is IOreScanner scanner && this.IsBuildingUiVisible)
            {
                this.statusControl.IsVisible = true;
                this.powerButton.IsVisible = true;
                this.powerButton.IsOn = scanner.IsSwitchedOn;

                if (scanner.EnergyUseRate.KWh != this.energyUseRate)
                {
                    this.energyUseRate = scanner.EnergyUseRate.KWh;
                    this.powerButton.EnergyOutput = -this.energyUseRate.Value;
                    this.powerButtonTooltip.SetTitle(GetString(StringsForThingPanels.EnergyUsekW, this.energyUseRate));
                }

                this.UpdateStatusControl(scanner);

                if (scanner.IsSwitchedOn && scanner.FactoryStatus != FactoryStatus.ScanComplete)
                {
                    this.statusDetailLabel.Text = GetString(StringsForThingPanels.OreScannerCurrent, scanner.CurrentTileCount, scanner.CurrentRadius);
                }
                else
                {
                    this.statusDetailLabel.Text = "";
                }
            }
            else
            {
                this.powerButton.IsVisible = false;
                this.statusControl.IsVisible = false;
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.powerButtonTooltip.SetText(GetString(StringsForThingPanels.ClickToTogglePower));
            base.HandleLanguageChange();
        }

        private void UpdateStatusControl(IOreScanner scanner)
        {
            this.statusControl.ProgressFraction = scanner.Progress;
            this.statusControl.IsAutoRestartEnabled = scanner.IsAutoRestartEnabled;

            switch (scanner.FactoryStatus)
            {
                case FactoryStatus.Offline:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Offline, UIColour.RedText);
                    break;
                case FactoryStatus.Paused:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Paused, UIColour.OrangeText);
                    break;
                case FactoryStatus.InProgress:
                    this.statusControl.SetStatus(BuildingDisplayStatus.InProgress, UIColour.GreenText);
                    break;
                case FactoryStatus.NoPower:
                    this.statusControl.SetStatus(BuildingDisplayStatus.NoPower, UIColour.RedText);
                    break;
                case FactoryStatus.ScanComplete:
                    this.statusControl.SetStatus(BuildingDisplayStatus.ScanComplete, UIColour.RedText);
                    break;
            }

            this.statusControl.SetTimeRemaining(scanner.FactoryStatus == FactoryStatus.InProgress ? scanner.TimeRemainingFrames : 0);
        }

        private void OnOnOffButtonClick(object sender, MouseEventArgs e)
        {
            this.powerButton.IsOn = !this.powerButton.IsOn;
            if (this.building is IOreScanner scanner && scanner.IsSwitchedOn != this.powerButton.IsOn) scanner.TogglePower();
        }

        private void OnAutoRestartButtonClick(object sender, MouseEventArgs e)
        {
            if (this.building is IAutoRestartable ar && ar.IsAutoRestartEnabled != this.statusControl.IsAutoRestartEnabled) ar.ToggleAutoRestart();
        }
    }
}
