namespace SigmaDraconis.Shared
{
    using Draconis.Shared;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    public class CoordinateHelper
    {
        public static Vector2 GetWorldPosition(GraphicsDevice graphicsDevice, Vector2f scrollPosition, float zoom, int screenX, int screenY)
        {
            var projection = Matrix.CreateTranslation(-scrollPosition.X / 2f, -scrollPosition.Y / 2f, 0f)
                * Matrix.CreateTranslation(-graphicsDevice.Viewport.Width * 0.5f, -graphicsDevice.Viewport.Height * 0.5f, 0f)
                * Matrix.CreateScale(zoom)
                * Matrix.CreateTranslation(graphicsDevice.Viewport.Width * 0.5f, graphicsDevice.Viewport.Height * 0.5f, 0f);

            return Vector2.Transform(new Vector2(screenX, screenY), Matrix.Invert(projection));
        }

        public static Vector2 GetScreenPosition(GraphicsDevice graphicsDevice, Vector2f scrollPosition, float zoom, float worldX, float worldY)
        {
            var projection = Matrix.CreateTranslation(-scrollPosition.X / 2f, -scrollPosition.Y / 2f, 0f)
                * Matrix.CreateTranslation(-graphicsDevice.Viewport.Width * 0.5f, -graphicsDevice.Viewport.Height * 0.5f, 0f)
                * Matrix.CreateScale(zoom)
                * Matrix.CreateTranslation(graphicsDevice.Viewport.Width * 0.5f, graphicsDevice.Viewport.Height * 0.5f, 0f);

            return Vector2.Transform(new Vector2(worldX, worldY), projection);
        }
    }
}
