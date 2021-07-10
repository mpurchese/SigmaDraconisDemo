namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.Shared;
    using Draconis.UI;
    using Settings;
    using Shared;
    using Language;
    using World;
    using WorldInterfaces;

    public class EnvironmentControlPanel : BuildingPanel, IThingPanel
    {
        private readonly TextLabel notInRoomLabel;
        private readonly PowerButtonWithUsageDisplay powerButton;
        private readonly SimpleTooltip powerButtonTooltip;
        private readonly LightDisplay lightDisplay;
        private readonly SimpleTooltip lightDisplayTooltip;
        private readonly TemperatureDisplay temperatureDisplay;

        private readonly HorizontalStack tempControlStack;
        private readonly TextLabel targetTempMinLabel;
        private readonly TextLabel targetTempMaxLabel;
        private readonly LeftRightPicker minTempPicker;
        private readonly LeftRightPicker maxTempPicker;

        private readonly TextLabel lightingLabel;
        private readonly LeftRightPicker lightingPicker;
        private readonly TextLabel tempLabel;
        private readonly LeftRightPicker tempPicker;

        public EnvironmentControlPanel(IUIElement parent, int y)
            : base(parent, y)
        {
            this.lightDisplay = this.AddChild(new LightDisplay(this, Scale(8), Scale(16)));
            this.lightDisplayTooltip = UIHelper.AddSimpleTooltip(this, this.lightDisplay, "", GetString(StringsForThingPanels.RoomLightLevelTooltip));

            this.temperatureDisplay = new TemperatureDisplay(this, Scale(68), Scale(16));
            this.AddChild(this.temperatureDisplay);

            this.powerButton = new PowerButtonWithUsageDisplay(this, Scale(218), Scale(16), displayBoxHeight: true);
            this.AddChild(this.powerButton);
            this.powerButton.MouseLeftClick += this.OnOnOffButtonClick;
            this.powerButtonTooltip = UIHelper.AddSimpleTooltip(this, this.powerButton, "", GetString(StringsForThingPanels.ClickToTogglePower));

            this.notInRoomLabel = UIHelper.AddTextLabel(this, 0, 18, 320, StringsForThingPanels.OnlyWorksInRoom);

            this.tempControlStack = this.AddChild(new HorizontalStack(this, 0, Scale(38), Scale(320), Scale(20), TextAlignment.MiddleCentre) { Spacing = Scale(2) });

            var minTempStr = GetString(StringsForThingPanels.TargetTempMin);
            this.targetTempMinLabel = new TextLabel(this.tempControlStack, 0, 0, Scale(4) + (minTempStr.Length * UIStatics.TextRenderer.LetterSpace), Scale(18), minTempStr, UIColour.DefaultText);
            this.tempControlStack.AddChild(this.targetTempMinLabel);

            var labels = new List<string>();
            var tags = new List<object>();
            for (int i = -10; i <= 36; i++)
            {
                labels.Add(LanguageHelper.FormatTemperature(i));
                tags.Add(i);
            }

            this.minTempPicker = new LeftRightPickerNarrow(this.tempControlStack, 0, 0, Scale(70), labels, 16) { Tags = tags };
            this.minTempPicker.SelectedIndexChanged += this.OnMinMaxTempPickerSelectedIndexChanged;
            this.tempControlStack.AddChild(this.minTempPicker);

            var maxTempStr = GetString(StringsForThingPanels.Max);
            maxTempStr = $" {maxTempStr}";
            this.targetTempMaxLabel = new TextLabel(this.tempControlStack, 0, 0, Scale(4) + (maxTempStr.Length * UIStatics.TextRenderer.LetterSpace), Scale(18), maxTempStr, UIColour.DefaultText);
            this.tempControlStack.AddChild(this.targetTempMaxLabel);

            var labels2 = new List<string>();
            var tags2 = new List<object>();
            for (int i = 0; i <= 40; i++)
            {
                labels2.Add(LanguageHelper.FormatTemperature(i));
                tags2.Add(i);
            }

            this.maxTempPicker = new LeftRightPickerNarrow(this.tempControlStack, 0, 0, Scale(70), labels2, 20) { Tags = tags2 };
            this.maxTempPicker.SelectedIndexChanged += this.OnMinMaxTempPickerSelectedIndexChanged;
            this.tempControlStack.AddChild(this.maxTempPicker);

            this.lightingLabel = UIHelper.AddTextLabel(this, 50, 58, StringsForThingPanels.Lighting);
            this.lightingLabel.IsVisible = false;

            var lightSettings = Enum.GetValues(typeof(RoomLightSetting)).Cast<RoomLightSetting>().ToList();
            this.lightingPicker = new LeftRightEnumPicker<RoomLightSetting>(this, 140, 57, 140, lightSettings, 0) { IsVisible = false };
            this.lightingPicker.SelectedIndexChanged += this.OnLightingPickerSelectedIndexChanged;
            this.AddChild(this.lightingPicker);

            this.tempLabel = new TextLabelAutoScaling(this, 50, 78, $"{GetString(StringsForThingPanels.Temperature)}:", UIColour.DefaultText) { IsVisible = false };
            this.AddChild(this.tempLabel);

            var tempSettings = Enum.GetValues(typeof(RoomTemperatureSetting)).Cast<RoomTemperatureSetting>().ToList();
            this.tempPicker = new LeftRightEnumPicker<RoomTemperatureSetting>(this, 140, 77, 140, tempSettings, 0) { IsVisible = false };
            this.tempPicker.SelectedIndexChanged += this.OnTempPickerSelectedIndexChanged;
            this.AddChild(this.tempPicker);

            SettingsManager.SettingsSaved += this.OnSettingsSaved;
        }

        public override void ApplyLayout()
        {
            foreach (var child in this.Children)
            {
                child.X = this.Rescale(child.X);
                if (child == this.lightingPicker) child.Y = Scale(57);
                else if (child == this.tempPicker) child.Y = Scale(77);
                else child.Y = this.Rescale(child.Y);
                child.ApplyScale();
                child.ApplyLayout();
            }

            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        public override void Update()
        {
            if (this.IsBuildingUiVisible && this.Thing is IEnvironmentControl ec)
            {
                var room = ec.Room;
                if (room == null || !room.IsComplete)
                {
                    this.notInRoomLabel.IsVisible = true;
                    this.powerButton.IsVisible = false;
                    this.temperatureDisplay.IsVisible = false;
                    this.lightDisplay.IsVisible = false;
                    this.lightingLabel.IsVisible = false;
                    this.lightingPicker.IsVisible = false;
                    this.tempLabel.IsVisible = false;
                    this.tempPicker.IsVisible = false;
                    this.tempControlStack.IsVisible = false;
                }
                else
                {
                    this.notInRoomLabel.IsVisible = false;
                    this.temperatureDisplay.IsVisible = true;
                    this.lightDisplay.IsVisible = true;
                    this.powerButton.IsVisible = true;
                    this.lightingLabel.IsVisible = true;
                    this.lightingPicker.IsVisible = true;
                    this.tempLabel.IsVisible = true;
                    this.tempPicker.IsVisible = true;
                    this.tempControlStack.IsVisible = true;
                    this.temperatureDisplay.SetTemperature((int)room.Temperature, (int)room.Temperature < 0 ? UIColour.RedText : UIColour.GreenText);
                    this.lightingPicker.SelectedIndex = (int)ec.LightSetting;
                    this.tempPicker.SelectedIndex = (int)ec.TemperatureSetting;
                    this.minTempPicker.SelectedIndex = ec.TargetTempMin + 10;
                    this.maxTempPicker.SelectedIndex = ec.TargetTempMax;

                    var light = ec.Room?.ArtificialLight > 0 ? 60 : WorldLight.GetEffectiveLightPercent(World.WorldLight.Brightness);
                    if (light == 100) this.lightDisplay.SetValue(light, UIColour.GreenText);
                    else if (light > 0) this.lightDisplay.SetValue(light, UIColour.YellowText);
                    else this.lightDisplay.SetValue(light, UIColour.RedText);

                    this.powerButton.IsOn = ec.IsOn;
                    this.powerButton.EnergyOutput = -ec.SmoothedEnergyUseRate.KWh;
                    this.powerButtonTooltip.SetTitle(GetString(StringsForThingPanels.EnergyUsekW, ec.SmoothedEnergyUseRate.KWh));
                }
            }
            else
            {
                this.notInRoomLabel.IsVisible = false;
                this.powerButton.IsVisible = false;
                this.temperatureDisplay.IsVisible = false;
                this.lightDisplay.IsVisible = false;
                this.lightingLabel.IsVisible = false;
                this.lightingPicker.IsVisible = false;
                this.tempLabel.IsVisible = false;
                this.tempPicker.IsVisible = false;
                this.tempControlStack.IsVisible = false;
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.tempLabel.Text = $"{GetString(StringsForThingPanels.Temperature)}:";
            this.lightDisplayTooltip.SetText(GetString(StringsForThingPanels.RoomLightLevelTooltip));
            this.powerButtonTooltip.SetText(GetString(StringsForThingPanels.ClickToTogglePower));
            this.UpdateMinMaxTemperaturePickers();
            base.HandleLanguageChange();
        }

        private void OnOnOffButtonClick(object sender, MouseEventArgs e)
        {
            this.powerButton.IsOn = !this.powerButton.IsOn;
            if (this.building is IEnvironmentControl ec && ec.IsOn != this.powerButton.IsOn) ec.TogglePower();
        }

        private void OnSettingsSaved(object sender, EventArgs e)
        {
            this.UpdateMinMaxTemperaturePickers();
        }

        private void UpdateMinMaxTemperaturePickers()
        {
            var labels = new List<string>();
            for (int i = -10; i <= 36; i++) labels.Add(LanguageHelper.FormatTemperature(i));
            this.minTempPicker.UpdateOptions(labels, this.minTempPicker.SelectedIndex);

            var labels2 = new List<string>();
            for (int i = 0; i <= 40; i++) labels2.Add(LanguageHelper.FormatTemperature(i));
            this.maxTempPicker.UpdateOptions(labels2, this.maxTempPicker.SelectedIndex);
        }

        private void OnMinMaxTempPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            // Can't overlap temperature ranges
            if (sender == this.minTempPicker && (int)this.minTempPicker.SelectedTag >= (int)this.maxTempPicker.SelectedTag) this.maxTempPicker.SelectedIndex += 1;
            if (sender == this.maxTempPicker && (int)this.maxTempPicker.SelectedTag <= (int)this.minTempPicker.SelectedTag) this.minTempPicker.SelectedIndex -= 1;

            if (this.Thing is IEnvironmentControl ec)
            {
                ec.TargetTempMin = (int)this.minTempPicker.SelectedTag;
                ec.TargetTempMax = (int)this.maxTempPicker.SelectedTag;
            }
        }

        private void OnLightingPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.Thing is IEnvironmentControl roomControl)
            {
                var room = roomControl.Room;
                if (room == null) return;
                foreach (var rc in World.GetThings(ThingType.EnvironmentControl).OfType<IEnvironmentControl>().Where(r => r.Room == room))
                {
                    rc.LightSetting = (RoomLightSetting)this.lightingPicker.SelectedIndex;
                }
            }
        }

        private void OnTempPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.Thing is IEnvironmentControl ec)
            {
                ec.TemperatureSetting = (RoomTemperatureSetting)this.tempPicker.SelectedIndex;
            }
        }
    }
}
