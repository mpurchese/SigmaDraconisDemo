namespace SigmaDraconis.UI
{
    using System.Linq;
    using Draconis.UI;

    public class VerticalStack : UIElementBase
    {
        private int spacing = 2;

        public int Spacing
        {
            get { return this.spacing; }
            set { this.spacing = value; this.LayoutInvalidated = true; }
        }

        public bool LayoutInvalidated { get; set; }
        public bool IsAutoHeight { get; set; }

        public VerticalStack(IUIElement parent, int x, int y, int width, int height, bool isAutoHeight = false)
            : base(parent, x, y, width, height)
        {
            this.IsInteractive = false;
            this.IsAutoHeight = isAutoHeight;
        }

        public override void Update()
        {
            if (this.LayoutInvalidated) this.UpdateLayout();
            base.Update();
        }

        private void UpdateLayout()
        {
            var y = 0;

            foreach (var child in this.Children.Where(c => c.IsVisible))
            {
                if (child is TextLabel t && t.Text == "") continue;

                child.Y = y;
                if (child.H > 0) y += child.H + this.Spacing;
            }

            if (this.IsAutoHeight) this.H = y;

            this.IsContentChangedSinceDraw = true;
            this.LayoutInvalidated = false;
        }
    }
}
