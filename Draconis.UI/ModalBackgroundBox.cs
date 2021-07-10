namespace Draconis.UI
{
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public class ModalBackgroundBox : UIElementBase
    {
        private int alpha = 250;

        public static ModalBackgroundBox Instance { get; private set; }

        /// <summary>
        /// Gets or sets whether this box is shown.  Different from IsVisible, as fades in and out.
        /// </summary>
        public bool IsShown { get; set; }

        public ModalBackgroundBox(Screen parent)
            : base(parent, 0, 0, parent.W, parent.H)
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                throw new System.ApplicationException("ModelBackgroundBox instance already created");
            }
        }

        public override void Update()
        {
            this.W = this.Parent.W;
            this.H = this.Parent.H;

            if ((!this.IsInteractive || this.alpha > 120) && this.alpha > 0)
            {
                this.IsMouseOver = false;
                this.alpha -= 10;
                if (this.alpha < 0)
                {
                    this.alpha = 0;
                }
            }
            else if (this.IsInteractive && this.alpha < 120)
            {
                this.alpha += 10;
                this.IsMouseOver = false;
            }
            else if (this.IsInteractive)
            {
                this.IsMouseOver = true;
                foreach (IUIElement child in this.Children) child.Update();
            }
            else
            {
                this.IsMouseOver = false;
            }
        }

        public override void LoadContent()
        {
            this.texture = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] color = new Color[1] { new Color(0, 0, 0, 255) };
            this.texture.SetData(color);

            base.LoadContent();
        }

        public override void ApplyScale()
        {
            this.IsContentChangedSinceDraw = true;
        }

        public override void ApplyLayout()
        {
            this.W = this.Parent.W;
            this.H = this.Parent.H;

            foreach (var dialog in this.Children.OfType<Dialog>())
            {
                dialog.ApplyScale();
                dialog.UpdateHorizontalPosition(this.W);
                dialog.UpdateVerticalPosition(this.H);
                dialog.ApplyLayout();
            }

            this.appliedScale = UIStatics.Scale;
            this.IsContentChangedSinceDraw = true;
        }

        protected override void DrawContent()
        {
            if (this.IsInteractive || this.alpha > 0)
            {
                Rectangle r = new Rectangle(this.Parent.ScreenX, this.Parent.ScreenY, this.Parent.W, this.Parent.H);

                spriteBatch.Begin();
                spriteBatch.Draw(texture, r, new Color(Color.White, this.alpha));
                spriteBatch.End();
            }

            this.IsContentChangedSinceDraw = false;
        }
    }
}
