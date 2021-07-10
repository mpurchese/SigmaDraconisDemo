namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;

    public class FlowDiagramPowerStation : FlowDiagramBase
    {
        private readonly FlowDiagramTimeBox timeBox;

        public int Frames
        {
            get => this.timeBox.Frames;
            set
            {
                this.timeBox.Frames = value;
            }
        }

        public FlowDiagramPowerStation(IUIElement parent, int x, int y, int width, ItemType fuelType, int timeFrames, double water, double outputKWh)
            : base(parent, x, y, width)
        {
            this.IsInteractive = false;

            this.AddItemBox(fuelType, 1);
            this.AddPlus();
            this.AddWaterBox(water);
            this.AddPlus();
            this.timeBox = this.AddTimeBox(timeFrames);
            this.AddArrow();
            this.AddEnergyBox(outputKWh);

            this.ApplyLayout();
        }
    }
}
