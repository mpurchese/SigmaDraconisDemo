namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using Shared;

    public class TickBoxIconButtonWithCount : TickBoxIconButton
    {
        protected TextLabel textLabel;

        private int count;

        public int Count
        {
            get
            {
                return this.count;
            }
            set
            {
                if (this.count != value)
                {
                    this.count = value;
                    this.IsContentChangedSinceDraw = true;
                    this.textLabel.Text = value.ToString();
                }
            }
        }

        public ItemType ItemType { get; private set; }

        public TickBoxIconButtonWithCount(IUIElement parent, int x, int y, ItemType itemType) : base(parent, x, y, Scale(64), Scale(22), new Icon("Textures\\Icons\\Items", 13), (int)itemType - 1)
        {
            this.textLabel = new TextLabel(this, Scale(48), Scale(8), "0", UIColour.DefaultText);
            this.AddChild(this.textLabel);
        }

        protected override void DrawContent()
        {
            this.backgroundColour = (this.IsTicked && this.count > 0) ? UIColour.GreenText * .2f : UIColour.RedText * 0.2f;
            base.DrawContent();
        }
    }
}
