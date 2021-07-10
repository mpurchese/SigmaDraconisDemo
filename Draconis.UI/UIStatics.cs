namespace Draconis.UI
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    public static class UIStatics
    {
        // Store these as statics so we don't have to pass them around everywhere
        public static ContentManager Content { get; set; }
        public static GraphicsDevice Graphics { get; set; }
        public static TextRenderer TextRenderer { get; set; }

        public static int Scale { get; set; } = 100;
        public static Color DefaultTextColour { get { return new Color(200, 200, 200); } }
        public static int BackgroundAlpha = 64;

        public static MouseState PreviousMouseState;
        public static MouseState CurrentMouseState;

        public static string TickBoxIconPath = "Textures\\Icons\\TickBox";
        public static string RadioBoxIconPath = "Textures\\Icons\\RadioBox";

        public static int CurrentLanguageId = 0;
    }
}
