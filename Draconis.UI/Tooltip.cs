namespace Draconis.UI
{
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public class Tooltip : RenderTargetElement
    {
        protected Texture2D titleBackgroundTexture = null;
        protected TextLabel titleLabel = null;
        protected IUIElement attachedElement = null;
        protected TextRenderer textRenderer;
        protected bool isCurrentlyVisible;

        // Tooltips share a render target
        private static Texture2D tooltipRenderTarget;

        public bool IsEnabled { get; set; } = true;

        public IUIElement AttachedElement => this.attachedElement;

        private Color titleColour;

        public Color TitleColour
        {
            get => this.titleColour;
            set
            {
                if (this.titleColour != value)
                {
                    this.titleColour = value;
                    this.titleLabel.Colour = value;
                    this.IsContentChangedSinceDraw = true;
                }
            }
        }

        public Tooltip(IUIElement parent, IUIElement attachedElement, int width, int height, string title)
            : base(parent, 0, 0, width, height)
        {
            this.attachedElement = attachedElement;

            this.IsInteractive = false;

            this.textRenderer = UIStatics.TextRenderer;

            this.titleLabel = new TextLabel(textRenderer, this, 0, Scale(1), this.W, Scale(14), title, new Color(200, 200, 200));
            this.AddChild(this.titleLabel);

            this.backgroundColour = new Color(16, 16, 16, 212);
        }

        public virtual void SetTitle(string newTitle)
        {
            this.titleLabel.Text = newTitle;
            this.UpdateWidthAndHeight();
            this.ApplyLayout();
        }

        public virtual void SetTitle(string newTitle, Color colour)
        {
            this.titleLabel.Text = newTitle;
            this.titleLabel.Colour = colour;
            this.titleColour = colour;
        }

        public override void ApplyLayout()
        {
            this.titleLabel.W = this.W;
            this.titleLabel.H = Scale(14);
            this.titleLabel.Y = Scale(1);
            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        public override void Update()
        {
            this.isCurrentlyVisible = this.attachedElement.IsMouseOver && this.attachedElement.IsVisibleIncludeParents && this.IsEnabled;
            if (!this.isCurrentlyVisible) return;

            this.UpdatePostion();
            base.Update();
        }

        protected virtual void UpdatePostion()
        {
            this.X = UIStatics.CurrentMouseState.X + Scale(16);
            this.Y = UIStatics.CurrentMouseState.Y + Scale(28);
        }

        public override void Draw()
        {
            if (!this.isCurrentlyVisible) return;
            base.Draw();
        }

        protected void RemoveChildrenExceptTitle()
        {
            foreach (var child in this.Children.Where(c => c != this.titleLabel).ToList()) this.RemoveChild(child);
        }

        public override void LoadContent()
        {
            this.titleBackgroundTexture = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color2 = new Color[1] { new Color(8, 8, 8, 212) };
            this.titleBackgroundTexture.SetData(color2);

            this.borderTexture = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color3 = new Color[1] { new Color(96, 96, 96, 255) };
            this.borderTexture.SetData(color3);

            base.LoadContent();
        }

        protected override void DrawContent()
        {
            if (this.texture != null)
            {
                var r = new Rectangle(this.X, this.Y, this.W, this.H);
                this.spriteBatch.Begin();
                this.spriteBatch.Draw(this.texture, r, Color.White);
                this.spriteBatch.End();
            }

            this.IsContentChangedSinceDraw = false;
        }

        protected override void DrawBaseLayer()
        {
            if (this.attachedElement.IsMouseOver && this.attachedElement.IsVisible && this.IsEnabled)
            {
                Rectangle r = new Rectangle(0, 0, this.W, this.H);

                spriteBatch.Begin();
                if (this.titleLabel.Text != "") spriteBatch.Draw(this.titleBackgroundTexture, new Rectangle(r.X, r.Y, r.Width, UIStatics.TextRenderer.LineHeight - 1), Color.White);
                spriteBatch.Draw(this.borderTexture, new Rectangle(r.X, r.Y, r.Width, 1), Color.White);
                spriteBatch.Draw(this.borderTexture, new Rectangle(r.X, r.Y, 1, r.Height), Color.White);
                spriteBatch.Draw(this.borderTexture, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), Color.White);
                spriteBatch.Draw(this.borderTexture, new Rectangle(r.Right - 1, r.Y, 1, r.Height), Color.White);
                spriteBatch.End();
            }
        }

        protected override void ReloadContent()
        {
            this.texture = tooltipRenderTarget;
            if (this.texture == null || this.texture.IsDisposed)
            {
                this.texture = new RenderTarget2D(UIStatics.Graphics, this.W, this.H, false, UIStatics.Graphics.DisplayMode.Format, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
            else if (this.texture.Width != this.W || this.texture.Height != this.H)
            {
                this.texture.Dispose();
                this.texture = new RenderTarget2D(UIStatics.Graphics, this.W, this.H, false, UIStatics.Graphics.DisplayMode.Format, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }

            tooltipRenderTarget = this.texture;

            UIStatics.Graphics.SetRenderTarget(this.texture as RenderTarget2D);
            UIStatics.Graphics.Clear(this.backgroundColour);

            this.DrawBaseLayer();

            foreach (var element in this.Children.OfType<IUIElement>()) element.Draw();

            UIStatics.Graphics.SetRenderTarget(null);

            this.isContentInvalidated = false;
        }

        protected virtual void UpdateWidthAndHeight()
        {
            this.H = Scale(15);
            this.W = Scale(8) + (this.titleLabel.Text.Length * 7 * UIStatics.Scale / 100);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.borderTexture != null) this.borderTexture.Dispose();
                if (this.titleBackgroundTexture != null) this.titleBackgroundTexture.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
