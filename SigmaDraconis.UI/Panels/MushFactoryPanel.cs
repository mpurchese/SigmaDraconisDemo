namespace SigmaDraconis.UI
{
    using System;
    using Draconis.UI;
    using Config;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class MushFactoryPanel : FactoryBuildingPanel
    {
        private readonly int minTemperature;
        protected readonly TemperatureDisplay temperatureDisplay;
        protected readonly SimpleTooltip temperatureTooltip;

        public MushFactoryPanel(IUIElement parent, int y) : base(parent, y, true)
        {
            this.statusControl.X = 0;
            this.maintenanceControl.X = 0;
            this.showDeconstructsOnRight = true;

            this.temperatureDisplay = new TemperatureDisplay(this, Scale(260), Scale(44)) { IsVisible = false };
            this.AddChild(this.temperatureDisplay);

            this.temperatureTooltip = UIHelper.AddSimpleTooltip(this, this.temperatureDisplay);

            var energy = Constants.MushFactoryEnergyUse * Constants.GlassFactoryFramesToProcess / Constants.FramesPerHour;
            this.flowDiagram = new FlowDiagramFactory(this, 0, Scale(106), this.W, ItemType.Biomass, ItemType.None, energy, Constants.MushFactoryFramesToProcess, ItemType.Mush, 1);
            this.AddChild(this.flowDiagram);

            var definition = ThingTypeManager.GetDefinition(ThingType.MushFactory);
            this.minTemperature = definition.MinTemperature;
        }

        public override void Update()
        {
            base.Update();

            if (this.building is IFactoryBuilding building && this.IsBuildingUiVisible)
            {
                this.temperatureDisplay.IsVisible = true;
                this.temperatureTooltip.IsEnabled = true;
                var temperature = (int)Math.Round(building.Temperature);
                this.temperatureDisplay.SetTemperature(temperature, temperature >= this.minTemperature ? UIColour.GreenText : UIColour.RedText);
                this.temperatureTooltip.SetTitle($"{GetString(StringsForThingPanels.MinOperatingTemperature)}: {LanguageHelper.FormatTemperature(this.minTemperature)}");
            }
            else
            {
                this.temperatureDisplay.IsVisible = false;
                this.temperatureTooltip.IsEnabled = false;
            }
        }
    }
}
