namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class GeneratorPanel : GeneratorPanelBase, IThingPanel
    {
        private readonly TickBoxIconButton allowCoalButton;
        private readonly TickBoxIconButton allowOrganicsButton;
        private readonly SimpleTooltip allowCoalButtonTooltip;
        private readonly SimpleTooltip allowOrganicsButtonTooltip;
        private readonly FlowDiagramPowerStation flowDiagramCoal;
        private readonly FlowDiagramPowerStation flowDiagramOrganics;

        public GeneratorPanel(IUIElement parent, int y)
            : base(parent, y, Scale(146))
        {
            this.statusControl = new BuildingStatusControl(this, 0, Scale(44), Scale(248), Scale(20), false, true);
            this.AddChild(this.statusControl);
            this.statusControl.AutoRestartChanged += OnAutoRestartButtonClick;

            this.allowCoalButton = new TickBoxIconButton(this, Scale(262), Scale(44), Scale(50), Scale(22), new Icon("Textures\\Icons\\Items", 13), (int)ItemType.Coal - 1) { IsTicked = true };
            this.allowCoalButton.MouseLeftClick += this.OnAllowCoalButtonClick;
            this.AddChild(this.allowCoalButton);

            this.allowOrganicsButton = new TickBoxIconButton(this, Scale(262), Scale(68), Scale(50), Scale(22), new Icon("Textures\\Icons\\Items", 13), (int)ItemType.Biomass - 1) { IsTicked = true };
            this.allowOrganicsButton.MouseLeftClick += this.OnAllowOrganicsButtonClick;
            this.AddChild(this.allowOrganicsButton);

            this.allowCoalButtonTooltip = UIHelper.AddSimpleTooltip(this, this.allowCoalButton);
            this.allowOrganicsButtonTooltip = UIHelper.AddSimpleTooltip(this, this.allowOrganicsButton);

            this.maintenanceControl = new BuildingMaintenanceControl(this, 0, Scale(68));
            this.AddChild(this.maintenanceControl);
            this.maintenanceControl.PriorityChanged += this.OnRepairPriorityChanged;

            var energyCoal = Constants.GeneratorEnergyOutputCoal * Constants.GeneratorFramesToProcessCoal / Constants.FramesPerHour;
            var waterCoal = energyCoal * Constants.GeneratorWaterUsePerKwH / 100.0;
            this.flowDiagramCoal = new FlowDiagramPowerStation(this, 0, Scale(100), this.W, ItemType.Coal, Constants.GeneratorFramesToProcessCoal, waterCoal, energyCoal);
            this.AddChild(this.flowDiagramCoal);

            var energyOrganics = Constants.GeneratorEnergyOutputOrganics * Constants.GeneratorFramesToProcessOrganics / Constants.FramesPerHour;
            var waterOrganics = energyOrganics * Constants.GeneratorWaterUsePerKwH / 100.0;
            this.flowDiagramOrganics = new FlowDiagramPowerStation(this, 0, Scale(122), this.W, ItemType.Biomass, Constants.GeneratorFramesToProcessOrganics, waterOrganics, energyOrganics);
            this.AddChild(this.flowDiagramOrganics);

            this.suppressDeconstructConduitNode = true;   // No room for this
        }

        public override void Update()
        {
            if (this.building is IGenerator powerPlant && this.IsBuildingUiVisible)
            {
                this.powerButton.IsVisible = true;
                this.flowDiagramCoal.IsVisible = true;
                this.flowDiagramOrganics.IsVisible = true;
                this.statusControl.IsVisible = true;

                this.statusControl.ProgressFraction = powerPlant.FactoryProgress;
                this.statusControl.IsAutoRestartEnabled = powerPlant.IsAutoRestartEnabled;

                this.powerButton.IsOn = powerPlant.IsSwitchedOn;
                this.powerButton.EnergyOutput = powerPlant.EnergyGenRate.KWh;

                this.flowDiagramCoal.IsEnabled = powerPlant.AllowBurnCoal;
                this.flowDiagramOrganics.IsEnabled = powerPlant.AllowBurnOrganics;

                this.allowCoalButton.IsVisible = true;
                this.allowOrganicsButton.IsVisible = true;
                this.allowCoalButtonTooltip.SetTitle($"{GetString(StringsForThingPanels.BurnCoal)}: {GetString(this.allowCoalButton.IsTicked ? StringsForThingPanels.Allowed : StringsForThingPanels.NotAllowed)}");
                this.allowOrganicsButtonTooltip.SetTitle($"{GetString(StringsForThingPanels.BurnOrganics)}: {GetString(this.allowOrganicsButton.IsTicked ? StringsForThingPanels.Allowed : StringsForThingPanels.NotAllowed)}");

                this.allowOrganicsButton.IsTicked = powerPlant.AllowBurnOrganics;
                this.allowCoalButton.IsTicked = powerPlant.AllowBurnCoal;

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
                this.allowCoalButton.IsVisible = false;
                this.allowOrganicsButton.IsVisible = false;
                this.flowDiagramCoal.IsVisible = false;
                this.flowDiagramOrganics.IsVisible = false;
            }

            base.Update();
        }

        private void OnAllowCoalButtonClick(object sender, MouseEventArgs e)
        {
            if (this.building is IGenerator g)
            {
                g.AllowBurnCoal = !g.AllowBurnCoal;
            }
        }

        private void OnAllowOrganicsButtonClick(object sender, MouseEventArgs e)
        {
            if (this.building is IGenerator g)
            {
                g.AllowBurnOrganics = !g.AllowBurnOrganics;
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
