namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class WindIcon : UIElementBase
    {
        public WindIcon(IUIElement parent, int x, int y)
            : base(parent, x, y, Scale(18), Scale(18))
        {
        }

        public override void LoadContent()
        {
            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\Wind");
            base.LoadContent();
        }

        protected override void DrawContent()
        {
            if (this.texture != null)
            {
                var size = Scale(18);
                var sy = 0;
                if (UIStatics.Scale == 200) sy = 45;
                if (UIStatics.Scale == 150) sy = 18;
                var rSource = new Rectangle(0, sy, size, size);

                var rDest = new Rectangle(this.RenderX, this.RenderY, size, size);
                this.spriteBatch.Begin();
                this.spriteBatch.Draw(this.texture, rDest, rSource, Color.White);
                this.spriteBatch.End();
            }

            this.IsContentChangedSinceDraw = false;
        }
    }
}
