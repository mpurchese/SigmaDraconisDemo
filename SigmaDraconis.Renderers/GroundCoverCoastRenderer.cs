namespace SigmaDraconis.Renderers
{
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Config;
    using Shared;
    using World;

    using Vertices;

    public class GroundCoverCoastRenderer : IRenderer, IDisposable
    {
        private static readonly float bufferGrowthFactor = 1.5f;

        private DynamicVertexBuffer vertexBuffer;
        private DynamicIndexBuffer indexBuffer;
        private Effect effect;
        private GraphicsDevice graphics;
        private ContentManager content;
        protected Vector2 textureSize;
        protected Vector2 textureSize2;
        private readonly Vector2f scrollPosition = new Vector2i(0, 0);
        private float zoom;
        private int viewWidth;
        private int viewHeight;
        private int usedBufferSize = 0;
        private bool isBufferInvalidated = true;

        public Effect LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;

            var maskTexture = contentManager.Load<Texture2D>("Textures\\Tiles\\TerrainCoastMask");

            this.effect = content.Load<Effect>("Effects\\GeneralEffectWithCoastMask").Clone();
            this.effect.Parameters["xTextureMask"].SetValue(maskTexture);
            this.textureSize2 = new Vector2(maskTexture.Width, maskTexture.Height);

            this.ReloadContent();

            return this.effect;
        }

        public void ReloadContent()
        {
            var name = "GroundCover";
            var texture1 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(WorldRenderer.Instance.TextureRes == 0 ? $"PlantsAndRocks\\LoRes\\{name}_1" : $"PlantsAndRocks\\{name}_1");
            var texture2 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(WorldRenderer.Instance.TextureRes == 0 ? $"PlantsAndRocks\\LoRes\\{name}_2" : $"PlantsAndRocks\\{name}_2");
            this.effect.Parameters["xTexture1"].SetValue(texture1);
            this.effect.Parameters["xTexture2"].SetValue(texture2);
            this.textureSize = new Vector2(texture1.Width, texture1.Height);
        }

        public void InvalidateBuffers()
        {
            this.isBufferInvalidated = true;
        }

        public void Update(Vector2f scrollPos, float zoom)
        {
            var invalidateBuffers = this.isBufferInvalidated || this.graphics.Viewport.Width != this.viewWidth || this.graphics.Viewport.Height != this.viewHeight;
            var isProjectionInvalidated = this.scrollPosition == null
                || scrollPos.X != this.scrollPosition.X
                || scrollPos.Y != this.scrollPosition.Y
                || this.graphics.Viewport.Width != this.viewWidth
                || this.graphics.Viewport.Height != this.viewHeight
                || this.zoom != zoom;

            if (isProjectionInvalidated)
            {
                this.viewWidth = this.graphics.Viewport.Width;
                this.viewHeight = this.graphics.Viewport.Height;
                var sx = scrollPos.X / 2f;
                var sy = scrollPos.Y / 2f;
                Matrix projection = Matrix.CreateOrthographicOffCenter(sx, sx + this.graphics.Viewport.Width, sy + this.graphics.Viewport.Height, sy, 0, 1) * Matrix.CreateScale(zoom);
                this.effect.Parameters["xViewProjection"].SetValue(projection);
            }

            this.scrollPosition.X = scrollPos.X;
            this.scrollPosition.Y = scrollPos.Y;
            this.zoom = zoom;

            if (invalidateBuffers)
            {
                this.UpdateBuffers();
            }
        }

        public void UpdateBuffers()
        {
            this.isBufferInvalidated = false;

            var tiles = World.SmallTiles.Where(t => t.GroundCoverDensity > 0 && t.TerrainType == TerrainType.Coast).ToList();
            var size = tiles.Count * 4;
            if (size == 0)
            {
                this.usedBufferSize = 0;
                return;
            }

            VertexPositionColorTextureTexture[] vertexArray = new VertexPositionColorTextureTexture[size];
            int i = 0;
            var c = new Color(0f, 0f, 1f, 1f);

            foreach (var tile in tiles)
            {
                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.GroundCover, $"GroundCover_{tile.GroundCoverDensity}_{tile.GroundCoverDirection.ToString()}");
                var scale = 0.25f;

                var x = tile.CentrePosition.X - (scale * frame.CX);
                var y = tile.CentrePosition.Y - (scale * frame.CY);

                float width = frame.Width * scale;
                float height = frame.Height * scale;

                float tx = frame.X / this.textureSize.X;
                float ty = frame.Y / this.textureSize.Y;
                float tw = frame.Width / this.textureSize.X;
                float th = frame.Height / this.textureSize.Y;

                var bigTile = tile.BigTile;
                var bigTileFrame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.TerrainCoastMask, bigTile.GetTextureName());
                var ox = 1 / 3f;
                var oy = 1 / 3f;
                if (tile == bigTile.SmallTiles[Direction.W]) ox = 0;
                else if (tile == bigTile.SmallTiles[Direction.NW] || tile == bigTile.SmallTiles[Direction.SW]) ox = 1 / 6f;
                else if (tile == bigTile.SmallTiles[Direction.NE] || tile == bigTile.SmallTiles[Direction.SE]) ox = 1 / 2f;
                else if (tile == bigTile.SmallTiles[Direction.E]) ox = 2 / 3f;
                if (tile == bigTile.SmallTiles[Direction.N]) oy = 0;
                else if (tile == bigTile.SmallTiles[Direction.NW] || tile == bigTile.SmallTiles[Direction.NE]) oy = 1 / 6f;
                else if (tile == bigTile.SmallTiles[Direction.SW] || tile == bigTile.SmallTiles[Direction.SE]) oy = 1 / 2f;
                else if (tile == bigTile.SmallTiles[Direction.S]) oy = 2 / 3f;

                float tx2 = (bigTileFrame.X + (bigTileFrame.Width * ox)) / this.textureSize2.X;
                float ty2 = ((bigTileFrame.Y + (bigTileFrame.Height * oy)) - 1f) / this.textureSize2.Y;
                float tw2 = bigTileFrame.Width / (3f * this.textureSize2.X);
                float th2 = bigTileFrame.Height / (3f * this.textureSize2.Y);

                if (WorldRenderer.Instance.TextureRes == 1)
                {
                    tx *= 2;
                    ty *= 2;
                    tw *= 2;
                    th *= 2;
                }

                vertexArray[i] = new VertexPositionColorTextureTexture(new Vector3(x, y, 0), c, new Vector2(tx, ty), new Vector2(tx2, ty2));
                vertexArray[i + 1] = new VertexPositionColorTextureTexture(new Vector3(x + width, y, 0), c, new Vector2(tx + tw, ty), new Vector2(tx2 + tw2, ty2));
                vertexArray[i + 2] = new VertexPositionColorTextureTexture(new Vector3(x, y + height, 0), c, new Vector2(tx, ty + th), new Vector2(tx2, ty2 + th2));
                vertexArray[i + 3] = new VertexPositionColorTextureTexture(new Vector3(x + width, y + height, 0), c, new Vector2(tx + tw, ty + th), new Vector2(tx2 + tw2, ty2 + th2));

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

                this.vertexBuffer = new DynamicVertexBuffer(this.graphics, VertexPositionColorTextureTexture.VertexDeclaration, (int)(size * bufferGrowthFactor), BufferUsage.WriteOnly);
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

        public void Draw()
        {
            if (vertexBuffer == null || vertexBuffer.IsContentLost || vertexBuffer.IsDisposed || vertexBuffer.VertexCount == 0 || usedBufferSize == 0)
            {
                return;
            }

            if (indexBuffer == null || indexBuffer.IsContentLost || indexBuffer.IsDisposed)
            {
                return;
            }

            this.effect.CurrentTechnique = this.effect.Techniques["MainTechnique"];

            var light = World.WorldLight;
            var darkFactor = 0.5f * (1f - (light.LightFactorT + light.LightFactorS + light.LightFactorN + light.LightFactorE + light.LightFactorW));
            this.effect.Parameters["xLightingDirectionFactors"].SetValue(new Vector4(light.LightFactorW + darkFactor, light.LightFactorT + light.LightFactorS, light.LightFactorE + darkFactor, 1));
            this.effect.Parameters["xLightingFactorBlueness"].SetValue(light.LightFactorBlueness);
            this.effect.Parameters["xAlpha"].SetValue(1f);

            this.graphics.SetVertexBuffer(vertexBuffer);
            this.graphics.Indices = indexBuffer;
            var prevBlendState = this.graphics.BlendState;
            this.graphics.BlendState = BlendState.AlphaBlend;
            this.effect.CurrentTechnique.Passes[0].Apply();
            this.graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, this.usedBufferSize / 2);
            this.graphics.BlendState = prevBlendState;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.effect != null) this.effect.Dispose();
                if (this.vertexBuffer != null) this.vertexBuffer.Dispose();
                if (this.indexBuffer != null) this.indexBuffer.Dispose();
            }
        }
    }
}
