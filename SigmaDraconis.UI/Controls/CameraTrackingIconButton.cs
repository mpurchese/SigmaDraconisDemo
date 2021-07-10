namespace SigmaDraconis.UI
{
    using System;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class CameraTrackingIconButton : UIElementBase, IDisposable, IButton
    {
        protected Effect effect;

        protected Texture2D borderTexture;

        public bool IsSelected { get; set; }
        public bool IsEnabled { get; set; } = true;

        public CameraTrackingIconButton(IUIElement parent, int x, int y)
            : base(parent, x, y, Scale(24), Scale(14))
        {
            this.IsInteractive = true;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            this.texture = UIStatics.Content.Load<Texture2D>("Textures\\Icons\\CameraTracking");

            this.effect = UIStatics.Content.Load<Effect>("Effects\\SimpleEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(this.texture);
        }

        protected override void DrawContent()
        {
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, UIStatics.Graphics.Viewport.Width, UIStatics.Graphics.Viewport.Height, 0, 0, 1);
            this.effect.Parameters["xViewProjection"].SetValue(projection);

            var sourceX = this.IsSelected ? Scale(48) : 0;
            if (this.IsMouseOver) sourceX += Scale(24);
            var sourceY = 0;
            if (UIStatics.Scale == 200) sourceY = 35;
            else if (UIStatics.Scale == 150) sourceY = 14;
            
            var sourceRect = new Rectangle(sourceX, sourceY, Scale(24), Scale(14));
            var targetRect = new Rectangle(this.RenderX, this.RenderY, this.W, this.H);

            // Icon
            spriteBatch.Begin(effect: this.effect);
            spriteBatch.Draw(this.texture, targetRect, sourceRect, Color.White);
            spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }
    }
}
