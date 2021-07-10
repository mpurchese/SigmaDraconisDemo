namespace SigmaDraconis.Shared
{
    using Microsoft.Xna.Framework;
    using Draconis.Shared;

    public class Light
    {
        public Light(Vector2i terrainPosition, Color colour, float range)
        {
            this.TerrainPosition = terrainPosition;
            this.LightColour = colour;
            this.Range = range;
        }

        public Vector2i TerrainPosition { get; set; }

        public Color LightColour { get; set; } = Color.White;

        public float Range { get; set; } = 10f;
    }
}
