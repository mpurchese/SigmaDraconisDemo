namespace Draconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;

    public class Screen : UIElementBase, IKeyboardHandler
    {
        protected GameWindow window;
        protected bool isMouseInSafeArea;

        protected int prevHeight;
        protected int prevWidth;

        /// <summary>
        /// Gets or sets whether the game window is active.  Should be updated by the game engine.
        /// </summary>
        public bool IsWindowActive { get; set; }

        public Screen(GameWindow gameWindow)
            : base(null, 0, 0, UIStatics.Graphics.Viewport.TitleSafeArea.Width, UIStatics.Graphics.Viewport.TitleSafeArea.Height)
        {
            this.window = gameWindow;
            this.prevWidth = UIStatics.Graphics.Viewport.Width;
            this.prevHeight = UIStatics.Graphics.Viewport.Height;
        }

        public override void Update()
        {
            base.Update();

            if (this.prevWidth != UIStatics.Graphics.Viewport.Width || this.prevHeight != UIStatics.Graphics.Viewport.Height || UIStatics.Scale != this.appliedScale)
            {
                this.WidthHeightUpdate();
            }
        }

        protected void WidthHeightUpdate()
        {
            this.prevWidth = UIStatics.Graphics.Viewport.Width;
            this.prevHeight = UIStatics.Graphics.Viewport.Height;
            this.W = UIStatics.Graphics.Viewport.Width;
            this.H = UIStatics.Graphics.Viewport.Height;
            this.ApplyScale();
            this.ApplyLayout();
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

        public override void ApplyScale()
        {
            this.IsContentChangedSinceDraw = true;
        }
    }
}
