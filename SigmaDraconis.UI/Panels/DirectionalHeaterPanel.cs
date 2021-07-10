namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Settings;
    using Shared;
    using WorldInterfaces;
    using World.Rooms;

    public class DirectionalHeaterPanel : BuildingPanel, IThingPanel
    {
        private readonly PowerButtonWithUsageDisplay powerButton;
        private readonly SimpleTooltip powerButtonTooltip;
        private readonly LeftRightPicker directionPicker;
        private readonly TickBoxTextButton automaticTickButton;
        private readonly LeftRightPicker targetTempPicker;
        private readonly TickBoxTextButton roomHeatModeTickButton;
        private double? energyUseRate;

        public DirectionalHeaterPanel(IUIElement parent, int y)
            : base(parent, y)
        {
            var labels = new List<string>(31);
            var tags = new List<object>(31);
            for (int i = 0; i <= 30; i++)
            {
                labels.Add(GetString(StringsForThingPanels.TargetTemperature, LanguageHelper.FormatTemperature(i)));
                tags.Add(i);
            }

            this.targetTempPicker = this.AddChild(new LeftRightPicker(this, Scale(8), Scale(16), Scale(210), labels, 16) { IsVisible = false });
            this.targetTempPicker.Tags = tags;
            this.targetTempPicker.SelectedIndexChanged += this.OnTempPickerSelectedIndexChanged;

            this.powerButton = this.AddChild(new PowerButtonWithUsageDisplay(this, Scale(222), Scale(16)));
            this.powerButton.MouseLeftClick += this.OnOnOffButtonClick;
            this.powerButtonTooltip = UIHelper.AddSimpleTooltip(this, this.powerButton, "", GetString(StringsForThingPanels.ClickToTogglePower));

            this.automaticTickButton = this.AddChild(new TickBoxTextButton(this, Scale(42), Scale(38), Scale(116), Scale(18), GetString(StringsForThingPanels.Automatic)));
            this.automaticTickButton.MouseLeftClick += this.OnAutomaticButtonClick;
            UIHelper.AddSimpleTooltip(this, this.automaticTickButton, StringsForThingPanels.AutomaticTooltip);

            this.roomHeatModeTickButton = this.AddChild(new TickBoxTextButton(this, Scale(162), Scale(38), Scale(116), Scale(18), GetString(StringsForThingPanels.IndoorMode)));
            this.roomHeatModeTickButton.MouseLeftClick += this.OnRoomHeatModeButtonClick;
            UIHelper.AddSimpleTooltip(this, this.roomHeatModeTickButton, StringsForThingPanels.TryToHeatRoom);

            var directionSettings = new List<StringsForThingPanels> { StringsForThingPanels.DirectionNE, StringsForThingPanels.DirectionSE, StringsForThingPanels.DirectionSW, StringsForThingPanels.DirectionNW };
            this.directionPicker = new LeftRightEnumPicker<StringsForThingPanels>(this, 85, 60, 150, directionSettings, 1, true) { IsVisible = false };
            this.directionPicker.SelectedIndexChanged += this.OnDirectionPickerSelectedIndexChanged;
            this.AddChild(this.directionPicker);

            SettingsManager.SettingsSaved += this.OnSettingsSaved;
        }

        public override void Update()
        {
            if (this.IsVisible && this.building is IDirectionalHeater heater)
            {
                if (heater.ConstructionProgress < 100 || heater.IsRecycling)
                {
                    this.powerButton.IsVisible = false;
                    this.automaticTickButton.IsVisible = false;
                    this.directionPicker.IsVisible = false;
                    this.targetTempPicker.IsVisible = false;
                    this.roomHeatModeTickButton.IsVisible = false;
                }
                else
                {
                    this.powerButton.IsVisible = true;
                    this.automaticTickButton.IsVisible = true;
                    this.directionPicker.IsVisible = true;
                    this.targetTempPicker.IsVisible = true;
                    this.roomHeatModeTickButton.IsVisible = true;

                    this.automaticTickButton.IsTicked = heater.IsAutomatic;
                    this.directionPicker.SelectedIndex = (int)heater.Direction - 4;
                    this.targetTempPicker.SelectedIndex = heater.TargetTemperature;
                    this.roomHeatModeTickButton.IsTicked = heater.IsIndoorMode;
                    this.roomHeatModeTickButton.IsEnabled = RoomManager.GetRoom(heater.MainTileIndex) != null;

                    this.powerButton.IsOn = heater.IsOn;
                    if (heater.SmoothedEnergyUseRate.KWh != this.energyUseRate)
                    {
                        this.energyUseRate = heater.SmoothedEnergyUseRate.KWh;
                        this.powerButton.EnergyOutput = -this.energyUseRate.Value;
                        this.powerButtonTooltip.SetTitle(GetString(StringsForThingPanels.EnergyUsekW, this.energyUseRate));
                    }
                }
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.roomHeatModeTickButton.Text = GetString(StringsForThingPanels.IndoorMode);
            this.powerButtonTooltip.SetText(GetString(StringsForThingPanels.ClickToTogglePower));
            this.UpdateTemperaturePicker();
            base.HandleLanguageChange();
        }

        private void OnOnOffButtonClick(object sender, MouseEventArgs e)
        {
            this.powerButton.IsOn = !this.powerButton.IsOn;
            if (this.building is IHeater heater) heater.IsOn = this.powerButton.IsOn;
        }

        private void OnSettingsSaved(object sender, EventArgs e)
        {
            this.UpdateTemperaturePicker();
        }

        private void UpdateTemperaturePicker()
        {
            var labels = new List<string>(31);
            for (int i = 0; i <= 30; i++)
            {
                labels.Add(GetString(StringsForThingPanels.TargetTemperature, LanguageHelper.FormatTemperature(i)));
            }

            this.targetTempPicker.UpdateOptions(labels, this.targetTempPicker.SelectedIndex);
        }

        private void OnDirectionPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.building is IDirectionalHeater heater) heater.SetDirection((Direction)this.directionPicker.SelectedIndex + 4);
        }

        private void OnTempPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.building is IHeater heater) heater.TargetTemperature = this.targetTempPicker.SelectedIndex;
        }

        private void OnAutomaticButtonClick(object sender, MouseEventArgs e)
        {
            this.automaticTickButton.IsTicked = !this.automaticTickButton.IsTicked;
            if (this.building is IHeater heater) heater.IsAutomatic = this.automaticTickButton.IsTicked;
        }

        private void OnRoomHeatModeButtonClick(object sender, MouseEventArgs e)
        {
            this.roomHeatModeTickButton.IsTicked = !this.roomHeatModeTickButton.IsTicked;
            if (this.building is IHeater heater) heater.IsIndoorMode = this.roomHeatModeTickButton.IsTicked;
        }
    }
}
