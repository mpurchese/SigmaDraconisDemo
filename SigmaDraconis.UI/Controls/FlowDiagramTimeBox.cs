namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class FlowDiagramTimeBox : FlowDiagramBoxBase
    {
        private bool isWide;
        private int frames = 0;
        private int hours = 0;
        private int minutes = 0;

        public int Frames
        {
            get => this.frames;
            set
            {
                if (this.frames != value)
                {
                    this.frames = value;
                    var totalMinutes = value / 60;
                    this.minutes = totalMinutes % 60;
                    this.hours = totalMinutes / 60;
                    this.UpdateLabel();
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public FlowDiagramTimeBox(IUIElement parent, int frames, bool wide = false) : base(parent, 0, 0, Scale(wide ? 56 : 50) + 2)
        {
            this.isWide = wide;

            this.label = new TextLabel(this, Scale(UIStatics.Scale == 200 ? 16 : 18), 0, Scale(34), this.H, "", UIColour.DefaultText, TextAlignment.MiddleCentre, true);
            this.AddChild(this.label);

            this.Frames = frames;
            this.UpdateLabel();
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\FlowDiagramTime");
            base.LoadContent();
        }

        public override void ApplyScale()
        {
            this.W = Scale(this.isWide ? 56 : 50) + 2;
            this.H = Scale(16) + 3;
            this.IsContentChangedSinceDraw = true;
        }

        public override void ApplyLayout()
        {
            this.label.X = Scale(UIStatics.Scale == 200 ? 16 : 18);
            this.label.W = Scale(34);
            this.label.H = this.H;
            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void DrawIcon()
        {
            var rSource = this.GetTextureSourceRect();
            var rDest = new Rectangle(this.RenderX, this.RenderY, rSource.Value.Width, rSource.Value.Height);
            spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
        }

        private void UpdateLabel()
        {
            this.label.Text = this.frames > 0 ? $"{this.hours}:{this.minutes:D2}" : "-:--";
            this.IsContentChangedSinceDraw = true;
        }

        private Rectangle? GetTextureSourceRect()
        {
            if (UIStatics.Scale == 200) return new Rectangle(0, 46, 35, 35);
            if (UIStatics.Scale == 150) return new Rectangle(0, 19, 27, 27);
            return new Rectangle(0, 0, 19, 19);
        }
    }
}
