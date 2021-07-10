namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class HydrogenBurnerPanel : GeneratorPanelBase, IThingPanel
    {
        private readonly LeftRightPicker burnRatePicker;
        private ThingType currentThingType = ThingType.None;

        public HydrogenBurnerPanel(IUIElement parent, int y) : base(parent, y)
        {
            var burnRateSettings = new List<string> { "Placeholder1", "Placeholder2", "Placeholder3" };
            this.burnRatePicker = new LeftRightPicker(this, Scale(8), Scale(16), Scale(192), burnRateSettings, 0) { IsVisible = false };
            this.burnRatePicker.SelectedIndexChanged += this.OnBurnRatePickerSelectedIndexChanged;
            this.AddChild(this.burnRatePicker);

            this.statusControl = new BuildingStatusControl(this, Scale(36), Scale(42), Scale(248), Scale(20), false, true);
            this.AddChild(this.statusControl);
            this.statusControl.AutoRestartChanged += OnAutoRestartButtonClick;

            this.maintenanceControl = new BuildingMaintenanceControl(this, Scale(38), Scale(68));
            this.AddChild(this.maintenanceControl);
            this.maintenanceControl.PriorityChanged += this.OnRepairPriorityChanged;

            var energy = Constants.HydrogenBurnerEnergyOutput * Constants.HydrogenBurnerFramesToProcess / Constants.FramesPerHour;
            var water = energy * Constants.HydrogenBurnerWaterUsePerKwH / 100.0;
            this.flowDiagram = new FlowDiagramPowerStation(this, 0, Scale(106), this.W, ItemType.LiquidFuel, Constants.HydrogenBurnerFramesToProcess, water, energy);
            this.AddChild(this.flowDiagram);
        }

        public override void Update()
        {
            if (this.building is IPowerPlant powerPlant && this.IsBuildingUiVisible)
            {
                this.powerButton.IsVisible = true;
                this.burnRatePicker.IsVisible = true;
                this.flowDiagram.IsVisible = true;
                this.statusControl.IsVisible = true;

                this.statusControl.ProgressFraction = powerPlant.FactoryProgress;
                this.statusControl.IsAutoRestartEnabled = powerPlant.IsAutoRestartEnabled;

                this.powerButton.IsOn = powerPlant.IsSwitchedOn;
                this.powerButton.EnergyOutput = powerPlant.EnergyGenRate.KWh;

                if (this.flowDiagram is FlowDiagramPowerStation f) f.Frames = (int)(Constants.HydrogenBurnerFramesToProcess * Constants.HydrogenBurnerEnergyOutput / powerPlant.BurnRateSettingToKW(powerPlant.BurnRateSetting));

                var index = powerPlant.BurnRateSetting > 0 ? powerPlant.BurnRateSetting - 1 : 2;
                if (this.building.ThingType != this.currentThingType)
                {
                    var options = new List<string>();
                    for (int i = 1; i <= 3; i++) options.Add(GetString(StringsForThingPanels.TargetOutputKW, powerPlant.BurnRateSettingToKW(i)));
                    this.burnRatePicker.UpdateOptions(options, index);
                    this.currentThingType = this.building.ThingType;
                }
                else this.burnRatePicker.SelectedIndex = index;

                this.maintenanceControl.IsVisible = true;
                this.maintenanceControl.MaintenanceLevel = powerPlant.MaintenanceLevel;
                this.maintenanceControl.RepairPriority = powerPlant.RepairPriority;

                switch (powerPlant.FactoryStatus)
                {
                    case FactoryStatus.Offline:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Offline, UIColour.RedText);
                        break;
                    case FactoryStatus.Initialising:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Initialising, UIColour.OrangeText);
                        break;
                    case FactoryStatus.Standby:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Standby, UIColour.OrangeText);
                        break;
                    case FactoryStatus.Starting:
                    case FactoryStatus.InProgress:
                        this.statusControl.SetStatus(BuildingDisplayStatus.InProgress, UIColour.GreenText);
                        break;
                    case FactoryStatus.Pausing:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Pausing, UIColour.OrangeText);
                        break;
                    case FactoryStatus.Stopping:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Stopping, UIColour.OrangeText);
                        break;
                    case FactoryStatus.Paused:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Paused, UIColour.OrangeText);
                        break;
                    case FactoryStatus.Resuming:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Resuming, UIColour.OrangeText);
                        break;
                    case FactoryStatus.WaitingToDistribute:
                        this.statusControl.SetStatus(BuildingDisplayStatus.SilosFull, UIColour.OrangeText);
                        break;
                    case FactoryStatus.Broken:
                        this.statusControl.SetStatus(BuildingDisplayStatus.Broken, UIColour.RedText);
                        break;
                }
            }
            else
            {
                this.powerButton.IsVisible = false;
                this.statusControl.IsVisible = false;
                this.maintenanceControl.IsVisible = false;
                this.burnRatePicker.IsVisible = false;
                this.flowDiagram.IsVisible = false;
            }

            base.Update();
        }

        private void OnBurnRatePickerSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.building is IPowerPlant pp) pp.BurnRateSetting = this.burnRatePicker.SelectedIndex + 1;
        }

        private void OnRepairPriorityChanged(object sender, MouseEventArgs e)
        {
            if (this.building is IRepairableThing rt)
            {
                rt.RepairPriority = this.maintenanceControl.RepairPriority;
            }
        }

        private void OnAutoRestartButtonClick(object sender, MouseEventArgs e)
        {
            if (this.building is IFactoryBuilding factory && factory.IsAutoRestartEnabled != this.statusControl.IsAutoRestartEnabled) factory.ToggleAutoRestart();
        }
    }
}
