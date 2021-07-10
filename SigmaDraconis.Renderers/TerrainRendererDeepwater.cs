namespace SigmaDraconis.Renderers
{
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Config;
    using World;

    public class TerrainRendererDeepwater : TerrainRendererBase
    {
        public override void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.effect = contentManager.Load<Effect>("Effects\\GeneralEffect").Clone();
            var texture1 = contentManager.Load<Texture2D>("Textures\\Tiles\\TerrainDirtDeepwater_1");
            var texture2 = contentManager.Load<Texture2D>("Textures\\Tiles\\TerrainDirtDeepwater_2");
            this.effect.Parameters["xTexture1"].SetValue(texture1);
            this.effect.Parameters["xTexture2"].SetValue(texture2);
            this.textureSize = new Vector2i(texture1.Width, texture1.Height);
            base.LoadContent(graphicsDevice, contentManager);
        }

        public override void UpdateBuffers()
        {
            this.isBufferInvalidated = false;

            var size = World.BigTilesWithDeepWaterEdge.Count * 4;
            if (size == 0) return;

            var vertexArray = new VertexPositionColorTexture[size];
            int i = 0;

            var c = new Color(0f, 0f, 1f, 1f);
            foreach (var tile in World.BigTilesWithDeepWaterEdge)
            {
                var textureName = tile.GetTextureName();
                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.TerrainDirtDeepwater, textureName);

                var x = tile.CentrePosition.X - (frame.CX * 0.25f);
                var y = tile.CentrePosition.Y - (frame.CY * 0.25f);

                float tx = frame.X / (float)this.textureSize.X;
                float ty = frame.Y / (float)this.textureSize.Y;
                float tw = frame.Width / (float)this.textureSize.X;
                float th = frame.Height / (float)this.textureSize.Y;

                vertexArray[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(tx, ty));
                vertexArray[i + 1] = new VertexPositionColorTexture(new Vector3(x + (frame.Width * 0.25f), y, 0), c, new Vector2(tx + tw, ty));
                vertexArray[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + (frame.Height * 0.25f), 0), c, new Vector2(tx, ty + th));
                vertexArray[i + 3] = new VertexPositionColorTexture(new Vector3(x + (frame.Width * 0.25f), y + (frame.Height * 0.25f), 0), c, new Vector2(tx + tw, ty + th));

                i += 4;
            }

            // Set up index buffer
            var indexBufferSize = (size * 3) / 2;
            var indexArray = new int[indexBufferSize];
            for (int k = 0, j = 0; j < size; k += 6, j += 4)
            {
                indexArray[k] = j;
                indexArray[k + 1] = j + 1;
                indexArray[k + 2] = j + 2;
                indexArray[k + 3] = j + 2;
                indexArray[k + 4] = j + 1;
                indexArray[k + 5] = j + 3;
            }

            if (this.vertexBuffer == null || this.vertexBuffer.IsContentLost || this.vertexBuffer.IsDisposed || this.vertexBuffer.VertexCount < size)
            {
                if (this.vertexBuffer?.IsDisposed == false)
                {
                    // Destroy the old buffer, it's invalidated or too small
                    this.vertexBuffer.Dispose();
                    this.vertexBuffer = null;
                }

                this.vertexBuffer = new DynamicVertexBuffer(this.graphics, VertexPositionColorTexture.VertexDeclaration, (int)(size * bufferGrowthFactor), BufferUsage.WriteOnly);
            }

            this.vertexBuffer.SetData(vertexArray);
            this.usedBufferSize = size;

            if (this.indexBuffer == null || this.indexBuffer.IsContentLost || this.indexBuffer.IsDisposed || this.indexBuffer.IndexCount < indexBufferSize)
            {
                if (this.indexBuffer?.IsDisposed == false)
                {
                    // Destroy the old buffer, it's invalidated or too small
                    this.indexBuffer.Dispose();
                    this.indexBuffer = null;
                }

                this.indexBuffer = new DynamicIndexBuffer(this.graphics, typeof(int), (int)(indexBufferSize * bufferGrowthFactor), BufferUsage.WriteOnly);
            }

            this.indexBuffer.SetData(indexArray);
        }
    }
}
