namespace SigmaDraconis.UI
{
    using Draconis.UI;

    public class ConstructPageButton : IconButton
    {
        protected TextLabel textLabel;

        public int Page { get; private set; }

        public ConstructPageButton(IUIElement parent, int x, int y, int page)
            : base(parent, x, y, $"Textures\\Icons\\ConstructPage{page}", 1f, true)
        {
            this.Page = page;
            this.textLabel = new TextLabel(this, UIStatics.Scale == 200 ? -1 : 0, Scale(18) + (UIStatics.Scale > 100 ? 1 : 0), Scale(36), Scale(18), "0/0", UIColour.GrayText);
            this.AddChild(textLabel);
        }

        public void SetCounts(int countCanBuild, int countTotal)
        {
            this.textLabel.Text = $"{countCanBuild}/{countTotal}";
            this.textLabel.Colour = countCanBuild > 0 ? UIColour.GrayText : UIColour.DarkGrayText;
        }

        public override void ApplyLayout()
        {
            this.textLabel.X = UIStatics.Scale == 200 ? -1 : 0;
            this.textLabel.Y = Scale(18) + (UIStatics.Scale > 100 ? 1 : 0);
            this.textLabel.W = Scale(36);
            this.textLabel.H = Scale(18);
            this.textLabel.ApplyLayout();

            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }
    }
}
