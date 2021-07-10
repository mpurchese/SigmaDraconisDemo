namespace SigmaDraconis.Renderers
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using World;
    using World.Rooms;

    public class TemperatureOverlayRenderer : IRenderer, IDisposable
    {
        private static float bufferGrowthFactor = 1.5f;

        private DynamicVertexBuffer vertexBuffer;
        private DynamicIndexBuffer indexBuffer;
        private Effect effect;
        private GraphicsDevice graphics;
        private ContentManager content;
        private Vector2f scrollPosition = new Vector2i(0, 0);
        private float zoom;
        private int viewWidth;
        private int viewHeight;
        private int usedBufferSize = 0;
        private bool isBufferInvalidated = true;
        private Dictionary<int, int> roomTemperatures = new Dictionary<int, int>();
        private Dictionary<int, int> roomSizes = new Dictionary<int, int>();

        public bool IsVisible { get; set; } = false;

        public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;

            var texture = content.Load<Texture2D>("Textures\\Misc\\TemperatureOverlayColours");

            this.effect = content.Load<Effect>("Effects\\BuildingEffect").Clone();
            this.effect.Parameters["xNightTexture"].SetValue(texture);
            this.effect.Parameters["xLightingFactors"].SetValue(new Vector4(1, 1, 1, 1));
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

            if (!invalidateBuffers)
            {
                foreach (var room in RoomManager.Rooms)
                {
                    var temperature = (int)(Math.Round(room.Temperature));
                    if (!this.roomTemperatures.ContainsKey(room.Id))
                    {
                        this.roomTemperatures.Add(room.Id, temperature);
                        this.roomSizes.Add(room.Id, room.Tiles.Count);
                        invalidateBuffers = true;
                    }
                    else if (this.roomTemperatures[room.Id] != temperature || this.roomSizes[room.Id] != room.Tiles.Count)
                    {
                        this.roomTemperatures[room.Id] = temperature;
                        this.roomSizes[room.Id] = room.Tiles.Count;
                        invalidateBuffers = true;
                    }
                }
            }

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
            var size = World.SmallTiles.Count * 4;
            if (size == 0)
            {
                this.usedBufferSize = 0;
                return;
            }

            VertexPositionColorTexture[] vertexArray = new VertexPositionColorTexture[size];
            int i = 0;

            var c = new Color(0, 0, 0);
            foreach (var tile in World.SmallTiles)
            {
                var cx = tile.CentrePosition.X;
                var cy = tile.CentrePosition.Y;

                var temperature = RoomManager.GetTileTemperature(tile.Index).Clamp(-20, 40);

                if (temperature <= 20) c = new Color(0f, (temperature + 20) * 0.025f, 1f - ((temperature + 20) * 0.025f), 0.8f);
                else c = new Color(((temperature - 20) * 0.05f), 1f - ((temperature - 20) * 0.05f), 0f, 0.8f);

                vertexArray[i] = new VertexPositionColorTexture { Position = new Vector3(cx - 10.667f, cy, 0), TextureCoordinate = new Vector2(0f, 1f), Color = c };
                vertexArray[i + 1] = new VertexPositionColorTexture { Position = new Vector3(cx, cy - 5.333f, 0), TextureCoordinate = new Vector2(0f, 0f), Color = c };
                vertexArray[i + 2] = new VertexPositionColorTexture { Position = new Vector3(cx + 10.667f, cy, 0), TextureCoordinate = new Vector2(1f, 0f), Color = c };
                vertexArray[i + 3] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 5.333f, 0), TextureCoordinate = new Vector2(1f, 1f), Color = c };

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

            this.effect.CurrentTechnique = this.effect.Techniques["TemperatureTechnique"];
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
