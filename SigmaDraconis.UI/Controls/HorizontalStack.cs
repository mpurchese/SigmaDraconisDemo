namespace SigmaDraconis.UI
{
    using System.Linq;
    using Draconis.Shared;
    using Draconis.UI;

    public class HorizontalStack : UIElementBase
    {
        private TextAlignment alignment;
        private int paddingTop = 5;
        private int paddingRight = 5;
        private int paddingBottom = 5;
        private int paddingLeft = 5;
        private int spacing = 5;

        public TextAlignment Alignment
        {
            get { return this.alignment; }
            set { this.alignment = value; this.LayoutInvalidated = true; }
        }

        public int PaddingTop
        {
            get { return this.paddingTop; }
            set { this.paddingTop = value; this.LayoutInvalidated = true; }
        }

        public int PaddingRight
        {
            get { return this.paddingRight; }
            set { this.paddingRight = value; this.LayoutInvalidated = true; }
        }

        public int PaddingBottom
        {
            get { return this.paddingBottom; }
            set { this.paddingBottom = value; this.LayoutInvalidated = true; }
        }

        public int PaddingLeft
        {
            get { return this.paddingLeft; }
            set { this.paddingLeft = value; this.LayoutInvalidated = true; }
        }

        public int Spacing
        {
            get { return this.spacing; }
            set { this.spacing = value; this.LayoutInvalidated = true; }
        }

        public bool LayoutInvalidated { get; set; }

        public HorizontalStack(IUIElement parent, int x, int y, int width, int height, TextAlignment alignment)
            : base(parent, x, y, width, height)
        {
            this.Alignment = alignment;
            this.IsInteractive = false;
        }

        public override void Update()
        {
            if (this.LayoutInvalidated) this.UpdateLayout();
            base.Update();
        }

        private void UpdateLayout()
        {
            var x = this.PaddingLeft;
            if (this.Alignment.In(TextAlignment.TopRight, TextAlignment.MiddleRight, TextAlignment.BottomRight)) x = this.W + this.Spacing - this.PaddingRight;
            else if (this.Alignment.In(TextAlignment.TopCentre, TextAlignment.MiddleCentre, TextAlignment.BottomCentre)) x = this.PaddingLeft + ((this.W + this.Spacing - (this.PaddingLeft + this.PaddingRight)) / 2);

            foreach (var child in this.Children.Where(c => c.IsVisible))
            {
                var offsetY = child is LeftRightPicker ? -2 : 0;
                if (this.Alignment.In(TextAlignment.TopLeft, TextAlignment.TopCentre, TextAlignment.TopRight)) child.Y = this.PaddingTop + offsetY;
                else if (this.Alignment.In(TextAlignment.BottomLeft, TextAlignment.BottomCentre, TextAlignment.BottomRight)) child.Y = this.PaddingBottom + offsetY - child.H;
                else child.Y = this.PaddingTop + offsetY + ((this.H - (this.PaddingTop + this.PaddingBottom)) / 2) - (child.H / 2);

                if (this.Alignment.In(TextAlignment.TopRight, TextAlignment.MiddleRight, TextAlignment.BottomRight)) x -= child.W + this.Spacing;
                else if (this.Alignment.In(TextAlignment.TopCentre, TextAlignment.MiddleCentre, TextAlignment.BottomCentre)) x -= ((child.W + this.Spacing) / 2);
            }

            foreach (var child in this.Children.Where(c => c.IsVisible))
            {
                child.X = x;
                x += child.W + this.Spacing;
            }

            this.LayoutInvalidated = false;
        }
    }
}
