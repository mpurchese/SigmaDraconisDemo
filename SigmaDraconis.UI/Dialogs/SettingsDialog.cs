namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Settings;
    
    public class SettingsDialog : DialogBase
    {
        private readonly TextButton okButton;
        private readonly TextButton cancelButton;
        private readonly SettingsPicker<StringsForSettingsDialog> graphicsModePicker;
        private readonly SettingsPicker<string> screenResPicker;
        private readonly SettingsPicker<StringsForSettingsDialog> uiScalingPicker;
        private readonly SettingsPicker<StringsForSettingsDialog> textureResPicker;
        private readonly SettingsPicker<StringsForSettingsDialog> shadowDetailPicker;
        private readonly SettingsPicker<StringsForSettingsDialog> terrainGridPicker;
        private readonly SettingsPicker<string> soundVolumePicker;
        private readonly SettingsPicker<string> musicVolumePicker;
        private readonly SettingsPicker<StringsForUnits> temperatureUnitPicker;
        private readonly SettingsPicker<StringsForUnits> windSpeedUnitPicker;

        private readonly Dictionary<int, Vector2i> supportedResolutions = new Dictionary<int, Vector2i>();

        public bool IsFullScreen { get { return this.graphicsModePicker.SelectedIndex == 1; } }
        public int DisplayWidth { get { return this.supportedResolutions[this.screenResPicker.SelectedIndex].X; } }
        public int DisplayHeight { get { return this.supportedResolutions[this.screenResPicker.SelectedIndex].Y; } }
        public int SoundVolume { get { return this.soundVolumePicker.SelectedIndex * 10; } }

        public event EventHandler<EventArgs> OkClick;
        public event EventHandler<EventArgs> CancelClick;

        private static int GetMaxLabelLength()
        {
            var stringIds = new List<StringsForSettingsDialog>
            {
                StringsForSettingsDialog.DisplayMode,
                StringsForSettingsDialog.Language,
                StringsForSettingsDialog.MusicVolume,
                StringsForSettingsDialog.Resolution,
                StringsForSettingsDialog.ShadowDetail,
                StringsForSettingsDialog.SoundVolume,
                StringsForSettingsDialog.TemperatureUnit,
                StringsForSettingsDialog.TerrainGrid,
                StringsForSettingsDialog.TextureRes,
                StringsForSettingsDialog.UIScaling,
                StringsForSettingsDialog.WindSpeedUnit
            };

            return stringIds.Max(s => LanguageManager.Get<StringsForSettingsDialog>(s).Length);
        }

        public SettingsDialog(IUIElement parent, bool isFullScreen)
            : base(parent, Scale(360), Scale(310), StringsForDialogTitles.Settings)
        {
            this.IsVisible = false;

            var pickerWidth = Scale((GetMaxLabelLength() * 7) + 158);
            this.W = pickerWidth + Scale(16);
            this.titleLabel.W = this.W;

            var y = Scale(20);
            var graphicsOptions = new List<StringsForSettingsDialog> { StringsForSettingsDialog.Window, StringsForSettingsDialog.FullScreen };
            this.graphicsModePicker = new SettingsPicker<StringsForSettingsDialog>(this, Scale(8), y, pickerWidth, StringsForSettingsDialog.DisplayMode, graphicsOptions, isFullScreen ? 1 : 0);
            this.AddChild(this.graphicsModePicker);
            this.graphicsModePicker.SelectedIndexChanged += this.GraphicsModePickerSelectedIndexChanged;

            y += Scale(22);
            int currentResIndex = UpdateSupportedResolutions();
            this.screenResPicker = new SettingsPicker<string>(this, Scale(8), y, pickerWidth, StringsForSettingsDialog.Resolution, this.supportedResolutions.Values.Select(v => $"{v.X} x {v.Y}").ToList(), currentResIndex);
            this.AddChild(this.screenResPicker);
            this.screenResPicker.IsEnabled = isFullScreen;

            y += Scale(22);
            int currentScaleIndex = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.UIScaling).GetValueOrDefault(2);
            var scalingOptions = new List<StringsForSettingsDialog> { StringsForSettingsDialog.None, StringsForSettingsDialog.Medium, StringsForSettingsDialog.Maximum };
            this.uiScalingPicker = new SettingsPicker<StringsForSettingsDialog>(this, Scale(8), y, pickerWidth, StringsForSettingsDialog.UIScaling, scalingOptions, currentScaleIndex);
            this.uiScalingPicker.SelectedIndexChanged += this.OnUIScalingPickerSelectedIndexChanged;
            this.AddChild(this.uiScalingPicker);

            y += Scale(30);
            var textureResOptions = new List<StringsForSettingsDialog> { StringsForSettingsDialog.Low, StringsForSettingsDialog.High };
            var textureResSelectedOption = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.TextureRes).GetValueOrDefault(1);
            this.textureResPicker = new SettingsPicker<StringsForSettingsDialog>(this, Scale(8), y, pickerWidth, StringsForSettingsDialog.TextureRes, textureResOptions, textureResSelectedOption);
            this.textureResPicker.SelectedIndexChanged += this.OnTextureResPickerSelectedIndexChanged;
            this.AddChild(this.textureResPicker);

            y += Scale(22);
            var shadowOptions = new List<StringsForSettingsDialog> { StringsForSettingsDialog.Off, StringsForSettingsDialog.Low, StringsForSettingsDialog.High };
            var shadowSelectedOption = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.ShadowDetail).GetValueOrDefault(2);
            this.shadowDetailPicker = new SettingsPicker<StringsForSettingsDialog>(this, Scale(8), y, pickerWidth, StringsForSettingsDialog.ShadowDetail, shadowOptions, shadowSelectedOption);
            this.shadowDetailPicker.SelectedIndexChanged += this.OnShadowDetailPickerSelectedIndexChanged;
            this.AddChild(this.shadowDetailPicker);

            y += Scale(30);
            var terrainGridOptions = new List<StringsForSettingsDialog> { StringsForSettingsDialog.Off, StringsForSettingsDialog.On };
            var terrainGridSelectedOption = SettingsManager.GetSettingBool(SettingGroup.Graphics, SettingNames.EnableTerrainGrid) == true ? 1 : 0;
            this.terrainGridPicker = new SettingsPicker<StringsForSettingsDialog>(this, Scale(8), y, pickerWidth, StringsForSettingsDialog.TerrainGrid, terrainGridOptions, terrainGridSelectedOption);
            this.terrainGridPicker.SelectedIndexChanged += this.OnTerrainGridPickerSelectedIndexChanged;
            this.AddChild(this.terrainGridPicker);

            y += Scale(30);
            var volumeOptions = new List<string> { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };
            var volumeSelectedOption = (SettingsManager.GetSettingInt(SettingGroup.Sound, SettingNames.SoundVolume) ?? 8) / 10;
            this.soundVolumePicker = new SettingsPicker<string>(this, Scale(8), y, pickerWidth, StringsForSettingsDialog.SoundVolume, volumeOptions, volumeSelectedOption);
            this.AddChild(this.soundVolumePicker);

            y += Scale(22);
            volumeSelectedOption = (SettingsManager.GetSettingInt(SettingGroup.Sound, SettingNames.MusicVolume) ?? 4) / 10;
            this.musicVolumePicker = new SettingsPicker<string>(this, Scale(8), y, pickerWidth, StringsForSettingsDialog.MusicVolume, volumeOptions, volumeSelectedOption);
            this.musicVolumePicker.SelectedIndexChanged += this.OnMusicVolumePickerSelectedIndexChanged;
            this.AddChild(this.musicVolumePicker);

            y += Scale(30);
            var temperatureOptions = new List<StringsForUnits> { StringsForUnits.C, StringsForUnits.F };
            this.temperatureUnitPicker = new SettingsPicker<StringsForUnits>(this, Scale(8), y, pickerWidth, StringsForSettingsDialog.TemperatureUnit, temperatureOptions, (int)SettingsManager.TemperatureUnit);
            this.temperatureUnitPicker.SelectedIndexChanged += this.OnTemperatureUnitPickerSelectedIndexChanged;
            this.AddChild(this.temperatureUnitPicker);

            y += Scale(22);
            var windSpeedOptions = new List<StringsForUnits> { StringsForUnits.mps, StringsForUnits.kph, StringsForUnits.mph };
            this.windSpeedUnitPicker = new SettingsPicker<StringsForUnits>(this, Scale(8), y, pickerWidth, StringsForSettingsDialog.WindSpeedUnit, windSpeedOptions, (int)SettingsManager.SpeedUnit);
            this.windSpeedUnitPicker.SelectedIndexChanged += this.OnWindSpeedUnitPickerSelectedIndexChanged;
            this.AddChild(this.windSpeedUnitPicker);

            this.okButton = new TextButtonWithLanguage(this, (this.W / 4) - Scale(40), this.H - Scale(30), Scale(100), Scale(20), StringsForButtons.OK) { TextColour = UIColour.GreenText };
            this.okButton.MouseLeftClick += this.OnOkClick;
            this.AddChild(this.okButton);

            this.cancelButton = new TextButtonWithLanguage(this, (this.W * 3 / 4) - Scale(60), this.H - Scale(30), Scale(100), Scale(20), StringsForButtons.Cancel) { TextColour = UIColour.RedText };
            this.cancelButton.MouseLeftClick += this.OnCancelClick;
            this.AddChild(this.cancelButton);
        }

        public override void Show()
        {
            this.cancelButton.IsSelected = true;
            this.okButton.IsSelected = false;
            base.Show();
        }

        private void OnUIScalingPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.UIScaling, this.uiScalingPicker.SelectedIndex);
        }

        private void OnMusicVolumePickerSelectedIndexChanged(object sender, EventArgs e)
        {
            SettingsManager.SetSetting(SettingGroup.Sound, SettingNames.MusicVolume, this.musicVolumePicker.SelectedIndex * 10);
        }

        private void OnTerrainGridPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.EnableTerrainGrid, this.terrainGridPicker.SelectedIndex == 1);
        }

        private void OnTextureResPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.TextureRes, this.textureResPicker.SelectedIndex);
        }

        private void OnShadowDetailPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.ShadowDetail, this.shadowDetailPicker.SelectedIndex);
        }

        protected override void HandleLanguageChange()
        {
            var pickerWidth = Scale((GetMaxLabelLength() * 7) + 160);
            this.W = pickerWidth + Scale(16);

            foreach (var picker in this.Children.OfType<SettingsPicker<StringsForSettingsDialog>>()) picker.SetWidth(pickerWidth);
            foreach (var picker in this.Children.OfType<SettingsPicker<string>>()) picker.SetWidth(pickerWidth);
            foreach (var picker in this.Children.OfType<SettingsPicker<StringsForUnits>>()) picker.SetWidth(pickerWidth);

            this.okButton.X = (this.W / 4) - Scale(40);
            this.cancelButton.X = (this.W * 3 / 4) - Scale(60);

            this.UpdateHorizontalPosition();
            base.HandleLanguageChange();
        }

        private void OnTemperatureUnitPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            SettingsManager.SetSetting(SettingGroup.Misc, SettingNames.TemperatureUnit, this.temperatureUnitPicker.SelectedIndex == 1 ? "F" : "C");
        }

        private void OnWindSpeedUnitPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            switch (this.windSpeedUnitPicker.SelectedIndex)
            {
                case 0: SettingsManager.SetSetting(SettingGroup.Misc, SettingNames.WindSpeedUnit, "mps"); break;
                case 1: SettingsManager.SetSetting(SettingGroup.Misc, SettingNames.WindSpeedUnit, "kph"); break;
                case 2: SettingsManager.SetSetting(SettingGroup.Misc, SettingNames.WindSpeedUnit, "mph"); break;
            }
        }

        private int UpdateSupportedResolutions()
        {
            var i = 0;
            var currentResIndex = 0;
            foreach (var mode in UIStatics.Graphics.Adapter.SupportedDisplayModes.ToList().OrderBy(m => m.Width).ThenBy(m => m.Height))
            {
                if (mode.Format == SurfaceFormat.Color && mode.Width >= 1280 && mode.Height >= 720)
                {
                    this.supportedResolutions.Add(i, new Vector2i(mode.Width, mode.Height));
                    if (SettingsManager.FullScreenSizeX == mode.Width && SettingsManager.FullScreenSizeY == mode.Height)
                    {
                        currentResIndex = i;
                    }

                    i++;
                }
            }

            if (!supportedResolutions.Any(r => r.Value.X == SettingsManager.FullScreenSizeX && r.Value.Y == SettingsManager.FullScreenSizeY))
            {
                currentResIndex = supportedResolutions.Count - 1;
                SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.FullScreenSizeX, supportedResolutions[currentResIndex].X);
                SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.FullScreenSizeY, supportedResolutions[currentResIndex].Y);
            }

            return currentResIndex;
        }

        public void ToggleIsFullScreen()
        {
            this.graphicsModePicker.SelectedIndex = this.graphicsModePicker.SelectedIndex == 1 ? 0 : 1;
            this.GraphicsModePickerSelectedIndexChanged(null, null);
        }

        private void GraphicsModePickerSelectedIndexChanged(object sender, EventArgs e)
        {
            var isFullScreen = this.graphicsModePicker.SelectedIndex == 1;
            var currentResIndex = -1;
            foreach (var mode in this.supportedResolutions)
            {
                if (SettingsManager.FullScreenSizeX == mode.Value.X && SettingsManager.FullScreenSizeY == mode.Value.Y)
                {
                    currentResIndex = mode.Key;
                }
            }

            if (currentResIndex == -1)
            {
                currentResIndex = this.supportedResolutions.Count - 1;
            }

            this.screenResPicker.UpdateOptions(this.supportedResolutions.Values.Select(v => $"{v.X} x {v.Y}").ToList(), currentResIndex);
            this.screenResPicker.IsEnabled = isFullScreen;
        }

        public void ResetSettings(bool isFullScreen)
        {
            var currentResIndex = -1;
            foreach (var mode in this.supportedResolutions)
            {
                if (SettingsManager.FullScreenSizeX == mode.Value.X && SettingsManager.FullScreenSizeY == mode.Value.Y)
                {
                    currentResIndex = mode.Key;
                }
            }

            if (currentResIndex == -1)
            {
                currentResIndex = this.supportedResolutions.Count - 1;
            }

            this.screenResPicker.UpdateOptions(this.supportedResolutions.Values.Select(v => $"{v.X} x {v.Y}").ToList(), currentResIndex);
            this.graphicsModePicker.SelectedIndex = isFullScreen ? 1 : 0;
            this.uiScalingPicker.SelectedIndex = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.UIScaling).GetValueOrDefault(2);
            this.terrainGridPicker.SelectedIndex = SettingsManager.GetSettingBool(SettingGroup.Graphics, SettingNames.EnableTerrainGrid) == true ? 1 : 0;
            this.musicVolumePicker.SelectedIndex = (SettingsManager.GetSettingInt(SettingGroup.Sound, SettingNames.SoundVolume) ?? 8) / 10;
            this.soundVolumePicker.SelectedIndex = (SettingsManager.GetSettingInt(SettingGroup.Sound, SettingNames.MusicVolume) ?? 4) / 10;
            this.shadowDetailPicker.SelectedIndex = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.ShadowDetail).GetValueOrDefault(2);
            this.textureResPicker.SelectedIndex = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.TextureRes).GetValueOrDefault(1);

            this.temperatureUnitPicker.SelectedIndex = (int)SettingsManager.TemperatureUnit;
            this.windSpeedUnitPicker.SelectedIndex = (int)SettingsManager.SpeedUnit;

            this.screenResPicker.IsEnabled = isFullScreen;
        }

        protected override void HandleEscapeKey()
        {
            SettingsManager.Reload();
            this.CancelClick?.Invoke(this, new EventArgs());
            base.HandleEscapeKey();
        }

        protected override void HandleEnterOrSpaceKey()
        {
            if (this.okButton.IsSelected) this.OkClick?.Invoke(this, new EventArgs());
            else if (this.cancelButton.IsSelected) this.HandleEscapeKey();
            base.HandleEnterOrSpaceKey();
        }

        protected override void HandleLeftKey()
        {
            this.cancelButton.IsSelected = !this.cancelButton.IsSelected;
            this.okButton.IsSelected = !this.okButton.IsSelected;
            base.HandleLeftKey();
        }

        protected override void HandleRightKey()
        {
            this.cancelButton.IsSelected = !this.cancelButton.IsSelected;
            this.okButton.IsSelected = !this.okButton.IsSelected;
            base.HandleLeftKey();
        }

        protected override void HandleUpKey()
        {
            this.cancelButton.IsSelected = !this.cancelButton.IsSelected;
            this.okButton.IsSelected = !this.okButton.IsSelected;
            base.HandleLeftKey();
        }

        protected override void HandleDownKey()
        {
            this.cancelButton.IsSelected = !this.cancelButton.IsSelected;
            this.okButton.IsSelected = !this.okButton.IsSelected;
            base.HandleLeftKey();
        }

        private void OnOkClick(object sender, MouseEventArgs e)
        {
            this.OkClick?.Invoke(this, new EventArgs());
        }

        private void OnCancelClick(object sender, MouseEventArgs e)
        {
            SettingsManager.Reload();
            this.CancelClick?.Invoke(this, new EventArgs());
        }
    }
}
