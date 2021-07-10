namespace SigmaDraconis.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using Draconis.UI;
    using Shared;

    public abstract class FlowDiagramBase : UIElementBase
    {
        private readonly Dictionary<IUIElement, int> widthAllocations = new Dictionary<IUIElement, int>();
        private readonly bool isCompact;
        private bool isEnabled = true;

        public bool IsEnabled
        {
            get => this.isEnabled;
            set
            {
                if (this.isEnabled == value) return;
                this.isEnabled = value;
                foreach (var box in this.Children.OfType<FlowDiagramBoxBase>()) box.IsEnabled = value;
            }
        }

        public FlowDiagramBase(IUIElement parent, int x, int y, int width, bool isCompact = false)
            : base(parent, x, y, width, Scale(20))
        {
            this.IsInteractive = false;
            this.isCompact = isCompact;
        }

        public override void ApplyLayout()
        {
            foreach (var child in this.Children)
            {
                child.ApplyScale();
                child.ApplyLayout();
                this.DoWidthAllocation(child);
            }

            var totalWidthAllocation = this.Children.Sum(c => this.widthAllocations[c]);
            var childX = (this.W - totalWidthAllocation) / 2;

            foreach (var child in this.Children)
            {
                child.X = childX;
                if (child is TextLabel) child.Y = Scale(2) + 1;
                childX += this.widthAllocations[child];
            }

            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected FlowDiagramItemBox AddItemBox(ItemType itemType, int quantity)
        {
            var box = new FlowDiagramItemBox(this, itemType, quantity);
            this.Add(box);
            return box;
        }

        protected FlowDiagramEnergyBox AddEnergyBox(double amount)
        {
            var box = new FlowDiagramEnergyBox(this, amount);
            this.Add(box);
            return box;
        }

        protected FlowDiagramTimeBox AddTimeBox(int frames, bool isWide = false)
        {
            var box = new FlowDiagramTimeBox(this, frames, isWide);
            this.Add(box);
            return box;
        }

        protected FlowDiagramWaterBox AddWaterBox(double amount)
        {
            var box = new FlowDiagramWaterBox(this, amount);
            this.Add(box);
            return box;
        }

        protected void AddArrow()
        {
            var label = new TextLabel(this, 0, Scale(2) + 1, Scale(isCompact ? 12 : 20), Scale(14), ((char)127).ToString(), UIColour.DefaultText);
            this.Add(label);
        }

        protected void AddPlus()
        {
            var label = new TextLabel(this, 0, Scale(2) + 1, Scale(isCompact ? 8 : 12), Scale(14), "+", UIColour.DefaultText);
            this.Add(label);
        }

        private void Add(IUIElement element)
        {
            this.DoWidthAllocation(element);
            this.AddChild(element);
        }

        private void DoWidthAllocation(IUIElement child)
        {
            //var w = (child is TextLabel t) ? Scale(t.Text == "+" ? 12 : 20) : child.W;

            if (this.widthAllocations.ContainsKey(child)) this.widthAllocations[child] = child.W;
            else this.widthAllocations.Add(child, child.W);
        }
    }
}
