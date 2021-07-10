namespace Draconis.UI.Internal
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    internal class VerticalScrollBarSlider : UIElementBase, IDisposable
    {
        protected Texture2D mouseOverTexture;
        private int maxScrollPosition = 0;
        private float scrollPosition = 0;
        private float fractionVisible = 1f;
        private bool textureUpdateRequired = true;
        protected bool isMouseOverDragger;

        public bool IsMouseOverDragger => this.isMouseOverDragger;

        public VerticalScrollBarSlider(IUIElement parent, int x, int y, int height)
            : base(parent, x, y, Scale(18), height)
        {
            this.IsDraggable = true;
            this.IsInteractive = true;
        }

        public int MaxScrollPosition
        {
            get
            {
                return this.maxScrollPosition;
            }

            set
            {
                if (value != this.maxScrollPosition)
                {
                    this.maxScrollPosition = value;
                    this.IsContentChangedSinceDraw = true;
                    this.textureUpdateRequired = true;
                }
            }
        }

        public float ScrollPosition
        {
            get
            {
                return this.scrollPosition;
            }

            set
            {
                if (value != this.scrollPosition)
                {
                    this.scrollPosition = value;
                    this.IsContentChangedSinceDraw = true;
                    this.textureUpdateRequired = true;
                }
            }
        }

        public float FractionVisible
        {
            get
            {
                return this.fractionVisible;
            }

            set
            {
                if (value != this.fractionVisible)
                {
                    this.fractionVisible = value;
                    this.IsContentChangedSinceDraw = true;
                    this.textureUpdateRequired = true;
                }
            }
        }

        public override void Update()
        {
            // Only counts as mouse over if over the dragger
            var top = this.maxScrollPosition > 0 ? (int)(this.H * this.scrollPosition * (1f - this.fractionVisible) / this.maxScrollPosition) : 0;
            var bottom = top + (int)(this.H * this.fractionVisible);
            var mouseY = UIStatics.CurrentMouseState.Y - this.ScreenY;
            var newMouseOver = this.IsMouseOver && mouseY >= top && mouseY <= bottom;

            if (this.isMouseOverDragger != newMouseOver)
            {
                this.IsContentChangedSinceDraw = true;
                this.isMouseOverDragger = newMouseOver;
            }

            base.Update();
        }

        public override void ApplyLayout()
        {
            this.textureUpdateRequired = true;
            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void DrawContent()
        {
            if (this.textureUpdateRequired) this.GenerateTexture();

            Rectangle r = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);

            spriteBatch.Begin();
            spriteBatch.Draw(this.isMouseOverDragger ? this.mouseOverTexture : this.texture, r, Color.White);
            spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.mouseOverTexture != null) this.mouseOverTexture.Dispose();
            }

            base.Dispose(disposing);
        }

        protected void GenerateTexture()
        {
            this.texture = new Texture2D(UIStatics.Graphics, this.W, this.H);
            this.mouseOverTexture = new Texture2D(UIStatics.Graphics, this.texture.Width, this.texture.Height);
            Color[] color1 = new Color[this.W * this.H];
            Color[] color2 = new Color[this.W * this.H];

            var top = this.maxScrollPosition > 0 ? (int)(this.H * this.scrollPosition * (1f - this.fractionVisible) / this.maxScrollPosition) : 0;
            var bottom = top + (int)(this.H * this.fractionVisible);

            int index = 0;
            for (int y = 0; y < this.H; ++y)
            {
                for (int x = 0; x < this.W; ++x, ++index)
                {
                    if (y <= bottom - 1 && y >= top && (x == 0 || y == top || x == this.W - 1 || y == bottom - 1))
                    {
                        color1[index] = new Color(64, 64, 64, 255);
                        color2[index] = new Color(128, 128, 128, 255);
                    }
                    else if (y <= bottom - 1 && y >= top)
                    {
                        color1[index] = new Color(16, 16, 16, 64);
                        color2[index] = new Color(16, 16, 16, 64);
                    }
                    else
                    {
                        color1[index] = new Color(0, 0, 0, 0);
                        color2[index] = new Color(0, 0, 0, 0);
                    }
                }
            }

            this.texture.SetData(color1);
            this.mouseOverTexture.SetData(color2);

            this.textureUpdateRequired = false;
        }
    }
}
