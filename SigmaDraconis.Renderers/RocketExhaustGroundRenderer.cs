namespace SigmaDraconis.Renderers
{
    using System;

    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using World;
    using World.Particles;

    public class RocketExhaustGroundRenderer : IRenderer, IDisposable
    {
        private static float bufferGrowthFactor = 1.5f;

        private DynamicVertexBuffer vertexBuffer;
        private DynamicIndexBuffer indexBuffer;
        private Effect effect;
        private GraphicsDevice graphics;
        private Vector2f scrollPosition = new Vector2i(0, 0);
        private float zoom;
        private int viewWidth;
        private int viewHeight;
        private int usedBufferSize = 0;

        public int Layer { get; set; } = 1;

        public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            var mainTexture = contentManager.Load<Texture2D>("Textures\\Particles\\Smoke");
            this.effect = contentManager.Load<Effect>("Effects\\SmokeEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(mainTexture);
        }

        public void Update(Vector2f scrollPos, float zoom)
        {
            var invalidateBuffers = this.graphics.Viewport.Width != this.viewWidth || this.graphics.Viewport.Height != this.viewHeight;
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

            this.UpdateBuffers();
        }

        public void InvalidateBuffers()
        {
            // This renderer always updates
        }

        public void UpdateBuffers()
        {
            var particles = RocketExhaustSimulator.GetParticlesForGroundRenderer();
            if (particles == null || particles.Length == 0)
            {
                this.usedBufferSize = 0;
                return;
            }

            var bufferSize = particles.Length * 4;

            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[bufferSize];
            int i = 0;

            foreach (var particle in particles)
            {
                var size = particle.Size;
                var x = particle.X - (size * 0.5f);
                var y = particle.Y - particle.Z - (size * 0.5f);

                float width = size;
                float height = size;

                var alpha = particle.Alpha * particle.AlphaScale * 8f;
                var c = new Color(alpha * 0.6f, alpha * 0.6f, alpha * 0.6f, alpha);

                vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(0, 0), Color = c };
                vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(1, 0), Color = c };
                vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(0, 1), Color = c };
                vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(1, 1), Color = c };

                i += 4;
            }

            // Set up index buffer
            var indexBufferSize = (bufferSize * 3) / 2;
            var indexArray = new int[indexBufferSize];
            for (int k = 0, j = 0; j < bufferSize; k += 6, j += 4)
            {
                indexArray[k] = j;
                indexArray[k + 1] = j + 1;
                indexArray[k + 2] = j + 2;
                indexArray[k + 3] = j + 2;
                indexArray[k + 4] = j + 1;
                indexArray[k + 5] = j + 3;
            }

            if (this.vertexBuffer == null || this.vertexBuffer.IsContentLost || this.vertexBuffer.IsDisposed || this.vertexBuffer.VertexCount < bufferSize)
            {
                if (this.vertexBuffer?.IsDisposed == false)
                {
                    // Destroy the old buffer, it's invalidated or too small
                    this.vertexBuffer.Dispose();
                    this.vertexBuffer = null;
                }

                this.vertexBuffer = new DynamicVertexBuffer(this.graphics, VertexPositionColorTexture.VertexDeclaration, (int)(bufferSize * bufferGrowthFactor), BufferUsage.WriteOnly);
            }

            this.vertexBuffer.SetData(vertex);
            this.usedBufferSize = bufferSize;

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

            var light = 1f - World.WorldLight.NightLightFactor;
            var r = light + (0.35f * (1f - light));
            var g = light + (0.5f * (1f - light));
            var b = light + (0.7f * (1f - light));
            this.effect.Parameters["xColour"].SetValue(new Vector4(r, g, b, 1));
            this.effect.CurrentTechnique = this.effect.Techniques["SimpleTechnique"];

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
