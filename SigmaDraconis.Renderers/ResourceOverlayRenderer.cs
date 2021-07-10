namespace SigmaDraconis.Renderers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using World;
    using WorldControllers;

    public class ResourceOverlayRenderer : IRenderer, IDisposable
    {
        private readonly static float bufferGrowthFactor = 1.5f;

        private DynamicVertexBuffer vertexBuffer;
        private DynamicIndexBuffer indexBuffer;
        private Effect effect;
        private GraphicsDevice graphics;
        private ContentManager content;
        private readonly Vector2f scrollPosition = new Vector2i(0, 0);
        private float zoom;
        private int viewWidth;
        private int viewHeight;
        private int usedBufferSize = 0;
        private bool isBufferInvalidated = true;
        private readonly Dictionary<ItemType, Color> colours = new Dictionary<ItemType, Color>();

        public bool IsVisible { get; set; } = false;

        public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;

            this.effect = content.Load<Effect>("Effects\\TerrainOverlayEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(content.Load<Texture2D>("Textures\\Icons\\OreDensity"));
            this.effect.Parameters["xGlobalAlpha"].SetValue(1f);

            this.colours.Add(ItemType.None, new Color(Constants.TerrainOverlayNoResourceColourR, Constants.TerrainOverlayNoResourceColourG, Constants.TerrainOverlayNoResourceColourB));
            this.colours.Add(ItemType.Coal, new Color(Constants.TerrainOverlayCoalColourR, Constants.TerrainOverlayCoalColourG, Constants.TerrainOverlayCoalColourB));
            this.colours.Add(ItemType.IronOre, new Color(Constants.TerrainOverlayIronOreColourR, Constants.TerrainOverlayIronOreColourG, Constants.TerrainOverlayIronOreColourB));
            this.colours.Add(ItemType.Stone, new Color(Constants.TerrainOverlayStoneColourR, Constants.TerrainOverlayStoneColourG, Constants.TerrainOverlayStoneColourB));
        }

        public void Update(Vector2f scrollPos, float zoom)
        {
            var invalidateBuffers = ResourceMapController.RendererUpdateFlag || this.isBufferInvalidated || this.graphics.Viewport.Width != this.viewWidth || this.graphics.Viewport.Height != this.viewHeight;
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

        public void InvalidateBuffers()
        {
            this.isBufferInvalidated = true;
        }

        public void UpdateBuffers()
        {
            ResourceMapController.RendererUpdateFlag = false;
            this.isBufferInvalidated = false;
            var tiles = World.SmallTiles.Where(t => t.TerrainType == TerrainType.Dirt && t.IsMineResourceVisible
                && (t.MineResourceType != ItemType.None || t.ThingsAll.All(a => a.ThingType != ThingType.FoundationMetal && a.ThingType != ThingType.FoundationStone && a.ThingType != ThingType.AlgaePool))).ToList();
            var size = tiles.Count * 4;
            if (size == 0)
            {
                this.usedBufferSize = 0;
                return;
            }

            VertexPositionColorTexture[] vertexArray = new VertexPositionColorTexture[size];
            int i = 0;

            foreach (var tile in tiles)
            {
                var cx = tile.CentrePosition.X;
                var cy = tile.CentrePosition.Y;

                var tx = 0f;

                var resource = tile.GetResources();
                var density = resource?.Density ?? MineResourceDensity.None;
                if (density != MineResourceDensity.None)
                {
                    if (density == MineResourceDensity.VeryHigh) tx = 5 / 6f;
                    else if (density == MineResourceDensity.High) tx = 4 / 6f;
                    else if (density == MineResourceDensity.Medium) tx = 3 / 6f;
                    else if (density == MineResourceDensity.Low) tx = 2 / 6f;
                    else tx = 1 / 6f;
                }

                var c = this.colours[resource?.Type ?? ItemType.None];
                vertexArray[i] = new VertexPositionColorTexture { Position = new Vector3(cx - 10.667f, cy, 0), TextureCoordinate = new Vector2(tx, 1f), Color = c };
                vertexArray[i + 1] = new VertexPositionColorTexture { Position = new Vector3(cx, cy - 5.333f, 0), TextureCoordinate = new Vector2(tx, 0f), Color = c };
                vertexArray[i + 2] = new VertexPositionColorTexture { Position = new Vector3(cx + 10.667f, cy, 0), TextureCoordinate = new Vector2(tx + 1 / 6f, 0f), Color = c };
                vertexArray[i + 3] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 5.333f, 0), TextureCoordinate = new Vector2(tx + 1 / 6f, 1f), Color = c };

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
                indexArray[k + 4] = j + 3;
                indexArray[k + 5] = j;
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

            this.effect.CurrentTechnique = this.effect.Techniques["MainTechnique"];
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
