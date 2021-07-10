namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;

    public class SolarCellFactoryPanel : FactoryBuildingPanel
    {
        public SolarCellFactoryPanel(IUIElement parent, int y) : base(parent, y, true)
        {
            var energy = Constants.SolarCellFactoryEnergyUse * Constants.SolarCellFactoryFramesToProcess / Constants.FramesPerHour;
            this.flowDiagram = new FlowDiagramFactory(this, 0, Scale(106), this.W, ItemType.Metal, ItemType.Stone, energy, Constants.SolarCellFactoryFramesToProcess, ItemType.SolarCells, 4);
            this.AddChild(this.flowDiagram);
        }
    }
}
