namespace SigmaDraconis.UI
{
    using System;
    using Draconis.Shared;
    using Draconis.UI;
    using Shared;
    using Language;
    using WorldInterfaces;

    public class FactoryBuildingPanel : BuildingPanel, IThingPanel
    {
        protected IPowerButton powerButton;
        protected readonly BuildingStatusControl statusControl;
        protected readonly BuildingMaintenanceControl maintenanceControl;
        protected readonly SimpleTooltip powerButtonTooltip;
        protected InventoryTargetControl inventoryTargetControl;

        private double? energyUseRate;
        protected int unscaledHeight;

        public FactoryBuildingPanel(IUIElement parent, int y, bool isEnergyConsumer = false, bool canAutoRestart = true, int unscaledHeight = 130)
            : base(parent, y, Scale(unscaledHeight))
        {
            this.unscaledHeight = unscaledHeight;

            this.AddPowerButton(isEnergyConsumer);

            this.maintenanceControl = new BuildingMaintenanceControl(this, Scale(38), Scale(68));
            this.AddChild(this.maintenanceControl);
            this.maintenanceControl.PriorityChanged += this.OnRepairPriorityChanged;

            this.statusControl = new BuildingStatusControl(this, Scale(38), Scale(44), Scale(248), Scale(20), false, canAutoRestart);
            this.AddChild(this.statusControl);

            if (this.powerButton is PowerButtonWithUsageDisplay)
            {
                this.powerButtonTooltip = UIHelper.AddSimpleTooltip(this, this.powerButton, "", GetString(StringsForThingPanels.ClickToTogglePower));
            }

            this.powerButton.MouseLeftClick += this.OnOnOffButtonClick;
            this.statusControl.AutoRestartChanged += this.OnAutoRestartButtonClick;
        }

        public virtual void SetInventoryTarget(ItemType producedItemType, int defaultTarget)
        {
            if (this.inventoryTargetControl != null) return;

            this.inventoryTargetControl = new InventoryTargetControl(this, Scale(8), Scale(16), producedItemType, defaultTarget);
            this.AddChild(this.inventoryTargetControl);

            this.inventoryTargetControl.IsTargetEnabledChanged += this.OnIsInventoryTargetEnabledChanged;
            this.inventoryTargetControl.TargetValueChanged += this.OnInventoryTargetValueChanged;
            this.inventoryTargetControl.TargetActionOnCompleteChanged += this.OnInventoryTargetActionOnCompleteChanged;
        }

        protected virtual void AddPowerButton(bool isEnergyConsumer)
        {
            this.powerButton = isEnergyConsumer ? (IPowerButton)new PowerButtonWithUsageDisplay(this, 0, Scale(16)) : new PowerButton(this, 0, Scale(16));
            this.powerButton.X = Scale(312) - this.powerButton.W;
            this.AddChild(this.powerButton);
        }

