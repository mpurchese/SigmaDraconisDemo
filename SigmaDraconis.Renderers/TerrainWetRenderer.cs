namespace SigmaDraconis.Renderers
{
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using World;

    public class TerrainWetRenderer : IRenderer, IDisposable
    {
        private static readonly float bufferGrowthFactor = 1.5f;

        private DynamicVertexBuffer vertexBuffer;
        private DynamicIndexBuffer indexBuffer;
        private Effect effect;
        private GraphicsDevice graphics;
        private ContentManager content;
        protected Vector2 textureSize;
        private readonly Vector2f scrollPosition = new Vector2i(0, 0);
        private float zoom;
        private int viewWidth;
        private int viewHeight;
        private int usedBufferSize = 0;
        private bool isBufferInvalidated = true;

        public int Layer { get; set; }

        public Effect LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;

            var texture1 = contentManager.Load<Texture2D>("Textures\\Tiles\\TerrainWet_1");
            var texture2 = contentManager.Load<Texture2D>("Textures\\Tiles\\TerrainWet_2");

            this.effect = content.Load<Effect>("Effects\\GeneralEffect").Clone();
            this.effect.Parameters["xTexture1"].SetValue(texture1);
            this.effect.Parameters["xTexture2"].SetValue(texture2);

            this.textureSize = new Vector2(texture1.Width, texture1.Height);

            return this.effect;
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

            var tiles = this.Layer == 2
                ? World.SmallTiles.Where(t => t.BiomeType == BiomeType.Wet && t.TerrainType == TerrainType.Dirt).ToList()
                : World.SmallTiles.Where(t => t.BiomeType != BiomeType.Desert && t.TerrainType == TerrainType.Dirt).ToList();
            var size = tiles.Count * 4;
            if (size == 0)
            {
                this.usedBufferSize = 0;
                return;
            }

            VertexPositionColorTexture[] vertexArray = new VertexPositionColorTexture[size];
            int i = 0;
            var c = new Color(0f, 0f, 1f, 1f);

            foreach (var tile in tiles)
            {
                var nw = this.Layer == 2 ? tile.TileToNW?.BiomeType == BiomeType.Wet : tile.TileToNW?.BiomeType != BiomeType.Desert;
                var ne = this.Layer == 2 ? tile.TileToNE?.BiomeType == BiomeType.Wet : tile.TileToNE?.BiomeType != BiomeType.Desert;
                var sw = this.Layer == 2 ? tile.TileToSW?.BiomeType == BiomeType.Wet : tile.TileToSW?.BiomeType != BiomeType.Desert;
                var se = this.Layer == 2 ? tile.TileToSE?.BiomeType == BiomeType.Wet : tile.TileToSE?.BiomeType != BiomeType.Desert;
                var n = this.Layer == 2 ? tile.TileToN?.BiomeType == BiomeType.Wet : tile.TileToN?.BiomeType != BiomeType.Desert;
                var e = this.Layer == 2 ? tile.TileToE?.BiomeType == BiomeType.Wet : tile.TileToE?.BiomeType != BiomeType.Desert;
                var s = this.Layer == 2 ? tile.TileToS?.BiomeType == BiomeType.Wet : tile.TileToS?.BiomeType != BiomeType.Desert;
                var w = this.Layer == 2 ? tile.TileToW?.BiomeType == BiomeType.Wet : tile.TileToW?.BiomeType != BiomeType.Desert;

                var tx1 = 2;
                var tx2 = 3;
                var ty1 = 2;
                var ty2 = 3;

                if (!nw && !ne && !sw && !se)
                {
                    tx1 = 0;
                    tx2 = 1;
                    ty1 = 4;
                    ty2 = 5;
                }
                else if (!nw && !ne && !sw)
                {
                    tx1 = 1;
                    tx2 = 2;
                    ty1 = 5;
                    ty2 = 4;
                }
                else if (!nw && !ne && !se)
                {
                    tx1 = 3;
                    tx2 = 2;
                    ty1 = 4;
                    ty2 = 5;
                }
                else if (!nw && !se && !sw)
                {
                    ty1 = 4;
                    ty2 = 5;
                }
                else if (!ne && !sw && !se)
                {
                    tx1 = 1;
                    tx2 = 2;
                    ty1 = 4;
                    ty2 = 5;
                }
                else if (!nw && !ne)
                {
                    tx1 = 3;
                    tx2 = 4;
                    ty1 = 0;
                    ty2 = 1;
                }
                else if (!nw && !sw)
                {
                    tx1 = 1;
                    tx2 = 2;
                    ty1 = 0;
                    ty2 = 1;
                }
                else if (!ne && !sw)
                {
                    tx1 = 4;
                    tx2 = 5;
                    ty1 = 3;
                    ty2 = 4;
                }
                else if (!nw && !se)
                {
                    tx1 = 3;
                    tx2 = 4;
                    ty1 = 4;
                    ty2 = 5;
                }
                else if (!ne && !se)
                {
                    tx1 = 3;
                    tx2 = 4;
                    ty1 = 3;
                    ty2 = 4;
                }
                else if (!sw && !se)
                {
                    tx1 = 0;
                    tx2 = 1;
                    ty1 = 3;
                    ty2 = 4;
                }
                else if (!nw)
                {
                    ty1 = 0;
                    ty2 = 1;
                }
                else if (!ne)
                {
                    tx1 = 3;
                    tx2 = 4;
                }
                else if (!se)
                {
                    ty1 = 3;
                    ty2 = 4;
                }
                else if (!sw)
                {
                    tx1 = 0;
                    tx2 = 1;
                }
                else if (!n)
                {
                    tx1 = 2;
                    tx2 = 1;
                    ty1 = 1;
                    ty2 = 2;
                }
                else if (!e)
                {
                    tx1 = 2;
                    tx2 = 1;
                    ty1 = 2;
                    ty2 = 1;
                }
                else if (!s)
                {
                    tx1 = 1;
                    tx2 = 2;
                    ty1 = 2;
                    ty2 = 1;
                }
                else if (!w)
                {
                    tx1 = 1;
                    tx2 = 2;
                    ty1 = 1;
                    ty2 = 2;
                }
                // More scenarios could be considered here, but with this subtle effect it shouldn't be too noticable if we ignore
                

                var x0 = tile.CentrePosition.X;
                var x1 = x0 - 10.667f;
                var x2 = x0 + 10.667f;
                var y0 = tile.CentrePosition.Y;
                var y1 = y0 - 5.333f;
                var y2 = y0 + 5.333f;

                vertexArray[i] = new VertexPositionColorTexture(new Vector3(x1, y0, 0), c, new Vector2(tx1 / 5f, ty1 / 5f));
                vertexArray[i + 1] = new VertexPositionColorTexture(new Vector3(x0, y1, 0), c, new Vector2(tx2 / 5f, ty1 / 5f));
                vertexArray[i + 2] = new VertexPositionColorTexture(new Vector3(x0, y2, 0), c, new Vector2(tx1 / 5f, ty2 / 5f));
                vertexArray[i + 3] = new VertexPositionColorTexture(new Vector3(x2, y0, 0), c, new Vector2(tx2 / 5f, ty2 / 5f));

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

            this.effect.CurrentTechnique = this.effect.Techniques["LinearFilterTechnique"];

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
