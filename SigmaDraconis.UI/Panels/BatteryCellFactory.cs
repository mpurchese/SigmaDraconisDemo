namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;

    public class BatteryCellFactory : FactoryBuildingPanel
    {
        public BatteryCellFactory(IUIElement parent, int y) : base(parent, y, true)
        {
            var energy = Constants.BatteryCellFactoryEnergyUse * Constants.BatteryCellFactoryFramesToProcess / Constants.FramesPerHour;
            this.flowDiagram = new FlowDiagramFactory(this, 0, Scale(106), this.W, ItemType.Metal, ItemType.None, energy, Constants.BatteryCellFactoryFramesToProcess, ItemType.BatteryCells, 2);
            this.AddChild(this.flowDiagram);
        }
    }
}