        public override void Update()
        {
            if (this.building is IFactoryBuilding building && this.IsBuildingUiVisible)
            {
                this.statusControl.IsVisible = true;
                this.powerButton.IsVisible = true;
                this.powerButton.IsOn = building.IsSwitchedOn;

                if (this.powerButton is PowerButtonWithUsageDisplay p && this.building is IEnergyConsumer c && c.EnergyUseRate.KWh != this.energyUseRate)
                {
                    this.energyUseRate = c.EnergyUseRate.KWh;
                    p.EnergyOutput = -this.energyUseRate.Value;
                    this.powerButtonTooltip.SetTitle(GetString(StringsForThingPanels.EnergyUsekW, this.energyUseRate));
                }

                if (this.building is IRepairableThing rp && this.building.IsReady)
                {
                    this.maintenanceControl.IsVisible = true;
                    this.maintenanceControl.MaintenanceLevel = rp.MaintenanceLevel;
                    this.maintenanceControl.RepairPriority = rp.RepairPriority;
                }
                else
                {
                    this.maintenanceControl.IsVisible = false;
                }

                this.UpdateStatusControl(building);

                if (this.inventoryTargetControl != null)
                {
                    this.inventoryTargetControl.IsVisible = true;
                    if (building.InventoryTarget.HasValue)
                    {
                        this.inventoryTargetControl.IsTargetEnabled = true;
                        this.inventoryTargetControl.TargetValue = building.InventoryTarget.Value;
                        this.inventoryTargetControl.IsStopOnComplete = building.InventoryTargetShutdown;
                    }
                    else
                    {
                        this.inventoryTargetControl.IsTargetEnabled = false;
                    }
                }
            }
            else
            {
                this.powerButton.IsVisible = false;
                this.maintenanceControl.IsVisible = false;
                this.statusControl.IsVisible = false;
                if (this.inventoryTargetControl != null) this.inventoryTargetControl.IsVisible = false;
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            if (this.powerButtonTooltip != null) this.powerButtonTooltip.SetText(GetString(StringsForThingPanels.ClickToTogglePower));
            base.HandleLanguageChange();
        }

        public override void ApplyLayout()
        {
            base.ApplyLayout();
            if (this.powerButton != null) this.powerButton.X = Scale(312) - this.powerButton.W;
        }

        protected virtual void UpdateStatusControl(IFactoryBuilding building)
        {
            this.statusControl.ProgressFraction = building.FactoryProgress;
            this.statusControl.IsAutoRestartEnabled = building.IsAutoRestartEnabled;

            switch (building.FactoryStatus)
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
                case FactoryStatus.InProgress:
                case FactoryStatus.InProgressReverse:
                    this.statusControl.SetStatus(BuildingDisplayStatus.InProgress, UIColour.GreenText);
                    break;
                case FactoryStatus.Pausing:
                    this.statusControl.SetStatus(BuildingDisplayStatus.Pausing, UIColour.OrangeText);
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
                case FactoryStatus.NoPower:
                    this.statusControl.SetStatus(BuildingDisplayStatus.NoPower, UIColour.RedText);
                    break;
                case FactoryStatus.TooCold:
                    this.statusControl.SetStatus(BuildingDisplayStatus.TooCold, UIColour.RedText);
                    break;
                case FactoryStatus.NoResource:
                    // Only applies to mush churn for now
                    this.statusControl.SetStatus(BuildingDisplayStatus.NotEnoughWater, UIColour.RedText);
                    break;
            }

            this.statusControl.SetTimeRemaining(building.FactoryStatus == FactoryStatus.InProgress ? building.FramesRemaining : 0);
        }

        private void OnOnOffButtonClick(object sender, MouseEventArgs e)
        {
            this.powerButton.IsOn = !this.powerButton.IsOn;
            if (this.building is IFactoryBuilding factory && factory.IsSwitchedOn != this.powerButton.IsOn) factory.TogglePower();
        }

        private void OnAutoRestartButtonClick(object sender, MouseEventArgs e)
        {
            if (this.building is IAutoRestartable ar && ar.IsAutoRestartEnabled != this.statusControl.IsAutoRestartEnabled) ar.ToggleAutoRestart();
        }

        private void OnRepairPriorityChanged(object sender, MouseEventArgs e)
        {
            if (this.building is IRepairableThing rt)
            {
                rt.RepairPriority = this.maintenanceControl.RepairPriority;
            }
        }

        private void OnIsInventoryTargetEnabledChanged(object sender, EventArgs e)
        {
            if (this.building is IFactoryBuilding factory) factory.SetInventoryTarget(this.inventoryTargetControl.IsTargetEnabled ? this.inventoryTargetControl.TargetValue : (int?)null, this.inventoryTargetControl.IsStopOnComplete);
        }

        private void OnInventoryTargetValueChanged(object sender, EventArgs e)
        {
            if (this.building is IFactoryBuilding factory) factory.SetInventoryTarget(this.inventoryTargetControl.IsTargetEnabled ? this.inventoryTargetControl.TargetValue : (int?)null, this.inventoryTargetControl.IsStopOnComplete);
        }

        private void OnInventoryTargetActionOnCompleteChanged(object sender, EventArgs e)
        {
            if (this.building is IFactoryBuilding factory) factory.SetInventoryTarget(this.inventoryTargetControl.IsTargetEnabled ? this.inventoryTargetControl.TargetValue : (int?)null, this.inventoryTargetControl.IsStopOnComplete);
        }
    }
}
