namespace Draconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    public abstract class Dialog : RenderTargetElement, IKeyboardHandler
    {
        protected TextRenderer textRenderer;
        protected TextLabel titleLabel;
        protected Texture2D pixelTexture = null;

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

        public Dialog(IUIElement parent, int w, int h, string title) : base(parent, w, h, HorizontalAlignment.Centre, VerticalAlignment.OneThird, 0, 0)
        {
            this.IsVisible = false;
            this.IsInteractive = true;
            this.textRenderer = UIStatics.TextRenderer;
            this.titleLabel = new TextLabel(this, 0, 0, w, Scale(14), title, UIStatics.DefaultTextColour);
            this.AddChild(this.titleLabel);

            this.backgroundColour = new Color(0, 0, 0, 64);
        }

        public virtual void Show()
        {
            if (this.IsVisible) return;

            if (this.currentLanguageId != UIStatics.CurrentLanguageId)
            {
                this.currentLanguageId = UIStatics.CurrentLanguageId;
                this.HandleLanguageChange();
            }

            this.IsVisible = true;
        }

        public override void LoadContent()
        {
            this.pixelTexture = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color2 = new Color[1] { new Color(0, 0, 0, 160) };
            this.pixelTexture.SetData(color2);

            base.LoadContent();
        }

        protected override void DrawBaseLayer()
        {
            Rectangle r2 = new Rectangle(0, 0, this.W, Scale(14));
            spriteBatch.Begin();
            spriteBatch.Draw(this.pixelTexture, r2, Color.White);
            spriteBatch.End();
        }

        public virtual void HandleKeyPress(Keys key)
        {
        }

        public virtual void HandleKeyHold(Keys key)
        {
        }

        public virtual void HandleKeyRelease(Keys key)
        {
        }
    }
}
