namespace SigmaDraconis.Renderers
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using UI;
    using UI.Managers;
    using World;

    public class TerrainOverlayRenderer : IRenderer, IDisposable
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

        public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;

            var texture = new Texture2D(graphicsDevice, 1, 1);
            Color[] color = new Color[1] { new Color(Color.White, 140) };
            texture.SetData(color);

            this.effect = content.Load<Effect>("Effects\\TerrainOverlayEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(texture);
            this.effect.Parameters["xGlobalAlpha"].SetValue(1f);

            EventManager.Subscribe(EventType.HighlightedTiles, delegate (object obj) { this.OnHighlightUpdated(); });
            EventManager.Subscribe(EventType.BuildableArea, delegate (object obj) { this.OnHighlightUpdated(); });
            PlayerWorldInteractionManager.CurrentActivityChanged += this.OnPlayerActivityChanged;
        }

        private void OnPlayerActivityChanged(object sender, EventArgs e)
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

        public void InvalidateBuffers()
        {
            this.isBufferInvalidated = true;
        }

        public void UpdateBuffers()
        {
            this.isBufferInvalidated = false;
            var highlights = TileHighlightManager.TileHighlightsSteady;
            var buildableTiles = PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Build ? World.BuildableTiles : new HashSet<int>();
            var size = (buildableTiles.Count + highlights.Count) * 4;
            if (size == 0)
            {
                this.usedBufferSize = 0;
                return;
            }

            VertexPositionColorTexture[] vertexArray = new VertexPositionColorTexture[size];
            int i = 0;

            var buildableAreaColour = new Color(Color.Gold, 0.2f);
            foreach (var tileIndex in buildableTiles)
            {
                var tile = World.GetSmallTile(tileIndex);

                var cx = tile.CentrePosition.X;
                var cy = tile.CentrePosition.Y;

                vertexArray[i] = new VertexPositionColorTexture { Position = new Vector3(cx - 10.667f, cy, 0), TextureCoordinate = new Vector2(0, 1), Color = buildableAreaColour };
                vertexArray[i + 1] = new VertexPositionColorTexture { Position = new Vector3(cx, cy - 5.333f, 0), TextureCoordinate = new Vector2(0, 0), Color = buildableAreaColour };
                vertexArray[i + 2] = new VertexPositionColorTexture { Position = new Vector3(cx + 10.667f, cy, 0), TextureCoordinate = new Vector2(1, 0), Color = buildableAreaColour };
                vertexArray[i + 3] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 5.333f, 0), TextureCoordinate = new Vector2(1, 1), Color = buildableAreaColour };

                i += 4;
            }

            foreach (var highlight in highlights)
            {
                var tile = World.GetSmallTile(highlight.Tile);
                var colour = new Color(Color.Blue, highlight.Alpha);

                var cx = tile.CentrePosition.X;
                var cy = tile.CentrePosition.Y;

                vertexArray[i] = new VertexPositionColorTexture { Position = new Vector3(cx - 10.667f, cy, 0), TextureCoordinate = new Vector2(0, 1), Color = colour };
                vertexArray[i + 1] = new VertexPositionColorTexture { Position = new Vector3(cx, cy - 5.333f, 0), TextureCoordinate = new Vector2(0, 0), Color = colour };
                vertexArray[i + 2] = new VertexPositionColorTexture { Position = new Vector3(cx + 10.667f, cy, 0), TextureCoordinate = new Vector2(1, 0), Color = colour };
                vertexArray[i + 3] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 5.333f, 0), TextureCoordinate = new Vector2(1, 1), Color = colour };

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

        private void OnHighlightUpdated()
        {
            this.UpdateBuffers();
        }
    }
}
