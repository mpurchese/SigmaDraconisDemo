namespace SigmaDraconis.Renderers
{
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Config;
    using Shared;
    using World;
    using World.Rooms;
    using WorldInterfaces;

    public class SnowAnimalRenderer : SinglePassAnimalRendererBase
    {
        public SnowAnimalRenderer() : base("SnowAnimals")
        {
        }

        public override void UpdateBuffers()
        {
            var things = World.GetThings<IAnimal>(ThingType.SnowTortoise).ToList();
            var size = things.Count * 4;
            if (size == 0)
            {
                this.usedBufferSize = 0;
                return;
            }

            VertexPositionColorTexture[] vertexArray = new VertexPositionColorTexture[size];
            int i = 0;
            float textureScale = WorldRenderer.Instance.TextureRes == 1 ? 2f : 1f;

            foreach (IAnimal thing in things)
            {
                var textureName = thing.GetTextureName();
                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.SnowAnimals, thing.ThingType, thing?.AnimationFrame ?? 1);
                var scale = 0.25f;

                var tile = thing.MainTile;
                var x = tile.CentrePosition.X + thing.PositionOffset.X - (0.25f * frame.CX);
                var y = tile.CentrePosition.Y + thing.PositionOffset.Y - (0.25f * frame.CY);

                float width = frame.Width * scale;
                float height = frame.Height * scale;

                float tx = frame.X * textureScale / this.textureSize.X;
                float ty = frame.Y * textureScale / this.textureSize.Y;
                float tw = frame.Width * textureScale / this.textureSize.X;
                float th = frame.Height * textureScale / this.textureSize.Y;

                var posTile = World.GetSmallTile((int)(thing.Position.X + 0.5f), (int)(thing.Position.Y + 0.5f));
                var artificialLight = RoomManager.GetTileArtificialLightLevel(posTile.Index);
                var c = new Color(artificialLight, 0f, 1f, thing.RenderAlpha);

                vertexArray[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(tx, ty));
                vertexArray[i + 1] = new VertexPositionColorTexture(new Vector3(x + width, y, 0), c, new Vector2(tx + tw, ty));
                vertexArray[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + height, 0), c, new Vector2(tx, ty + th));
                vertexArray[i + 3] = new VertexPositionColorTexture(new Vector3(x + width, y + height, 0), c, new Vector2(tx + tw, ty + th));

                i += 4;
            }

            this.UpdateIndexBuffer(vertexArray, size);
        }
    }
}
