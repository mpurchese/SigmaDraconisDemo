namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Language;
    using Shared;

    public class FlowDiagramAlgaePool : FlowDiagramBase
    {
        private readonly FlowDiagramTimeBox timeBox;
        private readonly FlowDiagramItemBox outputBox;

        public int Frames
        {
            get => this.timeBox.Frames;
            set
            {
                this.timeBox.Frames = value;
            }
        }

        public int OutputQuantity
        {
            get => this.outputBox.Quantity;
            set
            {
                this.outputBox.Quantity = value;
            }
        }

        public FlowDiagramAlgaePool(IUIElement parent, int x, int y, int width, int timeFrames, double water, int outputQuantity)
            : base(parent, x, y, width)
        {
            this.IsInteractive = false;

            this.AddWaterBox(water);
            this.AddPlus();
            this.timeBox = this.AddTimeBox(timeFrames, true);
            this.AddArrow();
            this.outputBox = this.AddItemBox(ItemType.Biomass, outputQuantity);

            UIHelper.AddSimpleTooltip(this.Parent, this.timeBox, StringsForThingPanels.ProductionTimeDepends);

            this.ApplyLayout();
        }
    }
}
