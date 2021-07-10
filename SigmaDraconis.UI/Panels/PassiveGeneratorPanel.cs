namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Language;
    using WorldInterfaces;

    public class PassiveGeneratorPanel : BuildingPanel, IThingPanel
    {
        private readonly EnergyGenDisplay energyGenDisplay;
        private readonly SimpleTooltip energyGenTooltip;
        private readonly double maxEnergyGen;

        public PassiveGeneratorPanel(IUIElement parent, int y, string iconName, double maxEnergyGen)
            : base(parent, y)
        {
            this.maxEnergyGen = maxEnergyGen;

            this.energyGenDisplay = new EnergyGenDisplay(this, Scale(8), Scale(16), 78, iconName);
            this.AddChild(this.energyGenDisplay);

            this.energyGenTooltip = new SimpleTooltip(TooltipParent.Instance, this.energyGenDisplay);
            TooltipParent.Instance.AddChild(this.energyGenTooltip);
        }

        public override void Update()
        {
            if (this.IsBuildingUiVisible && this.building is IEnergyGenerator generator)
            {
                this.energyGenDisplay.IsVisible = true;
                if (this.energyGenDisplay.EnergyGen != generator.EnergyGenRate.KWh)
                {
                    this.energyGenDisplay.EnergyGen = generator.EnergyGenRate.KWh;
                    this.energyGenTooltip.SetTitle(GetString(StringsForThingPanels.OutputKwWithMax, generator.EnergyGenRate.KWh, this.maxEnergyGen));
                }
            }
            else
            {
                this.energyGenDisplay.IsVisible = false;
            }

            base.Update();
        }
    }
}
