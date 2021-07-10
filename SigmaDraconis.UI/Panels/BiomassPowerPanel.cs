namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class BiomassPowerPanel : GeneratorPanelBase, IThingPanel
    {
        private readonly LeftRightPicker burnRatePicker;
        private readonly TickBoxIconButton allowOrganicsButton;
        private readonly TickBoxIconButton allowMushButton;
        private readonly SimpleTooltip allowOrganicsButtonTooltip;
        private readonly SimpleTooltip allowMushButtonTooltip;
        private readonly FlowDiagramPowerStation flowDiagramOrganics;
        private readonly FlowDiagramPowerStation flowDiagramMush;

        private ThingType currentThingType = ThingType.None;

        public BiomassPowerPanel(IUIElement parent, int y)
            : base(parent, y, Scale(146))
        {
            this.statusControl = new BuildingStatusControl(this, 0, Scale(44), Scale(248), Scale(20), false, true);
            this.AddChild(this.statusControl);
            this.statusControl.AutoRestartChanged += OnAutoRestartButtonClick;

            var burnRateSettings = new List<string> { "Placeholder1", "Placeholder2", "Placeholder3" };
            this.burnRatePicker = new LeftRightPicker(this, Scale(8), Scale(16), Scale(192), burnRateSettings, 0) { IsVisible = false };
            this.burnRatePicker.SelectedIndexChanged += this.OnBurnRatePickerSelectedIndexChanged;
            this.AddChild(this.burnRatePicker);

            this.allowOrganicsButton = new TickBoxIconButton(this, Scale(262), Scale(44), Scale(50), Scale(22), new Icon("Textures\\Icons\\Items", 13), (int)ItemType.Biomass - 1) { IsTicked = true };
            this.allowOrganicsButton.MouseLeftClick += this.OnAllowOrganicsButtonClick;
            this.AddChild(this.allowOrganicsButton);

            this.allowMushButton = new TickBoxIconButton(this, Scale(262), Scale(68), Scale(50), Scale(22), new Icon("Textures\\Icons\\Items", 13), (int)ItemType.Mush - 1) { IsTicked = true };
            this.allowMushButton.MouseLeftClick += this.OnAllowMushButtonClick;
            this.AddChild(this.allowMushButton);

            this.allowOrganicsButtonTooltip = UIHelper.AddSimpleTooltip(this, this.allowOrganicsButton);
            this.allowMushButtonTooltip = UIHelper.AddSimpleTooltip(this, this.allowMushButton);

            this.maintenanceControl = new BuildingMaintenanceControl(this, 0, Scale(68));
            this.AddChild(this.maintenanceControl);
            this.maintenanceControl.PriorityChanged += this.OnRepairPriorityChanged;

            var energyOrganics = Constants.BiomassPowerEnergyOutput * Constants.BiomassPowerFramesToProcessOrganics / Constants.FramesPerHour;
            var waterOrganics = energyOrganics * Constants.BiomassPowerWaterUsePerKwH / 100.0;
            this.flowDiagramOrganics = new FlowDiagramPowerStation(this, 0, Scale(100), this.W, ItemType.Biomass, Constants.BiomassPowerFramesToProcessOrganics, waterOrganics, energyOrganics);
            this.AddChild(this.flowDiagramOrganics);

            var energyMush = Constants.BiomassPowerEnergyOutput * Constants.BiomassPowerFramesToProcessMush / Constants.FramesPerHour;
            var waterMush = energyMush * Constants.BiomassPowerWaterUsePerKwH / 100.0; ;
            this.flowDiagramMush = new FlowDiagramPowerStation(this, 0, Scale(122), this.W, ItemType.Mush, Constants.BiomassPowerFramesToProcessMush, waterMush, energyMush);
            this.AddChild(this.flowDiagramMush);

            this.suppressDeconstructConduitNode = true;   // No room for this
        }

        public override void Update()
        {
            if (this.building is IBiomassPower powerPlant && this.IsBuildingUiVisible)
            {
                this.powerButton.IsVisible = true;
                this.flowDiagramOrganics.IsVisible = true;
                this.flowDiagramMush.IsVisible = true;
                this.statusControl.IsVisible = true;

                this.statusControl.ProgressFraction = powerPlant.FactoryProgress;
                this.statusControl.IsAutoRestartEnabled = powerPlant.IsAutoRestartEnabled;

                this.powerButton.IsOn = powerPlant.IsSwitchedOn;
                this.powerButton.EnergyOutput = powerPlant.EnergyGenRate.KWh;

                this.flowDiagramOrganics.Frames = Constants.BiomassPowerFramesToProcessOrganics * 3 / powerPlant.BurnRateSetting;
                this.flowDiagramMush.Frames = Constants.BiomassPowerFramesToProcessMush * 3 / powerPlant.BurnRateSetting;

                this.flowDiagramOrganics.IsEnabled = powerPlant.AllowBurnOrganics;
                this.flowDiagramMush.IsEnabled = powerPlant.AllowBurnMush;

                var index = powerPlant.BurnRateSetting > 0 ? powerPlant.BurnRateSetting - 1 : 2;
                if (this.building.ThingType != this.currentThingType)
                {
                    var options = new List<string>();
                    for (int i = 1; i <= 3; i++) options.Add(GetString(StringsForThingPanels.TargetOutputKW, powerPlant.BurnRateSettingToKW(i)));
                    this.burnRatePicker.UpdateOptions(options, index);
                    this.currentThingType = this.building.ThingType;
                }
                else this.burnRatePicker.SelectedIndex = index;

                this.burnRatePicker.IsVisible = true;
                this.allowOrganicsButton.IsVisible = true;
                this.allowMushButton.IsVisible = true;
                this.allowOrganicsButtonTooltip.SetTitle($"{GetString(StringsForThingPanels.BurnOrganics)}: {GetString(this.allowOrganicsButton.IsTicked ? StringsForThingPanels.Allowed : StringsForThingPanels.NotAllowed)}");
                this.allowMushButtonTooltip.SetTitle($"{GetString(StringsForThingPanels.BurnMush)}: {GetString(this.allowMushButton.IsTicked ? StringsForThingPanels.Allowed : StringsForThingPanels.NotAllowed)}");

                this.allowMushButton.IsTicked = powerPlant.AllowBurnMush;
                this.allowOrganicsButton.IsTicked = powerPlant.AllowBurnOrganics;

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
                this.burnRatePicker.IsVisible = false;
                this.statusControl.IsVisible = false;
                this.maintenanceControl.IsVisible = false;
                this.allowOrganicsButton.IsVisible = false;
                this.allowMushButton.IsVisible = false;
                this.flowDiagramOrganics.IsVisible = false;
                this.flowDiagramMush.IsVisible = false;
            }

            base.Update();
        }

        private void OnBurnRatePickerSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.building is IPowerPlant pp) pp.BurnRateSetting = this.burnRatePicker.SelectedIndex + 1;
        }

        private void OnAllowMushButtonClick(object sender, MouseEventArgs e)
        {
            if (this.building is IBiomassPower bp)
            {
                bp.AllowBurnMush = !bp.AllowBurnMush;
            }
        }

        private void OnAllowOrganicsButtonClick(object sender, MouseEventArgs e)
        {
            if (this.building is IBiomassPower bp)
            {
                bp.AllowBurnOrganics = !bp.AllowBurnOrganics;
            }
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
