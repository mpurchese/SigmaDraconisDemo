namespace Draconis.UI
{
    using Microsoft.Xna.Framework;
    using Shared;

    public class TextRenderString
    {
        public string Text { get; set; }
        public Vector2i Postion { get; set; }
        public Vector2i ScissorRectangeSize { get; set; }
        public Color Colour { get; set; }
        public bool HasShadow { get; set; }
        public int OwnerID { get; set; }
        public int WordSpacing { get; set; }

        public TextRenderString(int ownerID, string text, Vector2i position, Color colour, Vector2i scissorRectangeSize, bool hasShadow, int wordSpacing)
        {
            this.OwnerID = ownerID;
            this.Colour = colour;
            this.Text = text;
            this.Postion = position;
            this.ScissorRectangeSize = scissorRectangeSize;
            this.HasShadow = hasShadow;
            this.WordSpacing = wordSpacing;
        }
    }
}
