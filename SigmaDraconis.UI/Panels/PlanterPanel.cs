namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Draconis.Shared;
    using Draconis.UI;
    using Config;
    using Language;
    using Settings;
    using Shared;
    using World.Projects;
    using World.Rooms;
    using WorldInterfaces;

    public class PlanterPanel : BuildingPanel, IThingPanel
    {
        private readonly SpeedDisplay speedDisplay;
        private readonly LightDisplay lightDisplay;
        private readonly LeftRightPicker cropPicker;
        private readonly LeftRightPicker priorityPicker;
        private readonly PlanterPanelStatusLabel statusLabel;
        private readonly ProgressBar growthProgressBar;
        private readonly TextLabel healthLabel;
        private readonly ProgressBar healthProgressBar;
        private readonly GrowthRateTooltip growthRateTooltip;
        private readonly CropTooltip cropPickerTooltip;
        private readonly TextLabel currentCropLabel;
        private readonly TemperatureDisplay temperatureDisplay;
        private readonly SimpleTooltip temperatureTooltip;

        private ThingType planterType = ThingType.None;
        private bool canPlantKekke;
        private bool isLanguageChanged;

        public PlanterPanel(IUIElement parent, int y)
            : base(parent, y)
        {
            this.speedDisplay = new SpeedDisplay(this, Scale(8), Scale(16));
            this.AddChild(this.speedDisplay);

            this.statusLabel = new PlanterPanelStatusLabel(this, 0, 38, 320);
            this.growthProgressBar = new ProgressBar(this, Scale(60), Scale(52), Scale(200), Scale(4)) { BarColour = UIColour.ProgressBar };
            this.AddChild(this.statusLabel);
            this.AddChild(this.growthProgressBar);

            this.growthRateTooltip = new GrowthRateTooltip(this, this.speedDisplay);
            TooltipParent.Instance.AddChild(this.growthRateTooltip);

            this.lightDisplay = this.AddChild(new LightDisplay(this, this.W - Scale(116), Scale(16)));

            this.temperatureDisplay = new TemperatureDisplay(this, this.W - Scale(60), Scale(16)) { IsVisible = false };
            this.AddChild(this.temperatureDisplay);

            this.temperatureTooltip = UIHelper.AddSimpleTooltip(this, this.temperatureDisplay);

            this.healthLabel = new TextLabelAutoScaling(this, 0, 56, 320, 20, $"{GetString(StringsForThingPanels.Health)}: ---%", UIColour.DefaultText);
            this.healthProgressBar = new ProgressBar(this, Scale(60), Scale(70), Scale(200), Scale(4)) { BarColour = UIColour.GreenText };
            this.AddChild(this.healthLabel);
            this.AddChild(this.healthProgressBar);

            var cropTypes = (new List<string>() { GetString(StringsForThingPanels.DontPlant) }).Concat(CropDefinitionManager.GetNames().Select(n => $"{GetString(StringsForThingPanels.Next)}: {n}")).ToList();
            this.cropPicker = new LeftRightPicker(this, Scale(4), Scale(78), Scale(140), cropTypes, 1) { IsVisible = false };
            this.cropPicker.SelectedIndexChanged += this.OnCropTypeSelectedIndexChanged;
            this.AddChild(this.cropPicker);

            var priorityOptions = new List<StringsForThingPanels> { StringsForThingPanels.Priority1, StringsForThingPanels.Priority2, StringsForThingPanels.Priority3, StringsForThingPanels.Priority4 };
            this.priorityPicker = new LeftRightEnumPicker<StringsForThingPanels>(this, 150, 78, 166, priorityOptions, 2);
            this.AddChild(this.priorityPicker);

            this.cropPickerTooltip = new CropTooltip(TooltipParent.Instance, this.cropPicker, null);
            TooltipParent.Instance.AddChild(this.cropPickerTooltip);

            this.currentCropLabel = UIHelper.AddTextLabel(this, 0, 102, 320, UIColour.DefaultText);
        }

        public override void Update()
        {
            if (this.IsBuildingUiVisible && this.Thing is IPlanter planter)
            {
                this.speedDisplay.IsVisible = true;
                this.cropPicker.IsVisible = true;
                this.priorityPicker.IsVisible = true;
                this.growthProgressBar.IsVisible = true;
                this.statusLabel.IsVisible = true;
                this.healthProgressBar.IsVisible = true;
                this.healthLabel.IsVisible = true;
                this.currentCropLabel.IsVisible = true;
                this.temperatureDisplay.IsVisible = true;

                this.speedDisplay.Speed = planter.GrowthRate.HasValue ? (int)(planter.GrowthRate * 100) : 0;

                var light = WorldLight.GetEffectiveLightPercent(RoomManager.GetTileLightLevel(planter.MainTileIndex));
                if (light == 100) this.lightDisplay.SetValue(light, UIColour.GreenText);
                else if (light > 0) this.lightDisplay.SetValue(light, UIColour.YellowText);
                else this.lightDisplay.SetValue(light, UIColour.RedText);

                var temperature = RoomManager.GetTileTemperature(planter.MainTileIndex);
                if (planter.CurrentCropTypeId > 0 && CropDefinitionManager.GetDefinition(planter.CurrentCropTypeId) is CropDefinition cropDef)
                {
                    if (temperature >= cropDef.MinGoodTemp && temperature < cropDef.MaxGoodTemp)
                    {
                        this.temperatureDisplay.SetTemperature((int)temperature, UIColour.GreenText);
                    }
                    else if (temperature >= cropDef.MinTemp && temperature < cropDef.MaxTemp)
                    {
                        this.temperatureDisplay.SetTemperature((int)temperature, UIColour.YellowText);
                    }
                    else
                    {
                        this.temperatureDisplay.SetTemperature((int)temperature, UIColour.RedText);
                    }

                    this.temperatureTooltip.SetText(GetTemperatureTooltipText(cropDef));
                    this.temperatureTooltip.IsEnabled = true;
                }
                else
                {
                    this.temperatureDisplay.SetTemperature((int)temperature, temperature >= 0 ? UIColour.GreenText : UIColour.RedText);
                    this.temperatureTooltip.IsEnabled = false;
                }

                if (planter.IsTooCold && planter.PlanterStatus.In(PlanterStatus.WaitingForSeeds, PlanterStatus.InProgress)) this.statusLabel.SetStatus(BuildingDisplayStatus.TooCold);
                else if (planter.IsTooHot && planter.PlanterStatus.In(PlanterStatus.WaitingForSeeds, PlanterStatus.InProgress)) this.statusLabel.SetStatus(BuildingDisplayStatus.TooHot);
                else if (!planter.HasWater && planter.PlanterStatus.In(PlanterStatus.WaitingToHarvest, PlanterStatus.InProgress)) this.statusLabel.SetStatus(BuildingDisplayStatus.NoWater);
                else if (planter.IsTooDark && planter.PlanterStatus.In(PlanterStatus.WaitingForSeeds, PlanterStatus.InProgress)) this.statusLabel.SetStatus(BuildingDisplayStatus.TooDark);
                else this.statusLabel.SetStatus(planter.PlanterStatus, planter.Progress);

                this.growthProgressBar.Fraction = planter.Progress.GetValueOrDefault();
                this.healthLabel.Text = $"{GetString(StringsForThingPanels.Health)}: {FormatPercent(planter.Health, GetString(StringsForThingPanels.NotPlanted))}";
                this.healthProgressBar.Fraction = planter.Health.GetValueOrDefault();

                var newCanPlantKekke = this.planterType == ThingType.PlanterStone && ProjectManager.GetDefinition(11)?.IsDone == true;

                if (planter.ThingType != this.planterType || this.canPlantKekke != newCanPlantKekke || this.isLanguageChanged)
                {
                    this.planterType = planter.ThingType;
                    this.canPlantKekke = newCanPlantKekke;
                    this.isLanguageChanged = false;

                    var cropTypes = (new List<string>() { GetString(StringsForThingPanels.DontPlant) });
                    var tags = new List<object>() { 0 };
                    foreach (var def in CropDefinitionManager.GetAll().Where(v => v.IsCrop))
                    {
                        if (this.planterType == ThingType.PlanterHydroponics && !def.CanGrowHydroponics) continue;
                        else if (this.planterType == ThingType.PlanterStone && !def.CanGrowSoil) continue;

                        if (def.Id == 6 && !this.canPlantKekke) continue;

                        cropTypes.Add($"{GetString(StringsForThingPanels.Next)}: {def.DisplayName}");
                        tags.Add(def.Id);
                    }

                    this.cropPicker.UpdateOptions(cropTypes, 0);
                    this.cropPicker.Tags = tags;
                }

                this.cropPicker.SelectedTag = planter.SelectedCropTypeId;

                switch (planter.PlanterStatus)
                {
                    case PlanterStatus.InProgress:
                    case PlanterStatus.WaitingToHarvest:
                        this.currentCropLabel.Text = GetString(StringsForThingPanels.CurrentCrop, CropDefinitionManager.GetDefinition(planter.CurrentCropTypeId)?.DisplayNameLong ?? "?");
                        break;
                    case PlanterStatus.Dead:
                        this.currentCropLabel.Text = GetString(StringsForThingPanels.CurrentCropDead, CropDefinitionManager.GetDefinition(planter.CurrentCropTypeId)?.DisplayName ?? "?");
                        break;
                    default:
                        this.currentCropLabel.Text = GetString(StringsForThingPanels.CurrentCropNone);
                        break;
                }

                this.growthRateTooltip.UpdateModifiers(planter.GrowthRateModifiers);
                this.growthRateTooltip.IsEnabled = planter.GrowthRateModifiers.Any();

                planter.FarmPriority = (WorkPriority)this.priorityPicker.SelectedIndex + 1;

                if (planter.SelectedCropTypeId != (this.cropPickerTooltip.CropDefinition?.Id ?? 0))
                {
                    var definition = planter.SelectedCropTypeId > 0 ? CropDefinitionManager.GetDefinition(planter.SelectedCropTypeId) : null;

                    var lockedDescription = "";
                    if (planter.SelectedCropTypeId == 6 && ProjectManager.GetDefinition(11) is Project kekProject && !kekProject.IsDone)
                    {
                        // Kekke requires project
                        var labType = LanguageManager.GetName(ThingType.Biolab);
                        lockedDescription = LanguageManager.Get<StringsForConstructPanel>(StringsForConstructPanel.RequiresProject, kekProject.DisplayName, labType);
                    }

                    this.cropPickerTooltip.SetDefinition(definition, lockedDescription);
                }

                this.cropPickerTooltip.IsEnabled = this.cropPickerTooltip.CropDefinition != null;
            }
            else
            {
                this.speedDisplay.IsVisible = false;
                this.priorityPicker.IsVisible = false;
                this.cropPicker.IsVisible = false;
                this.growthProgressBar.IsVisible = false;
                this.statusLabel.IsVisible = false;
                this.healthLabel.IsVisible = false;
                this.healthProgressBar.IsVisible = false;
                this.growthRateTooltip.IsEnabled = false;
                this.currentCropLabel.IsVisible = false;
                this.temperatureDisplay.IsVisible = false;
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.isLanguageChanged = true;
            base.HandleLanguageChange();
        }

        protected override void OnBuildingChanged()
        {
            base.OnBuildingChanged();
            if (this.building is IPlanter p) this.priorityPicker.SelectedIndex = p.FarmPriority == WorkPriority.Disabled ? 0 : (int)p.FarmPriority - 1;
        }

        private static string GetTemperatureTooltipText(CropDefinition definition)
        {
            var sb = new StringBuilder();
            if (SettingsManager.TemperatureUnit == TemperatureUnit.F)
            {
                var unit = LanguageManager.Get<StringsForUnits>(StringsForUnits.F);
                sb.Append(LanguageManager.Get<StringsForCropTooltip>(StringsForCropTooltip.Temperature, definition.MinTemp.ToFahrenheit(), definition.MaxTemp.ToFahrenheit(), unit));
                sb.Append("|");
                sb.Append(LanguageManager.Get<StringsForCropTooltip>(StringsForCropTooltip.OptimalTemperature, definition.MinGoodTemp.ToFahrenheit(), definition.MaxGoodTemp.ToFahrenheit(), unit));
            }
            else
            {
                var unit = LanguageManager.Get<StringsForUnits>(StringsForUnits.C);
                sb.Append(LanguageManager.Get<StringsForCropTooltip>(StringsForCropTooltip.Temperature, definition.MinTemp, definition.MaxTemp, unit));
                sb.Append("|");
                sb.Append(LanguageManager.Get<StringsForCropTooltip>(StringsForCropTooltip.OptimalTemperature, definition.MinGoodTemp, definition.MaxGoodTemp, unit));
            }

            return sb.ToString();
        }

        private static string FormatPercent(double? val, string defaultStr)
        {
            return val.HasValue ? string.Format("{0,3:D0}%", (int)(val * 100.0)) : defaultStr;
        }

        private void OnCropTypeSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.Thing is IPlanter planter) planter.SetCrop((int)this.cropPicker.SelectedTag);
        }
    }
}
