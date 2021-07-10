namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;

    public class GlassFactoryPanel : FactoryBuildingPanel
    {
        public GlassFactoryPanel(IUIElement parent, int y) : base(parent, y, true)
        {
            var energy = Constants.GlassFactoryEnergyUse * Constants.GlassFactoryFramesToProcess / Constants.FramesPerHour;
            this.flowDiagram = new FlowDiagramFactory(this, 0, Scale(106), this.W, ItemType.Stone, ItemType.None, energy, Constants.GlassFactoryFramesToProcess, ItemType.Glass, 2);
            this.AddChild(this.flowDiagram);
        }
    }
}
