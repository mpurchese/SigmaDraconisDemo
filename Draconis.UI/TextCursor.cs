namespace Draconis.UI
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    internal class TextCursor : UIElementBase
    {
        #region Private Methods

        private DateTime lastUpdateTime;
        private Color colour = new Color(192, 192, 192, 255);
        private readonly int blinkPeriod = 1000;
        private bool blinkOn = true;

        #endregion

        #region Constructor

        public TextCursor(IUIElement parent, int x, int y, int width, int height)
            : base(parent, x, y, width, height)
        {
        }

        #endregion

        public override void Update()
        {
            if (this.IsVisible == true)
            {
                if (this.blinkPeriod == 0 && this.blinkOn == false)
                {
                    this.blinkOn = true;
                    this.IsContentChangedSinceDraw = true;
                }
                else if (this.blinkPeriod > 0 && this.lastUpdateTime.AddMilliseconds(this.blinkPeriod / 2) < DateTime.UtcNow)
                {
                    this.lastUpdateTime = DateTime.UtcNow;
                    this.blinkOn = !this.blinkOn;
                    this.IsContentChangedSinceDraw = true;
                }
            }

            base.Update();
        }

        public override void LoadContent()
        {
            texture = new Texture2D(UIStatics.Graphics, 1, 1);
            Color[] textureData = new Color[1];

            textureData[0] = this.colour;
            this.texture.SetData(textureData);

            base.LoadContent();
        }

        protected override void DrawContent()
        {
            if (this.blinkOn) base.DrawContent();
            this.IsContentChangedSinceDraw = false;
        }
    }
}
