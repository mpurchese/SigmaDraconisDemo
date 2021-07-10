namespace Draconis.UI
{
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// Base class for UI elements where we optimise using a render target
    /// </summary>
    public abstract class RenderTargetElement : UIElementBase
    {
        protected Color backgroundColour;
        protected Color borderColour;
        protected bool hasBorder;
        protected Texture2D borderTexture;

        // Children will render relative to us
        public override int RenderX => 0;
        public override int RenderY => 0;

        public RenderTargetElement(IUIElement parent, int x, int y, int w, int h, bool hasBorder = false) : base(parent, x, y, w, h)
        {
            this.backgroundColour = new Color(0, 0, 0, 0);
            if (hasBorder)
            {
                this.hasBorder = true;
                this.borderColour = new Color(96, 96, 96, 255);
            }
        }

        public RenderTargetElement(IUIElement parent, int w, int h, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment, int offsetX, int offsetY) 
            : base(parent, w, h, horizontalAlignment, verticalAlignment, offsetX, offsetY)
        {
            this.backgroundColour = new Color(0, 0, 0, 0);
        }

        public override void Update()
        {
            if (!this.IsVisible) return;
            var invalidated = CollectChildContentChanges(this);
            if (invalidated) this.InvalidateTexture();

            base.Update();
        }

        private static bool CollectChildContentChanges(IUIElement element)
        {
            if (!element.IsVisible) return false;

            if (element.IsContentChangedSinceDraw) return true;
            foreach (var c in element.Children)
            {
                if (CollectChildContentChanges(c)) return true;
            }

            return false;
        }

        protected override void DrawChildren()
        {
            // Don't draw children here - this is done when we prepare the render target
        }

        protected override void ReloadContent()
        {
            if (this.W == 0 || this.H == 0) return;  // Probably screen is minimised

            if (this.texture == null || this.texture.IsDisposed)
            {
                this.texture = new RenderTarget2D(UIStatics.Graphics, this.W, this.H, false, UIStatics.Graphics.DisplayMode.Format, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }
            else if (this.texture.Width != this.W || this.texture.Height != this.H)
            {
                this.texture.Dispose();
                this.texture = new RenderTarget2D(UIStatics.Graphics, this.W, this.H, false, UIStatics.Graphics.DisplayMode.Format, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            }

            var bindings = UIStatics.Graphics.GetRenderTargets();
            var prevRenderTarget = bindings.Length > 0 ? bindings[0].RenderTarget as RenderTarget2D : null;
            UIStatics.Graphics.SetRenderTarget(this.texture as RenderTarget2D);
            UIStatics.Graphics.Clear(this.backgroundColour);

            this.DrawBaseLayer();

            foreach (var element in this.Children.OfType<IUIElement>()) element.Draw();

            if (this.hasBorder) this.DrawBorder();

            UIStatics.Graphics.SetRenderTarget(prevRenderTarget);

            base.ReloadContent();
        }

        protected virtual void DrawBaseLayer()
        {
        }

        protected virtual void DrawBorder()
        {
            if (this.borderTexture == null)
            {
                this.borderTexture = new Texture2D(UIStatics.Graphics, 1, 1);
                this.borderTexture.SetData(new Color[1] { Color.White });
            }

            var r1 = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);

            this.spriteBatch.Begin();
            this.spriteBatch.Draw(this.borderTexture, new Rectangle(r1.X, r1.Y, r1.Width, 1), this.borderColour);
            this.spriteBatch.Draw(this.borderTexture, new Rectangle(r1.X, r1.Y, 1, r1.Height), this.borderColour);
            this.spriteBatch.Draw(this.borderTexture, new Rectangle(r1.X, r1.Bottom - 1, r1.Width, 1), this.borderColour);
            this.spriteBatch.Draw(this.borderTexture, new Rectangle(r1.Right - 1, r1.Y, 1, r1.Height), this.borderColour);
            this.spriteBatch.End();
        }
    }
}
