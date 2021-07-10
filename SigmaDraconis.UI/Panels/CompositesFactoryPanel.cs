namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;

    public class CompositesFactoryPanel : FactoryBuildingPanel
    {
        public CompositesFactoryPanel(IUIElement parent, int y) : base(parent, y, true)
        {
            var energy = Constants.CompositesFactoryEnergyUse * Constants.CompositesFactoryFramesToProcess / Constants.FramesPerHour;
            this.flowDiagram = new FlowDiagramFactory(this, 0, Scale(106), this.W, ItemType.Metal, ItemType.Biomass, energy, Constants.CompositesFactoryFramesToProcess, ItemType.Composites, 4);
            this.AddChild(this.flowDiagram);
        }
    }
}
