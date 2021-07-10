namespace SigmaDraconis.UI
{
    using Draconis.Shared;
    using Draconis.UI;
    using Language;
    using Shared;
    using WorldInterfaces;
    using World;

    public class LampPanel : PanelLeft, IThingPanel
    {
        private readonly TextLabel underConstructionLabel;
        private readonly LightDisplay lightDisplay;
        private readonly SimpleTooltip lightDisplayTooltip;
        private readonly PowerButtonWithUsageDisplay powerButton;
        private readonly SimpleTooltip powerButtonTooltip;
        private readonly TickBoxTextButton automaticTickButton;

        private ILamp lamp;

        public IThing Thing
        {
            get => this.lamp;
            set => this.lamp = value as ILamp;
        }

        public LampPanel(IUIElement parent, int y)
            : base(parent, y, Scale(320), Scale(100), GetString(StringsForThingPanels.Lamp))
        {
            this.underConstructionLabel = this.AddChild(new TextLabelAutoScaling(this, 0, 25, 320, 20, "", UIColour.DefaultText) { IsVisible = false });

            this.lightDisplay = this.AddChild(new LightDisplay(this, Scale(8), Scale(16)));
            this.lightDisplayTooltip = UIHelper.AddSimpleTooltip(this, this.lightDisplay, "", GetString(StringsForThingPanels.LampLightLevelTooltip));

            this.powerButton = this.AddChild(new PowerButtonWithUsageDisplay(this, Scale(222), Scale(16)));
            this.powerButton.MouseLeftClick += this.OnOnOffButtonClick;
            this.powerButtonTooltip = UIHelper.AddSimpleTooltip(this, this.powerButton, "", GetString(StringsForThingPanels.ClickToTogglePower));

            this.automaticTickButton = this.AddChild(new TickBoxTextButton(this, Scale(68), Scale(16), Scale(110), Scale(18), GetString(StringsForThingPanels.Automatic)));
            this.automaticTickButton.MouseLeftClick += this.OnAutomaticButtonClick;
            UIHelper.AddSimpleTooltip(this, this.automaticTickButton, StringsForThingPanels.AutomaticTooltip);
        }

        public override void Update()
        {
            if (this.IsVisible && this.lamp != null)
            {
                if (this.lamp.ConstructionProgress < 100)
                {
                    this.underConstructionLabel.IsVisible = true;
                    this.underConstructionLabel.Text = GetString(StringsForThingPanels.UnderConstruction, this.lamp.ConstructionProgress);
                    this.powerButton.IsVisible = false;
                    this.automaticTickButton.IsVisible = false;
                    this.lightDisplay.IsVisible = false;
                }
                else if (this.lamp.IsRecycling)
                {
                    this.underConstructionLabel.IsVisible = true;
                    this.underConstructionLabel.Text = GetString(StringsForThingPanels.UnderDeconstruction, this.lamp.RecycleProgress);
                    this.powerButton.IsVisible = false;
                    this.automaticTickButton.IsVisible = false;
                    this.lightDisplay.IsVisible = false;
                }
                else
                {
                    this.underConstructionLabel.IsVisible = false;
                    this.powerButton.IsVisible = true;
                    this.automaticTickButton.IsVisible = true;
                    this.lightDisplay.IsVisible = true;

                    var light = this.lamp.EnergyUseRate > 0 ? 60 : WorldLight.GetEffectiveLightPercent(World.WorldLight.Brightness);
                    if (light == 100) this.lightDisplay.SetValue(light, UIColour.GreenText);
                    else if (light > 0) this.lightDisplay.SetValue(light, UIColour.YellowText);
                    else this.lightDisplay.SetValue(light, UIColour.RedText);

                    this.automaticTickButton.IsTicked = this.lamp.IsAutomatic;
                    this.powerButton.IsOn = this.lamp.IsOn;
                    this.powerButton.EnergyOutput = -this.lamp.EnergyUseRate.KWh;
                    this.powerButtonTooltip.SetTitle(GetString(StringsForThingPanels.EnergyUsekW, this.lamp.EnergyUseRate.KWh));
                }
            }

            base.Update();
        }

        protected override void HandleLanguageChange()
        {
            this.automaticTickButton.Text = GetString(StringsForThingPanels.Automatic);
            this.lightDisplayTooltip.SetText(GetString(StringsForThingPanels.LampLightLevelTooltip));
            this.powerButtonTooltip.SetText(GetString(StringsForThingPanels.ClickToTogglePower));
            base.HandleLanguageChange();
        }

        private void OnOnOffButtonClick(object sender, MouseEventArgs e)
        {
            this.powerButton.IsOn = !this.powerButton.IsOn;
            if (this.lamp != null) this.lamp.IsOn = this.powerButton.IsOn;
        }

        private void OnAutomaticButtonClick(object sender, MouseEventArgs e)
        {
            this.automaticTickButton.IsTicked = !this.automaticTickButton.IsTicked;
            if (this.lamp != null) this.lamp.IsAutomatic = this.automaticTickButton.IsTicked;
        }

        protected static string GetString(StringsForThingPanels key)
        {
            return LanguageManager.Get<StringsForThingPanels>(key);
        }

        protected static string GetString(StringsForThingPanels key, object arg0)
        {
            return LanguageManager.Get<StringsForThingPanels>(key, arg0);
        }
    }
}
