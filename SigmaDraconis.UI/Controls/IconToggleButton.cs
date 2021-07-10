namespace SigmaDraconis.UI
{
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.UI;

    public class IconToggleButton : IconButton
    {
        protected string textureOnPath;
        protected Texture2D textureOn;
        protected Texture2D textureOff;
        public bool IsOn { get; protected set; }

        public IconToggleButton(IUIElement parent, int x, int y, string textureOnPath, string textureOffPath, float scale = 1f, bool multiscaleTexture = false)
            : base(parent, x, y, textureOffPath, scale, multiscaleTexture)
        {
            this.textureOnPath = textureOnPath;
        }

        public override void LoadContent()
        {
            base.LoadContent();

            this.textureOn = UIStatics.Content.Load<Texture2D>(this.textureOnPath);
            this.textureOff = this.texture;  // Base class loaded this
        }

        public void Toggle()
        {
            if (this.IsOn) this.TurnOff();
            else this.TurnOn();
        }

        public void TurnOn()
        {
            if (!this.IsOn)
            {
                this.texture = this.textureOn;
                this.IsOn = true;
                this.IsContentChangedSinceDraw = true;
            }
        }

        public void TurnOff()
        {
            if (this.IsOn)
            {
                this.texture = this.textureOff;
                this.IsOn = false;
                this.IsContentChangedSinceDraw = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // One of our textures will be the base texture, disposed by the base class
                if (this.textureOn != null && this.textureOn != this.texture) this.textureOn.Dispose();
                if (this.textureOff != null && this.textureOff != this.texture) this.textureOff.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
