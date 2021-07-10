namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;

    public class FuelFactoryPanel : FactoryBuildingPanel
    {
        public FuelFactoryPanel(IUIElement parent, int y) : base(parent, y, true)
        {
            var energy = Constants.FuelFactoryEnergyUse * Constants.FuelFactoryFramesToProcess / Constants.FramesPerHour;
            this.flowDiagram = new FlowDiagramFactory(this, 0, Scale(106), this.W, ItemType.None, ItemType.None, energy, Constants.FuelFactoryFramesToProcess, ItemType.LiquidFuel, 1);
            this.AddChild(this.flowDiagram);
        }
    }
}
