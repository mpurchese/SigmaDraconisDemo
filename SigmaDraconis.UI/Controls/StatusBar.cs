namespace SigmaDraconis.UI
{
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;
    using Draconis.UI;

    using Language;
    using Settings;
    using Shared;
    using World;
    using WorldControllers;
    using WorldInterfaces;

    using Managers;

    public class StatusBar : RenderTargetElement
    {
        private int gameSpeed = 1;
        private bool isPaused;
        private bool isRoofVisible = true;

        private readonly WeatherIcon weatherIcon;
        private readonly WindIcon windIcon;
        private readonly TextLabel timeLabel;
        private readonly TextLabel temperatureLabel;
        private readonly TextLabel windLabel;
        private readonly TextButton gameSpeedTextButton;
        private readonly StatusBarIconButton pauseButton;
        private readonly StatusBarIconButton playButton;
        private readonly StatusBarIconButton fastForwardButton;
        private readonly StatusBarIconButton roofOnButton;
        private readonly StatusBarIconButton roofOffButton;
        private readonly StatusBarIconButton deconstructButton;
        private readonly StatusBarIconButton constructButton;
        private readonly StatusBarIconButton resourceMapButton;
        private readonly MothershipStatusButton mothershipButton;
        private readonly StatusBarIconButton temperatureButton;
        private readonly StatusBarIconButton harvestButton;
        private readonly StatusBarIconButton farmButton;
        private readonly StatusBarIconButton geologyButton;
        private readonly CommentaryTicker commentaryTicker;
        private readonly StatusBarBox statusBarTickerBox;
        private readonly StatusBarBox weatherBox;
        private readonly StatusBarBox windBox;
        private readonly StatusBarBox timeBox;
        private ToolbarTooltip tooltip;

        private string pausedString;
        private string gameSpeedFormatString;
        private string timeFormatString;

        public int MaxGameSpeed { get; set; }

        public int GameSpeed
        {
            get
            {
                return this.gameSpeed;
            }
            set
            {
                this.gameSpeed = value;
                this.pauseButton.IsSelected = this.isPaused;
                this.playButton.IsEnabled = this.isPaused || value != 1;
                this.fastForwardButton.IsEnabled = !this.isPaused && value < 8;
            }
        }

        public bool IsPaused
        {
            get
            {
                return this.isPaused;
            }
            set
            {
                this.isPaused = value;
                this.pauseButton.IsSelected = value;
                this.playButton.IsEnabled = value || this.gameSpeed != 1;
                this.fastForwardButton.IsEnabled = !value && this.gameSpeed < this.MaxGameSpeed;
            }
        }

        public bool IsRoofVisible
        {
            get
            {
                return this.isRoofVisible;
            }
            set
            {
                if (this.isRoofVisible != value)
                { 
                    this.isRoofVisible = value;
                    this.ToggleRoofClick?.Invoke(this, null);
                }
            }
        }

        public event EventHandler<EventArgs> ZoomInClick;
        public event EventHandler<EventArgs> ZoomOutClick;
        public event EventHandler<EventArgs> PauseClick;
        public event EventHandler<EventArgs> PlayClick;
        public event EventHandler<EventArgs> FastForwardClick;
        public event EventHandler<EventArgs> ToggleRoofClick;
        public event EventHandler<EventArgs> ConstructClick;
        public event EventHandler<EventArgs> FarmClick;
        public event EventHandler<EventArgs> GeologyClick;
        public event EventHandler<EventArgs> HarvestClick;
        public event EventHandler<EventArgs> RecycleClick;
        public event EventHandler<EventArgs> ResourceMapClick;
        public event EventHandler<EventArgs> TemperatureClick;
        public event EventHandler<EventArgs> MothershipClick;
        public event EventHandler<EventArgs> CommentaryTickerClick;

        public StatusBar(IUIElement parent, int x, int y, int width, int height)
            : base(parent, x, y, width, height)
        {
            this.statusBarTickerBox = new StatusBarBox(this, 0, 1, 0, this.H - 2) { IsInteractive = true };
            this.AddChild(statusBarTickerBox);
            this.commentaryTicker = new CommentaryTicker(this.statusBarTickerBox);
            this.statusBarTickerBox.MouseLeftClick += this.OnCommentaryTickerClick;
            this.statusBarTickerBox.AddChild(this.commentaryTicker);

            this.timeBox = new StatusBarBox(this, 0, 1, 0, this.H - 2) { IsInteractive = true };
            this.AddChild(this.timeBox);
            this.timeLabel = new TextLabel(this.timeBox, Scale(4), Scale(6) - 1, 100, this.H - 2, "", Color.LightGray);
            this.timeBox.AddChild(this.timeLabel);

            this.weatherBox = new StatusBarBox(this, 0, 1, 0, this.H - 2);
            this.AddChild(this.weatherBox);
            this.weatherIcon = new WeatherIcon(this.weatherBox, Scale(4), Scale(4) - 1);
            this.weatherBox.AddChild(this.weatherIcon);
            this.temperatureLabel = new TextLabel(this.weatherBox, Scale(26), Scale(6) - 1, "", Color.LightGray);
            this.weatherBox.AddChild(this.temperatureLabel);

            this.windBox = new StatusBarBox(this, 0, 1, 0, this.H - 2);
            this.AddChild(this.windBox);
            this.windIcon = new WindIcon(this.windBox, Scale(4), Scale(4) - 1);
            this.windBox.AddChild(this.windIcon);
            this.windLabel = new TextLabel(this.windBox, Scale(26), Scale(6) - 1, "", Color.LightGray);
            this.windBox.AddChild(this.windLabel);

            this.fastForwardButton = new StatusBarIconButton(this, width, 1, "Textures\\Icons\\FastForward");
            this.AddChild(this.fastForwardButton);
            this.fastForwardButton.MouseLeftClick += this.OnFastForwardButtonClick;

            this.gameSpeedTextButton = new TextButton(this, width, 1, Scale(62), Scale(24), "")
            {
                IsInteractive = false,
                BackgroundColour = Color.Black,
                BorderColour1 = UIColour.BorderDark,
                BorderColour2 = UIColour.BorderDark,
                BorderColourMouseOver = UIColour.BorderDark
            };
            this.AddChild(this.gameSpeedTextButton);

            this.pauseButton = new StatusBarIconButton(this, width, 1, "Textures\\Icons\\Pause");
            this.AddChild(this.pauseButton);
            this.pauseButton.MouseLeftClick += this.OnPauseButtonClick;

            this.playButton = new StatusBarIconButton(this, width, 1, "Textures\\Icons\\Play");
            this.AddChild(this.playButton);
            this.playButton.MouseLeftClick += this.OnPlayButtonClick;

            this.mothershipButton = new MothershipStatusButton(this, 0, 1, Scale(152), Scale(24));
            this.AddChild(this.mothershipButton);
            this.mothershipButton.MouseLeftClick += this.OnMothershipButtonClick;

            this.temperatureButton = new StatusBarIconButton(this, width, 1, "Textures\\Icons\\Temperature");
            this.AddChild(this.temperatureButton);
            this.temperatureButton.MouseLeftClick += this.OnTemperatureButtonClick;

            this.resourceMapButton = new StatusBarIconButton(this, width, 1, "Textures\\Icons\\ResourceMap");
            this.AddChild(this.resourceMapButton);
            this.resourceMapButton.MouseLeftClick += this.OnResourceMapButtonClick;

            this.geologyButton = new StatusBarIconButton(this, width, 1, "Textures\\Icons\\Geology");
            this.AddChild(this.geologyButton);
            this.geologyButton.MouseLeftClick += this.OnGeologyButtonClick;

            this.harvestButton = new StatusBarIconButton(this, width, 1, "Textures\\Icons\\Fruit") { IsEnabled = false };
            this.AddChild(this.harvestButton);
            this.harvestButton.MouseLeftClick += this.OnHarvestButtonClick;

            this.farmButton = new StatusBarIconButton(this, width, 1, "Textures\\Icons\\Crop") { IsEnabled = false };
            this.AddChild(this.farmButton);
            this.farmButton.MouseLeftClick += this.OnFarmButtonClick;

            this.deconstructButton = new StatusBarIconButton(this, width, 1, "Textures\\Icons\\Recycle");
            this.AddChild(this.deconstructButton);
            this.deconstructButton.MouseLeftClick += this.OnRecycleButtonClick;

            this.constructButton = new StatusBarIconButton(this, width, 1, "Textures\\Icons\\Construct") { IsHighlighted = true };
            this.AddChild(this.constructButton);
            this.constructButton.MouseLeftClick += this.OnConstructButtonClick;

            this.roofOnButton = new StatusBarIconButton(this, width, 1, "Textures\\Icons\\RoofOn");
            this.AddChild(this.roofOnButton);
            this.roofOnButton.MouseLeftClick += this.OnRoofButtonClick;

            this.roofOffButton = new StatusBarIconButton(this, width, 1, "Textures\\Icons\\RoofOff") { IsVisible = false };
            this.AddChild(this.roofOffButton);
            this.roofOffButton.MouseLeftClick += this.OnRoofButtonClick;

            this.UpdateLayout();

            this.backgroundColour = new Color(0, 0, 0, UIStatics.BackgroundAlpha);
            this.IsInteractive = true;
            this.AnchorBottom = true;
            this.AnchorRight = true;
            this.AnchorTop = false;

            this.pausedString = GetString(StringsForStatusBar.Paused);
            this.gameSpeedFormatString = GetString(StringsForStatusBar.GameSpeedFormat);
            this.timeFormatString = GetString(StringsForStatusBar.TimeFormat);
        }

        private void AddTooltip(IUIElement attachedElement, string title, string detail = "")
        {
            this.tooltip = TooltipParent.Instance.AddTooltip(new ToolbarTooltip(attachedElement, title, detail), this);
        }

        private static string GetTooltipText(StringsForStatusBar strKey, string action)
        {
            var key = SettingsManager.GetFirstKeyForAction(action);
            var str = GetString(strKey);
            return string.IsNullOrEmpty(key) ? str : $"{str} ({key})";
        }

        public override void LoadContent()
        {
            this.borderTexture = new Texture2D(UIStatics.Graphics, 1, 1);
            this.borderTexture.SetData(new Color[1] { Color.White });
            base.LoadContent();
        }

        public override void Update()
        {
            if (this.backgroundColour.A != UIStatics.BackgroundAlpha)
            {
                this.backgroundColour = new Color(0, 0, 0, UIStatics.BackgroundAlpha);
                this.IsContentChangedSinceDraw = true;
            }

            var time = World.WorldTime;
            var df = time.DayFraction;

            this.weatherIcon.Weather = (df >= 0.25f && df <= 0.75f) ? WeatherType.Sunny : WeatherType.Clear;

            var isFahranheit = SettingsManager.GetSetting(SettingGroup.Misc, SettingNames.TemperatureUnit) == "F";
            var temperatureUnit = LanguageManager.Get<StringsForUnits>(isFahranheit ? StringsForUnits.F : StringsForUnits.C);

            var day = (time.TotalHoursPassed / WorldTime.HoursInDay) + 1;
            var hour = (time.TotalHoursPassed % WorldTime.HoursInDay) + 1;

            this.timeLabel.Text = string.Format(this.timeFormatString, day, hour);
            this.timeLabel.WordSpacing = this.timeLabel.Text.Length > 15 ? 5 : 7;
            this.temperatureLabel.Text = LanguageHelper.FormatTemperature(World.Temperature);
            this.windLabel.Text = LanguageHelper.FormatWind(World.Wind);
            this.windLabel.X = this.windLabel.Text.Length > 5 ? Scale(22) : Scale(26);

            this.mothershipButton.UpdateStatus(MothershipController.MothershipStatus, MothershipController.TimeUntilCanWake, MothershipController.TimeToArrival);

            var isPaused = this.IsPaused || !GameScreen.Instance.IsWindowActive;
            this.gameSpeedTextButton.Text = isPaused ? this.pausedString : string.Format(this.gameSpeedFormatString, this.GameSpeed);
            this.gameSpeedTextButton.TextColour = isPaused ? Color.OrangeRed : Color.LightGray;

            if (World.Temperature >= 40) this.temperatureLabel.Colour = UIColour.RedText;
            if (World.Temperature >= 30) this.temperatureLabel.Colour = UIColour.OrangeText;
            else if (World.Temperature <= -10) this.temperatureLabel.Colour = UIColour.BlueText;
            else if (World.Temperature < 0) this.temperatureLabel.Colour = UIColour.PaleBlueText;
            else this.temperatureLabel.Colour = UIColour.DefaultText;

            this.deconstructButton.IsSelected = PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Deconstruct;
            this.harvestButton.IsSelected = PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Harvest;
            this.geologyButton.IsSelected = PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Geology;
            this.resourceMapButton.IsSelected = GameScreen.Instance.OverlayType == OverlayType.Resources;
            this.temperatureButton.IsSelected = GameScreen.Instance.OverlayType == OverlayType.Temperature;
            this.constructButton.IsSelected = GameScreen.Instance.IsConstructPanelShown;
            this.farmButton.IsSelected = GameScreen.Instance.IsFarmPanelShown;
            this.harvestButton.IsEnabled = World.CanHarvestFruit;
            this.farmButton.IsEnabled = World.CanFarm;
            this.roofOnButton.IsEnabled = World.GetThings(ThingType.Roof).Any();
            this.roofOffButton.IsEnabled = this.roofOnButton.IsEnabled;

            // "Tutorial" highlights
            this.constructButton.IsHighlighted 
                = (World.Prefabs.Count(ThingType.ResourceProcessor) > 0 && World.GetThings<IColonist>(ThingType.Colonist).Any(c => c.IsArrived) && !World.GetThings(ThingType.ShorePump).Any())
                || (World.Prefabs.Count(ThingType.WaterPump) > 0 && WarningsController.IsShownWaterPumpNeededWarning);
            this.deconstructButton.IsHighlighted = !World.HasResourcesForDeconstructionBeenUsed && World.GetThings<IResourceProcessor>(ThingType.ResourceProcessor).Any(r => r.IsReady);

            this.UpdateTooltip();

            base.Update();
        }

        private void UpdateTooltip()
        {
            if (this.tooltip?.AttachedElement?.IsMouseOver == true)
            {
                if (this.tooltip.AttachedElement == this.mothershipButton) this.tooltip.SetText(GetMothershipTooltipText());
                return;
            }

            if (this.tooltip != null)
            {
                TooltipParent.Instance.RemoveChild(this.tooltip);
                this.tooltip = null;
            }

            if (!this.IsMouseOver) return;

            if (this.constructButton.IsMouseOver) this.AddTooltip(this.constructButton, GetTooltipText(StringsForStatusBar.TooltipConstruct, "Construct"));
            else if (this.deconstructButton.IsMouseOver) this.AddTooltip(this.deconstructButton, GetTooltipText(StringsForStatusBar.TooltipDeconstruct, "Deconstruct"));
            else if (this.farmButton.IsMouseOver) this.AddTooltip(this.farmButton, GetTooltipText(StringsForStatusBar.TooltipFarm, "Farm"), GetString(StringsForStatusBar.TooltipFarmRequirement));
            else if (this.fastForwardButton.IsMouseOver) this.AddTooltip(this.fastForwardButton, GetTooltipText(StringsForStatusBar.TooltipIncreaseGameSpeed, "GameSpeed:Increase"));
            else if (this.geologyButton.IsMouseOver) this.AddTooltip(this.geologyButton, GetTooltipText(StringsForStatusBar.TooltipGeology, "Geology"), GetString(StringsForStatusBar.TooltipGeologyRequirement));
            else if (this.harvestButton.IsMouseOver) this.AddTooltip(this.harvestButton, GetTooltipText(StringsForStatusBar.TooltipHarvest, "Harvest"), GetString(StringsForStatusBar.TooltipHarvestRequirement));
            else if (this.mothershipButton.IsMouseOver) this.AddTooltip(this.mothershipButton, GetTooltipText(StringsForStatusBar.TooltipMothership, "Mothership"), GetMothershipTooltipText());
            else if (this.pauseButton.IsMouseOver) this.AddTooltip(this.pauseButton, GetTooltipText(StringsForStatusBar.TooltipPause, "TogglePause"));
            else if (this.playButton.IsMouseOver) this.AddTooltip(this.playButton, GetString(StringsForStatusBar.TooltipPlay));
            else if (this.resourceMapButton.IsMouseOver) this.AddTooltip(this.resourceMapButton, GetTooltipText(StringsForStatusBar.TooltipResourceOverlay, "ResourceMap"));
            else if (this.roofOffButton.IsMouseOver) this.AddTooltip(this.roofOffButton, GetTooltipText(StringsForStatusBar.TooltipToggleRoofs, "ToggleRoof"));
            else if (this.roofOnButton.IsMouseOver) this.AddTooltip(this.roofOnButton, GetTooltipText(StringsForStatusBar.TooltipToggleRoofs, "ToggleRoof"));
            else if (this.temperatureButton.IsMouseOver) this.AddTooltip(this.temperatureButton, GetTooltipText(StringsForStatusBar.TooltipTemperatureOverlay, "Temperature"));
            else if (this.timeBox.IsMouseOver) this.AddTooltip(this.timeBox, "", GetString(StringsForStatusBar.TooltipSunriseSunset, 192, 86, 182));
            else if (this.weatherBox.IsMouseOver) this.AddTooltip(this.weatherBox, "", GetTemperatureTooltipText());
            else if (this.windBox.IsMouseOver) this.AddTooltip(this.windBox, "", GetWindTooltipText());
        }

        private static string GetMothershipTooltipText()
        {
            switch (MothershipController.MothershipStatus)
            {
                case MothershipStatus.ColonistIncoming:
                    var timeMinutes = MothershipController.TimeToArrival / 3600;
                    var timeSeconds = (MothershipController.TimeToArrival % 3600) / 60;
                    return LanguageManager.Get<StringsForMothershipStatusTooltip>(MothershipController.MothershipStatus, MothershipController.ArrivingColonistName, timeMinutes, timeSeconds);
                case MothershipStatus.ColonistArriving:
                    return LanguageManager.Get<StringsForMothershipStatusTooltip>(MothershipController.MothershipStatus, MothershipController.ArrivingColonistName);
                case MothershipStatus.PareparingToWake:
                    var timeMinutes1 = MothershipController.TimeUntilCanWake / 3600;
                    var timeSeconds1 = (MothershipController.TimeUntilCanWake % 3600) / 60;
                    return LanguageManager.Get<StringsForMothershipStatusTooltip>(MothershipController.MothershipStatus, timeMinutes1, timeSeconds1);
            }
                    
            return LanguageManager.Get<StringsForMothershipStatusTooltip>(MothershipController.MothershipStatus);
        }

        protected override void OnParentResized(int prevW, int prevH)
        {
            base.OnParentResized(prevW, prevH);
            this.UpdateLayout();
        }

        public override void ApplyScale()
        {
            this.W = this.Parent.W;
            this.H = Scale(24) + 2;
            this.IsContentChangedSinceDraw = true;
        }

        public override void ApplyLayout()
        {
            foreach (var child in this.Children)
            {
                if (!(child is IButton)) child.X = this.Rescale(child.X);
                child.Y = child.Y == 1 ? 1 : this.Rescale(child.Y);
                child.ApplyScale();
                child.ApplyLayout();
            }

            this.UpdateLayout();

            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void HandleLanguageChange()
        {
            this.pausedString = GetString(StringsForStatusBar.Paused);
            this.gameSpeedFormatString = GetString(StringsForStatusBar.GameSpeedFormat);
            this.timeFormatString = GetString(StringsForStatusBar.TimeFormat);
            base.HandleLanguageChange();
        }

        private static string GetTemperatureTooltipText()
        {
            return SettingsManager.TemperatureUnit == TemperatureUnit.F
                ? GetString(StringsForStatusBar.TooltipTemperatureRange, WeatherController.MinTemp.ToFahrenheit(), WeatherController.MaxTemp.ToFahrenheit(), LanguageManager.Get<StringsForUnits>(StringsForUnits.F))
                : GetString(StringsForStatusBar.TooltipTemperatureRange, WeatherController.MinTemp, WeatherController.MaxTemp, LanguageManager.Get<StringsForUnits>(StringsForUnits.C));
        }

        private static string GetWindTooltipText()
        {
            switch (SettingsManager.SpeedUnit)
            {
                case SpeedUnit.Mps: return GetString(StringsForStatusBar.TooltipWindSpeedRange, WeatherController.MinWind, WeatherController.MaxWind, LanguageManager.Get<StringsForUnits>(StringsForUnits.mps));
                case SpeedUnit.Mph: return GetString(StringsForStatusBar.TooltipWindSpeedRange, WeatherController.MinWind.ToMph(), WeatherController.MaxWind.ToMph(), LanguageManager.Get<StringsForUnits>(StringsForUnits.mph));
            }

            return GetString(StringsForStatusBar.TooltipWindSpeedRange, WeatherController.MinWind.ToKph(), WeatherController.MaxWind.ToKph(), LanguageManager.Get<StringsForUnits>(StringsForUnits.kph));
        }

        protected override void DrawBaseLayer()
        {
            if (this.borderTexture == null) return;

            var r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);

            this.spriteBatch.Begin();

            // Borders
            this.spriteBatch.Draw(this.borderTexture, new Rectangle(r.X, r.Y, r.Width, 1), UIColour.BorderMedium);
            this.spriteBatch.Draw(this.borderTexture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), UIColour.BorderDark);

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        private void UpdateLayout()
        {
            var nextX = this.W - Scale(24) - 1;
            this.fastForwardButton.X = nextX;

            nextX -= Scale(24) + 1;
            this.playButton.X = nextX;

            nextX -= Scale(60) + 1;
            this.gameSpeedTextButton.X = nextX;

            nextX -= Scale(24) + 1;
            this.pauseButton.X = nextX;

            this.mothershipButton.X = 1;

            nextX = this.mothershipButton.Right + Scale(6);
            this.constructButton.X = nextX;

            nextX += Scale(24) + 1;
            this.deconstructButton.X = nextX;

            nextX += Scale(24) + 1;
            this.farmButton.X = nextX;

            nextX += Scale(24) + 1;
            this.harvestButton.X = nextX;

            nextX += Scale(30) + 1;
            this.geologyButton.X = nextX;
            
            nextX += Scale(24) + 1;
            this.resourceMapButton.X = nextX;

            nextX += Scale(24) + 1;
            this.temperatureButton.X = nextX;

            nextX += Scale(30) + 1;
            this.roofOffButton.X = nextX;
            this.roofOnButton.X = nextX;

            this.timeBox.W = Scale(120);
            this.timeBox.X = this.pauseButton.X - this.timeBox.W - Scale(6);
            this.timeBox.H = this.H - 2;
            this.timeLabel.X = Scale(4);
            this.timeLabel.Y = Scale(6) - 1;
            this.timeLabel.W = this.timeBox.W - Scale(8);

            this.windBox.X = this.timeBox.X - Scale(72);
            this.windBox.W = Scale(66);
            this.windBox.H = this.H - 2;
            this.windIcon.X = Scale(4);
            this.windIcon.Y = Scale(4) - 1;
            this.windLabel.X = Scale(26);
            this.windLabel.Y = Scale(6) - 1;

            this.weatherBox.X = this.windBox.X - Scale(72);
            this.weatherBox.W = Scale(66);
            this.weatherBox.H = this.H - 2;
            this.weatherIcon.X = Scale(4);
            this.weatherIcon.Y = Scale(4) - 1;
            this.temperatureLabel.X = Scale(26);
            this.temperatureLabel.Y = Scale(6) - 1;

            this.statusBarTickerBox.X = this.roofOffButton.X + Scale(30);
            this.statusBarTickerBox.W = this.weatherBox.X - Scale(6) - this.statusBarTickerBox.X;
            this.statusBarTickerBox.H = this.H - 2;
        }

        private void OnZoomInButtonClick(object sender, MouseEventArgs e)
        {
            this.ZoomInClick?.Invoke(this, null);
        }

        private void OnZoomOutButtonClick(object sender, MouseEventArgs e)
        {
            this.ZoomOutClick?.Invoke(this, null);
        }

        private void OnZoomInButtonHold(object sender, MouseEventArgs e)
        {
            this.ZoomInClick?.Invoke(this, null);
        }

        private void OnZoomOutButtonHold(object sender, MouseEventArgs e)
        {
            this.ZoomOutClick?.Invoke(this, null);
        }

        private void OnPauseButtonClick(object sender, MouseEventArgs e)
        {
            this.PauseClick?.Invoke(this, null);
        }

        private void OnPlayButtonClick(object sender, MouseEventArgs e)
        {
            this.PlayClick?.Invoke(this, null);
        }

        private void OnFastForwardButtonClick(object sender, MouseEventArgs e)
        {
            this.FastForwardClick?.Invoke(this, null);
        }

        private void OnRoofButtonClick(object sender, MouseEventArgs e)
        {
            if (!this.roofOnButton.IsEnabled) return;

            this.roofOnButton.IsVisible = !this.roofOnButton.IsVisible;
            this.roofOffButton.IsVisible = !this.roofOffButton.IsVisible;
            this.isRoofVisible = this.roofOnButton.IsVisible;
            this.ToggleRoofClick?.Invoke(this, null);
        }

        private void OnConstructButtonClick(object sender, MouseEventArgs e)
        {
            if (this.harvestButton.IsEnabled) this.HarvestClick?.Invoke(this, null);
            this.ConstructClick?.Invoke(this, null);
        }

        private void OnGeologyButtonClick(object sender, MouseEventArgs e)
        {
            this.GeologyClick?.Invoke(this, null);
        }

        private void OnHarvestButtonClick(object sender, MouseEventArgs e)
        {
            if (this.harvestButton.IsEnabled || this.harvestButton.IsSelected) this.HarvestClick?.Invoke(this, null);
        }

        private void OnFarmButtonClick(object sender, MouseEventArgs e)
        {
            if (this.farmButton.IsEnabled) this.FarmClick?.Invoke(this, null);
        }

        private void OnRecycleButtonClick(object sender, MouseEventArgs e)
        {
            this.RecycleClick?.Invoke(this, null);
        }

        private void OnResourceMapButtonClick(object sender, MouseEventArgs e)
        {
            this.ResourceMapClick?.Invoke(this, null);
        }

        private void OnMothershipButtonClick(object sender, MouseEventArgs e)
        {
            this.MothershipClick?.Invoke(this, null);
        }

        private void OnTemperatureButtonClick(object sender, MouseEventArgs e)
        {
            this.TemperatureClick?.Invoke(this, null);
        }

        private void OnCommentaryTickerClick(object sender, MouseEventArgs e)
        {
            this.CommentaryTickerClick?.Invoke(this, null);
        }

        private static string GetString(StringsForStatusBar value)
        {
            return LanguageManager.Get<StringsForStatusBar>(value);
        }

        private static string GetString(StringsForStatusBar value, object arg1, object arg2, object arg3)
        {
            return LanguageManager.Get<StringsForStatusBar>(value, arg1, arg2, arg3);
        }
    }
}
