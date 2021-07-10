namespace SigmaDraconis.UI
{
    using System;
    using Draconis.Shared;
    using Draconis.UI;
    using Config;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class WaterPumpPanel : FactoryBuildingPanel
    {
        private readonly SpeedDisplay extractionRateDisplay;
        private readonly WaterDisplay storageDisplay;
        private readonly SimpleTooltip extractionRateTooltip;
        private readonly SimpleTooltip storageDisplayTooltip;
        private readonly TickBoxTextButton storageEnabledButton;
        protected readonly TemperatureDisplay temperatureDisplay;
        protected readonly SimpleTooltip temperatureTooltip;

        private int? extractionRate;
        private int? waterGenRate;
        private int? currentWater;
        private int? maxWater;

        public WaterPumpPanel(IUIElement parent, int y) : base(parent, y, true, false)
        {
            this.temperatureDisplay = new TemperatureDisplay(this, Scale(8), Scale(16)) { IsVisible = false };
            this.AddChild(this.temperatureDisplay);

            this.temperatureTooltip = UIHelper.AddSimpleTooltip(this, this.temperatureDisplay);

            this.storageEnabledButton = new TickBoxTextButton(this, Scale(94), StringsForThingPanels.StorageEnabled);
            this.AddChild(this.storageEnabledButton);
            this.storageEnabledButton.MouseLeftClick += this.OnStorageEnabledButtonClick;

            this.extractionRateDisplay = new SpeedDisplay(this, Scale(170), Scale(16));
            this.AddChild(this.extractionRateDisplay);

            this.storageDisplay = new WaterDisplay(this, Scale(64), Scale(16), 102);
            this.AddChild(this.storageDisplay);

            this.extractionRateTooltip = UIHelper.AddSimpleTooltip(this, this.extractionRateDisplay);
            this.storageDisplayTooltip = UIHelper.AddSimpleTooltip(this, this.storageDisplay);
        }

        protected override void AddPowerButton(bool isEnergyConsumer)
        {
            this.powerButton = new PowerButtonWithUsageDisplay(this, 0, Scale(16), 86, true);
            this.powerButton.X = Scale(312) - this.powerButton.W;
            this.AddChild(this.powerButton);
        }

        public override void Update()
        {
            base.Update();

            var pump = this.IsBuildingUiVisible && this.building?.IsReady == true ? this.building as IWaterProviderBuilding : null;
            if (pump == null)
            {
                this.storageEnabledButton.IsVisible = false;
                this.extractionRateDisplay.IsVisible = false;
                this.storageDisplay.IsVisible = false;
                this.temperatureDisplay.IsVisible = false;
                this.temperatureTooltip.IsEnabled = false;
                return;
            }

            this.storageEnabledButton.IsVisible = true;
            this.extractionRateDisplay.IsVisible = true;
            this.storageDisplay.IsVisible = true;

            var definition = ThingTypeManager.GetDefinition(building.ThingType);
            this.temperatureDisplay.IsVisible = true;
            this.temperatureTooltip.IsEnabled = true;
            var temperature = (int)Math.Round((building as IFactoryBuilding).Temperature);
            this.temperatureDisplay.SetTemperature(temperature, temperature >= definition.MinTemperature ? UIColour.GreenText : UIColour.RedText);
            this.temperatureTooltip.SetTitle($"{GetString(StringsForThingPanels.MinOperatingTemperature)}: {LanguageHelper.FormatTemperature(definition.MinTemperature)}");

            if (pump is IWaterPump wp)
            {
                this.extractionRateTooltip.IsEnabled = true;
                if (wp.ExtractionRate != this.extractionRate)
                {
                    this.extractionRate = wp?.ExtractionRate;
                    this.extractionRateTooltip.SetTitle(GetString(StringsForThingPanels.GroundwaterPercent, this.extractionRate.Value));
                }
            }
            else this.extractionRateTooltip.SetTitle("");

            if (pump.WaterGenRate != this.waterGenRate)
            {
                this.waterGenRate = pump?.WaterGenRate;
                this.extractionRateDisplay.IsVisible = this.waterGenRate.HasValue;
                this.extractionRateDisplay.Speed = this.waterGenRate.GetValueOrDefault() / (pump.ThingType == ThingType.ShorePump ? 4 : 2);
                this.extractionRateTooltip.SetText(GetString(StringsForThingPanels.ExtractionRatePerHour, this.waterGenRate / 100M));
            }

            var silo = pump as ISilo;
            if (silo?.StorageLevel != this.currentWater || silo?.StorageCapacity != this.maxWater)
            {
                this.currentWater = silo?.StorageLevel;
                this.maxWater = silo?.StorageCapacity;
                this.storageDisplay.IsVisible = this.currentWater.HasValue;
                this.storageDisplay.SetWater(this.currentWater.GetValueOrDefault() / 100M, this.maxWater.GetValueOrDefault() / 100M);
                this.storageDisplayTooltip.SetTitle(GetString(StringsForThingPanels.WaterStored, this.currentWater.GetValueOrDefault() / 100M, this.maxWater.GetValueOrDefault() / 100M));
            }
        }

        protected override void OnBuildingChanged()
        {
            var silo = this.building?.IsReady == true ? this.building as ISilo : null;
            if (silo == null) return;

            if (this.storageEnabledButton.IsTicked != silo.IsSiloSwitchedOn) this.storageEnabledButton.IsTicked = silo.IsSiloSwitchedOn;
            base.OnBuildingChanged();
        }


        protected override void UpdateStatusControl(IFactoryBuilding building)
        {
            this.statusControl.ProgressFraction = (building.FactoryStatus == FactoryStatus.InProgress || building.FactoryProgress > 0.01) ? 1.0 : 0.0;

            switch (building.FactoryStatus)
            {
                case FactoryStatus.Offline:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Offline, UIColour.RedText, UIColour.BuildingWorkBar);
                    break;
                case FactoryStatus.Initialising:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Initialising, UIColour.OrangeText, UIColour.BuildingWorkBar);
                    break;
                case FactoryStatus.Standby:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Standby, UIColour.OrangeText, UIColour.BuildingWorkBar);
                    break;
                case FactoryStatus.InProgress:
                    this.statusControl.SetStatus(BuildingDisplayStatus.InProgress, UIColour.GreenText, UIColour.GreenText);
                    break;
                case FactoryStatus.Pausing:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Pausing, UIColour.OrangeText, UIColour.BuildingWorkBar);
                    break;
                case FactoryStatus.Paused:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Paused, UIColour.OrangeText, UIColour.BuildingWorkBar);
                    break;
                case FactoryStatus.Resuming:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Resuming, UIColour.OrangeText, UIColour.BuildingWorkBar);
                    break;
                case FactoryStatus.WaitingToDistribute:
                    this.statusControl.SetStatus(BuildingDisplayStatus.StorageFull, UIColour.OrangeText, UIColour.BuildingWorkBar);
                    break;
                case FactoryStatus.Broken:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Broken, UIColour.RedText, UIColour.BuildingWorkBar);
                    break;
                case FactoryStatus.NoPower:
                    this.statusControl.SetStatus(BuildingDisplayStatus.NoPower, UIColour.RedText, UIColour.BuildingWorkBar);
                    break;
                case FactoryStatus.TooCold:
                    this.statusControl.SetStatus(BuildingDisplayStatus.TooCold, UIColour.RedText, UIColour.BuildingWorkBar);
                    break;
                case FactoryStatus.TooDry:
                    this.statusControl.SetStatus(BuildingDisplayStatus.TooDry, UIColour.RedText, UIColour.BuildingWorkBar);
                    break;
            }
        }

        protected override void UpdateDeconstructButtons()
        {
            base.UpdateDeconstructButtons();

            if (this.deconstructConduitNodeButton.IsVisible || this.deconstructFoundationButton.IsVisible)
            {
                this.deconstructConduitNodeButton.Y = this.H - Scale(22);
                this.deconstructFoundationButton.Y = this.H - Scale(22);
                this.statusControl.Y = Scale(36);
                this.maintenanceControl.Y = Scale(60);
                this.storageEnabledButton.Y = Scale(84);
            }
            else
            {
                this.statusControl.Y = Scale(40);
                this.maintenanceControl.Y = Scale(64);
                this.storageEnabledButton.Y = Scale(94);
            }
        }

        private void OnStorageEnabledButtonClick(object sender, MouseEventArgs e)
        {
            this.storageEnabledButton.IsTicked = !this.storageEnabledButton.IsTicked;

            var silo = this.building as ISilo;
            if (this.storageEnabledButton.IsTicked)
            {
                silo.IsSiloSwitchedOn = true;
                silo.SiloStatus = SiloStatus.Online;
            }
            else
            {
                silo.IsSiloSwitchedOn = false;
                silo.SiloStatus = silo.StorageLevel > 0 ? SiloStatus.WaitingToDistribute : SiloStatus.Offline;
            }
        }
    }
}
