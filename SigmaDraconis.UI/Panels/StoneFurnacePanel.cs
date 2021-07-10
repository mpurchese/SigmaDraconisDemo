namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;

    public class StoneFurnacePanel : FactoryBuildingPanel
    {
        public StoneFurnacePanel(IUIElement parent, int y) : base(parent, y)
        {
            this.flowDiagram = new FlowDiagramFactory(this, 0, Scale(106), this.W, ItemType.IronOre, ItemType.Coal, 0, Constants.StoneFurnaceFramesToProcess, ItemType.Metal, 1);
            this.AddChild(this.flowDiagram);
        }
    }
}
