namespace SigmaDraconis.Renderers
{
    using System;

    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Config;
    using World;

    public abstract class TerrainRendererBase : IRenderer, IDisposable
    {
        protected static float bufferGrowthFactor = 1.5f;
        protected DynamicVertexBuffer vertexBuffer;
        protected DynamicIndexBuffer indexBuffer;
        protected Effect effect;
        protected GraphicsDevice graphics;
        protected ContentManager content;
        protected Vector2f scrollPosition = new Vector2i(0, 0);
        protected float zoom;
        protected int viewWidth;
        protected int viewHeight;
        protected Vector2i textureSize;
        protected int usedBufferSize = 0;
        protected bool isBufferInvalidated = true;
    
        public virtual void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;
        }

        public virtual void InvalidateBuffers()
        {
            this.isBufferInvalidated = true;
        }

        public virtual void Update(Vector2f scrollPos, float zoom)
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

        public virtual void UpdateBuffers()
        { 
            var size = World.BigTilesWithLand.Count * 4;
            if (size == 0) return;

            this.isBufferInvalidated = false;
            var vertexArray = new VertexPositionColorTexture[size];
            int i = 0;

            var c = new Color(0f, 0f, 1f, 1f);
            foreach (var tile in World.BigTilesWithLand)
            {
                var textureName = tile.GetTextureName();
                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.TerrainDirtAbovewater, textureName);

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

        public virtual void Draw()
        {
            if (vertexBuffer == null || vertexBuffer.IsContentLost || vertexBuffer.IsDisposed || vertexBuffer.VertexCount == 0 || usedBufferSize == 0)
            {
                return;
            }

            if (indexBuffer == null || indexBuffer.IsContentLost || indexBuffer.IsDisposed)
            {
                return;
            }

            this.effect.CurrentTechnique = this.effect.Techniques["TerrainTechnique"];

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

        public virtual void Dispose()
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
