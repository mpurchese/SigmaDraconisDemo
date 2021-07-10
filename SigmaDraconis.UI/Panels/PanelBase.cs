namespace SigmaDraconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public abstract class PanelBase : RenderTargetElement
    {
        protected TextLabel titleLabel;
        protected DateTime lastUpdateTime;
        protected bool isShown = false;
        protected float speedMultiplier;
        protected Texture2D pixelTexture = null;

        public bool IsShown { get { return this.isShown; } }

        public event EventHandler<EventArgs> Close;

        public string Title
        {
            get
            {
                return this.titleLabel.Text;
            }
            set
            {
                this.titleLabel.Text = value;
            }
        }

        public PanelBase(IUIElement parent, int x, int y, int width, int height, string title)
            : base(parent, x, y, width, height)
        {
            this.IsVisible = false;
            this.IsInteractive = true;

            this.titleLabel = new TextLabel(this, 0, 0, this.W, Scale(14), title, UIColour.DefaultText);
            this.AddChild(this.titleLabel);

            this.speedMultiplier = this.W / 300f;

            this.backgroundColour = new Color(0, 0, 0, UIStatics.BackgroundAlpha);
        }

        public override void Update()
        {
            this.lastUpdateTime = DateTime.UtcNow;
            this.speedMultiplier = this.W / 300f;

            if (this.backgroundColour.A != UIStatics.BackgroundAlpha)
            {
                this.backgroundColour = new Color(0, 0, 0, UIStatics.BackgroundAlpha);
                this.IsContentChangedSinceDraw = true;
            }

            base.Update();
        }

        public virtual void Show()
        {
            this.IsVisible = true;
            this.isShown = true;
            this.lastUpdateTime = DateTime.UtcNow;
        }

        public virtual void Hide()
        {
            this.isShown = false;
            this.Close?.Invoke(this, new EventArgs());
        }

        public override void LoadContent()
        {
            this.pixelTexture = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color = new Color[1] { new Color(0, 0, 0, 180) };
            this.pixelTexture.SetData(color);

            base.LoadContent();
        }


        protected override void DrawBaseLayer()
        {
            Rectangle r2 = new Rectangle(this.RenderX, this.RenderY, this.W, Scale(14));

            spriteBatch.Begin();
            spriteBatch.Draw(pixelTexture, r2, Color.White);
            spriteBatch.End();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "titleLabel")]    // Children are disposed by base class
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.pixelTexture != null) this.pixelTexture.Dispose();
                base.Dispose(true);
            }
        }
    }
}
