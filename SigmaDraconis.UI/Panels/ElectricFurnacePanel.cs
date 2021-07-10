namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;

    public class ElectricFurnacePanel : FactoryBuildingPanel
    {
        public ElectricFurnacePanel(IUIElement parent, int y) : base(parent, y, true)
        {
            var energy = Constants.ElectricFurnaceEnergyUse * Constants.ElectricFurnaceFramesToProcess / Constants.FramesPerHour;
            this.flowDiagram = new FlowDiagramFactory(this, 0, Scale(106), this.W, ItemType.IronOre, ItemType.None, energy, Constants.ElectricFurnaceFramesToProcess, ItemType.Metal, 1);
            this.AddChild(this.flowDiagram);
        }
    }
}
