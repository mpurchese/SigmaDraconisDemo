namespace Draconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public class Icon
    {
        protected string texturePath;
        protected readonly int frameCount;
        protected Texture2D texture = null;
        protected Texture2D borderTexture = null;
        protected int frameSizeX = 0;
        protected int frameSizeY = 0;
        protected bool isMultiscaleTecture;
        protected bool hasBorder;

        protected Color borderColour = new Color(96, 96, 96, 255);

        public Icon(string texturePath, int frameCount = 1, bool isMultiscaleTecture = true, bool hasBorder = false)
        {
            this.texturePath = texturePath;
            this.frameCount = frameCount;
            this.isMultiscaleTecture = isMultiscaleTecture;
            this.hasBorder = hasBorder;
        }

        public void LoadContent(string texturePath = null)
        {
            if (texturePath != null) this.texturePath = texturePath;

            this.texture = UIStatics.Content.Load<Texture2D>(this.texturePath);
            this.frameSizeX = (this.texture.Width / this.frameCount) / 2;
            this.frameSizeY = this.isMultiscaleTecture ? (this.texture.Height * 2) / 9 : this.texture.Height / 2;

            if (this.hasBorder && this.borderTexture == null)
            {
                this.borderTexture = new Texture2D(UIStatics.Graphics, 1, 1);
                this.borderTexture.SetData(new Color[1] { Color.White });
            }
        }

        /// <summary>
        /// Draw the icon.  Calling code is responsible for calling Begin and End on the SpriteBatch.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, int x = 0, int y = 0, int frame = 0)
        {
            if (frame < 0 || frame >= frameCount) return;

            var sy = 0;
            if (this.isMultiscaleTecture)
            {
                if (UIStatics.Scale == 200) sy = (this.frameSizeY * 5) / 2;
                else if (UIStatics.Scale == 150) sy = this.frameSizeY;
            }

            var width = Scale(this.frameSizeX);
            var height = Scale(this.frameSizeY);

            var rSource = this.isMultiscaleTecture
                ? new Rectangle(frame * width, sy, width, height) 
                : new Rectangle(frame * this.texture.Width / this.frameCount, 0, this.texture.Width / this.frameCount, this.texture.Height);

            var rDest = new Rectangle(x, y, width, height);
            spriteBatch.Draw(this.texture, rDest, rSource, Color.White);

            if (this.hasBorder)
            {
                spriteBatch.Draw(this.borderTexture, new Rectangle(x, y, width, 1), this.borderColour);
                spriteBatch.Draw(this.borderTexture, new Rectangle(x, y, 1, height), this.borderColour);
                spriteBatch.Draw(this.borderTexture, new Rectangle(x, y + height - 1, width, 1), this.borderColour);
                spriteBatch.Draw(this.borderTexture, new Rectangle(x + width - 1, y, 1, height), this.borderColour);
            }
        }

        private static int Scale(int coord)
        {
            return coord * UIStatics.Scale / 100;
        }
    }
}
