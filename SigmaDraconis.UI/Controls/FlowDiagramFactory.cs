namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;

    public class FlowDiagramFactory : FlowDiagramBase
    {
        private readonly FlowDiagramEnergyBox energyBox;
        private readonly FlowDiagramTimeBox timeBox;

        public int Frames
        {
            get => this.timeBox.Frames;
            set
            {
                this.timeBox.Frames = value;
            }
        }

        public double Energy
        {
            get => this.energyBox.Amount;
            set
            {
                this.energyBox.Amount = value;
            }
        }

        public FlowDiagramFactory(IUIElement parent, int x, int y, int width, ItemType input1, ItemType input2, double energyKWh, int timeFrames, ItemType output, int outputCount)
            : base(parent, x, y, width, input2 != ItemType.None)
        {
            this.IsInteractive = false;

            if (input1 != ItemType.None)
            {
                this.AddItemBox(input1, 1);
                this.AddPlus();
            }

            if (input2 != ItemType.None)
            {
                this.AddItemBox(input2, 1);
                this.AddPlus();
            }

            if (energyKWh > 0)
            {
                this.energyBox = this.AddEnergyBox(energyKWh);
                this.AddPlus();
            }

            this.timeBox = this.AddTimeBox(timeFrames);
            this.AddArrow();
            this.AddItemBox(output, outputCount);

            this.ApplyLayout();
        }
    }
}
