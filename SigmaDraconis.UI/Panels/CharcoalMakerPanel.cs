namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;

    public class CharcoalMakerPanel : FactoryBuildingPanel
    {
        public CharcoalMakerPanel(IUIElement parent, int y) : base(parent, y)
        {
            this.flowDiagram = new FlowDiagramFactory(this, 0, Scale(106), this.W, ItemType.Biomass, ItemType.None, 0, Constants.CharcoalMakerFramesToProcess, ItemType.Coal, 1);
            this.AddChild(this.flowDiagram);
        }
    }
}
