namespace SigmaDraconis.Renderers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;
    using Draconis.Shared;
    using Shared;
    using World;
    using World.Particles;

    public class DustShadowRenderer : IRenderer, IDisposable
    {
        protected VertexBuffer vertexBuffer;
        protected IndexBuffer indexBuffer;
        protected GraphicsDevice graphics;
        protected ContentManager content;
        protected Vector2f scrollPosition = new Vector2i(0, 0);
        protected float zoom;
        protected int viewWidth;
        protected int viewHeight;
        protected int initialBufferCapacity = 64;
        protected float vertexGrowthFactor = 1.5f;
        protected Effect effect;
        protected Texture2D mainTexture;
        protected WorldTime WorldTime;
        protected Vector3 sunVector;
        protected VertexPositionColorTexture[] vertexArray = null;
        protected int particleCount = 0;
        protected long lastUpdateFrame;

        public virtual void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;

            if (this.effect == null)
            {
                this.mainTexture = contentManager.Load<Texture2D>("Textures\\Particles\\Smoke");
                this.LoadBasicEffect();
            }

            this.InitBuffers(this.initialBufferCapacity, true);
        }

        public virtual void Update(WorldTime time, Vector2f scrollPos, float zoom, Vector3 sunVector)
        {
            this.WorldTime = time.Clone();

            this.sunVector = sunVector.Y > 0 ? sunVector : new Vector3(0 - sunVector.X, 0 - sunVector.Y, 0 - sunVector.Z);

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

                if (this.effect is BasicEffect)
                {
                    (this.effect as BasicEffect).Projection = projection;
                }
                else
                {
                    this.effect.Parameters["xViewProjection"].SetValue(projection);
                }
            }

            this.scrollPosition.X = scrollPos.X;
            this.scrollPosition.Y = scrollPos.Y;
            this.zoom = zoom;

            this.effect.Parameters["xSunVector"].SetValue(this.sunVector);
            if (World.WorldTime.FrameNumber > this.lastUpdateFrame + 1 || World.WorldTime.FrameNumber < this.lastUpdateFrame)  // Update at 30fps
            {
                this.UpdateAll();
                this.lastUpdateFrame = World.WorldTime.FrameNumber;
            }
        }

        private void UpdateAll()
        {
            var particles = LanderExhaustSimulator.GetAllParticles().Where(p => p.IsVisible).Concat(RocketExhaustSimulator.GetParticlesForSahdowRenderer()).ToList();
            
            if (particles.Count > 0 || this.particleCount > 0)
            {
                if (particles.Count > this.vertexBuffer.VertexCount / 4)
                {
                    this.InitBuffers((int)(this.vertexGrowthFactor * this.vertexBuffer.VertexCount / 4));
                }

                for (int i = 0; i < this.vertexBuffer.VertexCount / 4; i++)
                {
                    var j = i * 4;
                    if (i < particles.Count)
                    {
                        var particle = particles[i];

                        var x1 = particle.X - (particle.Size * 0.5f);
                        var y1 = particle.Y - (particle.Size * 0.5f) - (particle.ParticleType == SmokeParticleType.Exhaust ? 2.6f : 0f);
                        var x2 = particle.X + (particle.Size * 0.5f);
                        var y2 = particle.Y + (particle.Size * 0.5f) - (particle.ParticleType == SmokeParticleType.Exhaust ? 2.6f : 0f);
                        var z = particle.Z;

                        var model = new List<Vector3>(4)
                        {
                            new Vector3(x1, y1, z),
                            new Vector3(x2, y1, z),
                            new Vector3(x2, y2, z),
                            new Vector3(x1, y2, z)
                        };

                        var alpha = particle.Alpha * particle.AlphaScale * 8f;
                        this.vertexArray[j] = new VertexPositionColorTexture { Position = model[0], TextureCoordinate = new Vector2(0, 0), Color = new Color(1f, 1f, 1f, alpha) };
                        this.vertexArray[j + 1] = new VertexPositionColorTexture { Position = model[1], TextureCoordinate = new Vector2(1, 0), Color = new Color(1f, 1f, 1f, alpha) };
                        this.vertexArray[j + 2] = new VertexPositionColorTexture { Position = model[2], TextureCoordinate = new Vector2(1, 1), Color = new Color(1f, 1f, 1f, alpha) };
                        this.vertexArray[j + 3] = new VertexPositionColorTexture { Position = model[3], TextureCoordinate = new Vector2(0, 1), Color = new Color(1f, 1f, 1f, alpha) };
                    }
                    else
                    {
                        var texCoord = Vector2.Zero;
                        this.vertexArray[j] = new VertexPositionColorTexture { Position = Vector3.Zero, TextureCoordinate = texCoord };
                        this.vertexArray[j + 1] = new VertexPositionColorTexture { Position = Vector3.Zero, TextureCoordinate = texCoord };
                        this.vertexArray[j + 2] = new VertexPositionColorTexture { Position = Vector3.Zero, TextureCoordinate = texCoord };
                        this.vertexArray[j + 3] = new VertexPositionColorTexture { Position = Vector3.Zero, TextureCoordinate = texCoord };
                    }
                }

                this.vertexBuffer.SetData(this.vertexArray);

                WorldRenderer.Instance.InvalidateShadows();
            }

            this.particleCount = particles.Count;
        }

        public void Clear()
        {
            this.vertexArray = null;
            if (this.graphics != null) this.InitBuffers(this.initialBufferCapacity, true);
        }

        protected void InitBuffers(int capacity, bool clear = false)
        {
            if (this.vertexBuffer?.IsDisposed == false)
            {
                this.vertexBuffer.Dispose();
            }

            if (this.indexBuffer?.IsDisposed == false)
            {
                this.indexBuffer.Dispose();
            }

            this.vertexBuffer = new VertexBuffer(this.graphics, VertexPositionColorTexture.VertexDeclaration, capacity * 4, BufferUsage.WriteOnly);
            this.indexBuffer = new IndexBuffer(this.graphics, typeof(int), capacity * 6, BufferUsage.WriteOnly);

            if (this.vertexArray == null)
            {
                this.vertexArray = new VertexPositionColorTexture[capacity * 4];
            }
            else
            {
                var oldCapacity = this.vertexArray.Length / 4;
                Array.Resize(ref this.vertexArray, capacity * 4);

                for (int i = clear ? 0 : oldCapacity * 4; i < this.vertexArray.Length; i++)
                {
                    var texCoord = Vector2.Zero;
                    this.vertexArray[i] = new VertexPositionColorTexture { Position = Vector3.Zero, TextureCoordinate = texCoord };
                }
            }

            int[] index = new int[capacity * 6];

            for (int i = 0, j = 0; j < capacity * 4; i += 6, j += 4)
            {
                index[i] = j;
                index[i + 1] = j + 1;
                index[i + 2] = j + 2;
                index[i + 3] = j + 2;
                index[i + 4] = j;
                index[i + 5] = j + 3;
            }

            this.indexBuffer.SetData(index);
        }

        public virtual void Draw()
        {
            if (this.vertexBuffer == null || this.vertexBuffer.IsDisposed || this.particleCount == 0)
            {
                return;
            }

            if (this.indexBuffer == null || this.indexBuffer.IsDisposed)
            {
                return;
            }

            this.graphics.SetVertexBuffer(vertexBuffer);
            this.graphics.Indices = indexBuffer;
            this.effect.CurrentTechnique.Passes[0].Apply();
            this.graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, this.particleCount * 2);
        }

        protected virtual void LoadBasicEffect()
        {
            this.effect = content.Load<Effect>("Effects\\ShadowEffect").Clone();
            if (this.mainTexture != null) this.effect.Parameters["xTexture"].SetValue(this.mainTexture);
            this.effect.CurrentTechnique = this.effect.Techniques["TexturedShadowTechnique"];
        }

        public void InvalidateBuffers()
        {
        }

        #region IDisposable implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.indexBuffer != null) this.indexBuffer.Dispose();
                if (this.vertexBuffer != null) this.vertexBuffer.Dispose();
            }
        }

        #endregion IDisposable implementation
    }
}
