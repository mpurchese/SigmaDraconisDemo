namespace SigmaDraconis.UI
{
    using System;
    using System.Linq;
    using System.Text;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Settings;
    using Shared;
    using World.Projects;
    using WorldInterfaces;

    public class AlgaePoolPanel : BuildingPanel, IThingPanel
    {
        private readonly SpeedDisplay speedDisplay;
        private readonly LightDisplay lightDisplay;
        private readonly TemperatureDisplay temperatureDisplay;
        private readonly TextLabel statusLabel;
        private readonly ProgressBar progressBar;
        private readonly TickBoxTextButton autoFillButton;
        private readonly TickBoxTextButton autoHarvestButton;
        private readonly TextButton fillButton;
        private readonly TextButton drainButton;
        private readonly TextButton harvestButton;
        private readonly HorizontalStack buttonStack1;
        private readonly HorizontalStack buttonStack2;
        private readonly GrowthRateTooltip growthRateTooltip;
        private readonly SimpleTooltip temperatureTooltip;
        private readonly InventoryTargetControl inventoryTargetControl;

        private TemperatureUnit displayedTemperatureUnit;

        public AlgaePoolPanel(IUIElement parent, int y)
            : base(parent, y)
        {
            this.speedDisplay = this.AddChild(new SpeedDisplay(this, Scale(262), Scale(16)));
            this.growthRateTooltip = new GrowthRateTooltip(this, this.speedDisplay);
            TooltipParent.Instance.AddChild(this.growthRateTooltip);

            this.lightDisplay = this.AddChild(new LightDisplay(this, Scale(262), Scale(38)));

            this.displayedTemperatureUnit = SettingsManager.TemperatureUnit;
            this.temperatureDisplay = this.AddChild(new TemperatureDisplay(this, Scale(262), Scale(60)) { IsVisible = false });
            this.temperatureTooltip = UIHelper.AddSimpleTooltip(this, this.temperatureDisplay, "", GetTemperatureTooltipText());

            this.statusLabel = new TextLabel(this, 0, Scale(38), Scale(262), Scale(20), LanguageManager.Get<BuildingDisplayStatus>(BuildingDisplayStatus.Offline), UIColour.DefaultText);
            this.progressBar = new ProgressBar(this, Scale(30), Scale(52), Scale(200), Scale(4)) { BarColour = UIColour.ProgressBar };
            this.AddChild(this.statusLabel);
            this.AddChild(this.progressBar);

            this.buttonStack1 = new HorizontalStack(this, 0, Scale(58), Scale(262), Scale(20), TextAlignment.MiddleCentre);
            this.AddChild(this.buttonStack1);

            this.buttonStack2 = new HorizontalStack(this, 0, Scale(80), Scale(262), Scale(20), TextAlignment.MiddleCentre);
            this.AddChild(this.buttonStack2);

            this.fillButton = UIHelper.AddTextButton(this.buttonStack1, 0, 0, 74, GetString(StringsForThingPanels.Fill));
            this.fillButton.MouseLeftClick += this.OnFillButtonClick;

            this.drainButton = UIHelper.AddTextButton(this.buttonStack1, 0, 0, 74, GetString(StringsForThingPanels.Drain));
            this.drainButton.MouseLeftClick += this.OnDrainButtonClick;

            this.harvestButton = UIHelper.AddTextButton(this.buttonStack1, 0, 0, 74, GetString(StringsForThingPanels.Harvest));
            this.harvestButton.MouseLeftClick += this.OnHarvestButtonClick;

            var autoFillStr = GetString(StringsForThingPanels.AutoFill);
            var autoHarvestStr = GetString(StringsForThingPanels.AutoHarvest);

            this.autoFillButton = new TickBoxTextButton(this.buttonStack2, 0, 0, Scale(28 + (autoFillStr.Length * 7)), Scale(20), autoFillStr);
            this.autoFillButton.MouseLeftClick += this.OnAutoFillButtonClick;
            this.buttonStack2.AddChild(this.autoFillButton);

            this.autoHarvestButton = new TickBoxTextButton(this.buttonStack2, 0, 0, Scale(28 + (autoHarvestStr.Length * 7)), Scale(20), autoHarvestStr);
            this.autoHarvestButton.MouseLeftClick += this.OnAutoHarvestButtonClick;
            this.buttonStack2.AddChild(this.autoHarvestButton);

            this.inventoryTargetControl = new InventoryTargetControl(this, Scale(8), Scale(16), ItemType.Biomass, 8, false);
            this.AddChild(this.inventoryTargetControl);

            this.inventoryTargetControl.IsTargetEnabledChanged += this.OnIsInventoryTargetEnabledChanged;
            this.inventoryTargetControl.TargetValueChanged += this.OnInventoryTargetValueChanged;
            this.inventoryTargetControl.TargetActionOnCompleteChanged += this.OnInventoryTargetActionOnCompleteChanged;

            this.flowDiagram = new FlowDiagramAlgaePool(this, 0, Scale(106), this.W, (int)(1.0 / Constants.AlgaeGrowthRate), Constants.AlgaePoolWaterUse / 100.0, Constants.AlgaeYield);
            this.AddChild(this.flowDiagram);
        }

        public override void Update()
        {
            if (this.IsBuildingUiVisible && this.Thing is IAlgaePool pool)
            {
                this.autoFillButton.IsTicked = pool.AutoFill;
                this.autoHarvestButton.IsTicked = pool.AutoHarvest;

                this.speedDisplay.IsVisible = true;
                this.temperatureDisplay.IsVisible = true;
                this.statusLabel.IsVisible = true;
                this.progressBar.IsVisible = true;
                this.buttonStack1.IsVisible = true;
                this.buttonStack2.IsVisible = true;
                this.inventoryTargetControl.IsVisible = true;
                this.flowDiagram.IsVisible = true;
                this.lightDisplay.IsVisible = true;

                if (this.IsShown)  // To avoid possible exception when new game is loaded
                {
                    this.fillButton.IsEnabled = pool.CanFill();
                    this.drainButton.IsEnabled = pool.CanDrain();
                    this.harvestButton.IsEnabled = pool.CanHarvest();
                }

                this.speedDisplay.Speed = (int)(pool.GrowthRate * 100.0);

                if (this.flowDiagram is FlowDiagramAlgaePool f)
                {
                    f.OutputQuantity = ProjectManager.GetDefinition(3)?.IsDone == true ? Constants.AlgaeYieldImproved : Constants.AlgaeYield;
                    if (pool.GrowthRate >= 0.01)
                    {
                        f.Frames = (int)(100.0 * Constants.FramesPerHour * Constants.FramesPerHour * Constants.AlgaeGrowthRate / pool.GrowthRate);
                    }
                    else f.Frames = 0;
                }

                var light = WorldLight.GetEffectiveLightPercent(pool.Light);
                if (light == 100) this.lightDisplay.SetValue(light, UIColour.GreenText);
                else if (light > 0) this.lightDisplay.SetValue(light, UIColour.YellowText);
                else this.lightDisplay.SetValue(light, UIColour.RedText);

                var temperature = (int)pool.Temperature;
                if (temperature < 0 || temperature >= 45) this.temperatureDisplay.SetTemperature(temperature, UIColour.RedText);
                else if (temperature < 25 || temperature >= 35) this.temperatureDisplay.SetTemperature(temperature, UIColour.YellowText);
                else this.temperatureDisplay.SetTemperature(temperature, UIColour.GreenText);

                if (this.displayedTemperatureUnit != SettingsManager.TemperatureUnit)
                {
                    this.temperatureTooltip.SetText(GetTemperatureTooltipText());
                    this.displayedTemperatureUnit = SettingsManager.TemperatureUnit;
                }

                var status = pool.FactoryStatus;
                if (pool.IsTooCold && (status == FactoryStatus.InProgress || status == FactoryStatus.NoResource)) this.statusLabel.Text = LanguageManager.Get<BuildingDisplayStatus>(BuildingDisplayStatus.TooCold);
                else if (pool.IsTooHot && (status == FactoryStatus.InProgress || status == FactoryStatus.NoResource)) this.statusLabel.Text = LanguageManager.Get<BuildingDisplayStatus>(BuildingDisplayStatus.TooHot);
                else if (pool.IsTooDark && (status == FactoryStatus.InProgress || status == FactoryStatus.NoResource)) this.statusLabel.Text = LanguageManager.Get<BuildingDisplayStatus>(BuildingDisplayStatus.TooDark);
                else if (status == FactoryStatus.WaitingToDistribute) this.statusLabel.Text = LanguageManager.Get<BuildingDisplayStatus>(BuildingDisplayStatus.SilosFull);
                else if (status == FactoryStatus.NoResource) this.statusLabel.Text = LanguageManager.Get<BuildingDisplayStatus>(BuildingDisplayStatus.NotEnoughWater);
                else this.statusLabel.Text =
                        status == FactoryStatus.InProgress
                        ? LanguageManager.Get<BuildingDisplayStatus>(BuildingDisplayStatus.InProgressPercent, FormatPercent(pool.FactoryProgress, "  0%"))
                        : pool.FactoryStatus.ToString();

                this.progressBar.Fraction = pool.FactoryProgress;

                this.growthRateTooltip.UpdateModifiers(pool.GrowthRateModifiers);
                this.growthRateTooltip.IsEnabled = pool.GrowthRateModifiers.Any();

                if (pool.InventoryTarget.HasValue)
                {
                    this.inventoryTargetControl.IsTargetEnabled = true;
                    this.inventoryTargetControl.TargetValue = pool.InventoryTarget.Value;
                }
                else
                {
                    this.inventoryTargetControl.IsTargetEnabled = false;
                }
            }
            else
            {
                this.speedDisplay.IsVisible = false;
                this.temperatureDisplay.IsVisible = false;
                this.statusLabel.IsVisible = false;
                this.progressBar.IsVisible = false;
                this.statusLabel.IsVisible = false;
                this.buttonStack1.IsVisible = false;
                this.buttonStack2.IsVisible = false;
                this.growthRateTooltip.IsEnabled = false;
                this.inventoryTargetControl.IsVisible = false;
                this.lightDisplay.IsVisible = false;
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.temperatureTooltip.SetText(GetTemperatureTooltipText());

            this.fillButton.Text = GetString(StringsForThingPanels.Fill);
            this.drainButton.Text = GetString(StringsForThingPanels.Drain);
            this.harvestButton.Text = GetString(StringsForThingPanels.Harvest);
            this.autoFillButton.Text = GetString(StringsForThingPanels.AutoFill);
            this.autoHarvestButton.Text = GetString(StringsForThingPanels.AutoHarvest);

            this.autoFillButton.W = Scale(28 + (this.autoFillButton.Text.Length * 7));
            this.autoHarvestButton.W = Scale(28 + (this.autoHarvestButton.Text.Length * 7));
            this.buttonStack2.LayoutInvalidated = true;

            base.HandleLanguageChange();
        }

        private void OnFillButtonClick(object sender, MouseEventArgs e)
        {
            if (this.building is IAlgaePool pool && pool.CanFill()) pool.Fill();
        }

        private void OnDrainButtonClick(object sender, MouseEventArgs e)
        {
            (this.building as IAlgaePool).Drain();
        }

        private void OnHarvestButtonClick(object sender, MouseEventArgs e)
        {
            (this.building as IAlgaePool).Harvest();
        }

        private void OnAutoFillButtonClick(object sender, MouseEventArgs e)
        {
            this.autoFillButton.IsTicked = !this.autoFillButton.IsTicked;
            (this.building as IAlgaePool).AutoFill = this.autoFillButton.IsTicked;
        }

        private void OnAutoHarvestButtonClick(object sender, MouseEventArgs e)
        {
            this.autoHarvestButton.IsTicked = !this.autoHarvestButton.IsTicked;
            (this.building as IAlgaePool).AutoHarvest = this.autoHarvestButton.IsTicked;
        }

        private void OnIsInventoryTargetEnabledChanged(object sender, EventArgs e)
        {
            if (this.building is IAlgaePool factory) factory.SetInventoryTarget(this.inventoryTargetControl.IsTargetEnabled ? this.inventoryTargetControl.TargetValue : (int?)null, this.inventoryTargetControl.IsStopOnComplete);
        }

        private void OnInventoryTargetValueChanged(object sender, EventArgs e)
        {
            if (this.building is IAlgaePool factory) factory.SetInventoryTarget(this.inventoryTargetControl.IsTargetEnabled ? this.inventoryTargetControl.TargetValue : (int?)null, this.inventoryTargetControl.IsStopOnComplete);
        }

        private void OnInventoryTargetActionOnCompleteChanged(object sender, EventArgs e)
        {
            if (this.building is IAlgaePool factory) factory.SetInventoryTarget(this.inventoryTargetControl.IsTargetEnabled ? this.inventoryTargetControl.TargetValue : (int?)null, this.inventoryTargetControl.IsStopOnComplete);
        }

        private static string GetTemperatureTooltipText()
        {
            var sb = new StringBuilder();
            if (SettingsManager.TemperatureUnit == TemperatureUnit.F)
            {
                var unit = LanguageManager.Get<StringsForUnits>(StringsForUnits.F);
                sb.Append(LanguageManager.Get<StringsForCropTooltip>(StringsForCropTooltip.Temperature, 0.ToFahrenheit(), 45.ToFahrenheit(), unit));
                sb.Append("|");
                sb.Append(LanguageManager.Get<StringsForCropTooltip>(StringsForCropTooltip.OptimalTemperature, 25.ToFahrenheit(), 35.ToFahrenheit(), unit));
            }
            else
            {
                var unit = LanguageManager.Get<StringsForUnits>(StringsForUnits.C);
                sb.Append(LanguageManager.Get<StringsForCropTooltip>(StringsForCropTooltip.Temperature, 0, 45, unit));
                sb.Append("|");
                sb.Append(LanguageManager.Get<StringsForCropTooltip>(StringsForCropTooltip.OptimalTemperature, 25, 35, unit));
            }

            return sb.ToString();
        }

        private static string FormatPercent(double? val, string defaultStr)
        {
            return val.HasValue ? string.Format("{0:D0}%", (int)(val * 100.0)) : defaultStr;
        }
    }
}
