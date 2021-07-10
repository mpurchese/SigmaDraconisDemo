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

    public class RendererBase : IRenderer, IDisposable
    {
        private const int initialIndexBufferSize = 12000;

        private readonly Dictionary<int, DynamicVertexBuffer> vertexBuffers = new Dictionary<int, DynamicVertexBuffer>();
        private static DynamicIndexBuffer indexBuffer = null;
        private readonly Dictionary<int, int> vertexBufferSizes = new Dictionary<int, int>();
        protected static GraphicsDevice Graphics => WorldRenderer.Instance.Graphics;
        protected static ContentManager Content => WorldRenderer.Instance.GeneralContent;
        private readonly Vector2f scrollPosition = new Vector2i(0, 0);
        protected float zoom;
        private int viewWidth;
        private int viewHeight;

        protected Effect effect;
        protected Texture2D mainTexture;
        protected Texture2D eveningTexture;
        protected Texture2D nightTexture;
        protected Texture2D texture1;
        protected Texture2D texture2;
        protected Vector2 textureSize;
        protected bool isBufferInvalidated = true;
        protected HashSet<int> invalidatedRows = new HashSet<int>();

        protected BlendState blendState = BlendState.AlphaBlend;

        public virtual void LoadContent()
        {
            EventManager.Subscribe(EventType.GameExit, delegate (object obj) { this.OnGameExit(); });
        }

        public virtual void ReloadContent()
        {
        }

        private void OnGameExit()
        {
            this.vertexBuffers.Clear();
            this.vertexBufferSizes.Clear();
            this.invalidatedRows.Clear();

            if (indexBuffer != null)
            {
                indexBuffer.Dispose();
                indexBuffer = null;
            }
        }

        public virtual void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            var isProjectionInvalidated = this.scrollPosition == null
               || scrollPos.X != this.scrollPosition.X
               || scrollPos.Y != this.scrollPosition.Y
               || Graphics.Viewport.Width != this.viewWidth
               || Graphics.Viewport.Height != this.viewHeight
               || this.zoom != zoom;

            if (isProjectionInvalidated)
            {
                this.viewWidth = Graphics.Viewport.Width;
                this.viewHeight = Graphics.Viewport.Height;
                var sx = scrollPos.X / 2f;
                var sy = scrollPos.Y / 2f;
                Matrix projection = Matrix.CreateOrthographicOffCenter(sx, sx + Graphics.Viewport.Width, sy + Graphics.Viewport.Height, sy, 0, 1) * Matrix.CreateScale(zoom);

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

            if (this.isBufferInvalidated
                || Graphics.Viewport.Width != this.viewWidth
                || Graphics.Viewport.Height != this.viewHeight)
            {
                this.GenerateBuffers();
                this.isBufferInvalidated = false;
            }

            this.GenerateBuffersForInvalidatedRows();
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
                if (this.mainTexture != null) this.mainTexture.Dispose();
                if (this.eveningTexture != null) this.eveningTexture.Dispose();
                if (this.nightTexture != null) this.nightTexture.Dispose();
            }
        }

        protected virtual void GenerateBuffersForInvalidatedRows()
        {
            foreach (var row in this.invalidatedRows)
            {
                try
                {
                    this.GenerateBuffersForRow(row);
                }
                catch (Exception ex)
                {
                    ExceptionManager.CurrentExceptions.Add(ex);
                }
            }

            this.invalidatedRows.Clear();
        }

        public void InvalidateRow(int row)
        {
            if (!this.invalidatedRows.Contains(row))
            {
                this.invalidatedRows.Add(row);
            }
        }

        protected void GenerateBuffers()
        {
            var rowCount = World.SmallTileRowCount;
            for (int r = 0; r < rowCount; ++r)
            {
                this.GenerateBuffersForRow(r);
            }

            this.invalidatedRows.Clear();
        }

        protected void ClearBuffers()
        {
            var rowCount = World.SmallTileRowCount;
            for (int r = 0; r < rowCount; ++r)
            {
                this.SetBufferData(r, new VertexPositionTexture[0], 0);
            }
        }

        protected virtual void GenerateBuffersForRow(int row)
        {
            throw new NotImplementedException("GenerateBuffers method not implemented");
        }

        protected void SetBufferData(int row, object vertexBuffer, int bufferSize, float growthFactor = 1.5f)
        {
            if (bufferSize == 0)
            {
                // Mark buffer as empty so we don't draw it - this is quicker than clearing or disposing
                if (this.vertexBuffers.ContainsKey(row))
                {
                    this.vertexBufferSizes[row] = 0;
                }

                return;
            }

            DynamicVertexBuffer oldVertexBuffer = this.vertexBuffers.ContainsKey(row) ? this.vertexBuffers[row] : null;

            if (oldVertexBuffer != null && (oldVertexBuffer.IsContentLost || oldVertexBuffer.IsDisposed || oldVertexBuffer.VertexCount < bufferSize || bufferSize == 0))
            {
                // Vertex buffer is invalid or too small
                oldVertexBuffer.Dispose();
                oldVertexBuffer = null;
            }

            this.UpdateIndexBuffer(bufferSize);

            if (bufferSize == 0)
            {
                return;
            }

            if (vertexBuffer is VertexPositionTexture[])
            {
                if (oldVertexBuffer == null)
                {
                    oldVertexBuffer = new DynamicVertexBuffer(Graphics, VertexPositionTexture.VertexDeclaration, (int)(bufferSize * growthFactor), BufferUsage.WriteOnly);
                }

                oldVertexBuffer.SetData(vertexBuffer as VertexPositionTexture[]);
            }
            else if (vertexBuffer is VertexPositionColorTexture[])
            {
                if (oldVertexBuffer == null)
                {
                    oldVertexBuffer = new DynamicVertexBuffer(Graphics, VertexPositionColorTexture.VertexDeclaration, (int)(bufferSize * growthFactor), BufferUsage.WriteOnly);
                }

                oldVertexBuffer.SetData(vertexBuffer as VertexPositionColorTexture[]);
            }
            else if (vertexBuffer is ColonistVertex[])
            {
                if (oldVertexBuffer == null)
                {
                    oldVertexBuffer = new DynamicVertexBuffer(Graphics, ColonistVertex.VertexDeclaration, (int)(bufferSize * growthFactor), BufferUsage.WriteOnly);
                }

                oldVertexBuffer.SetData(vertexBuffer as ColonistVertex[]);
            }

            if (this.vertexBuffers.ContainsKey(row))
            {
                this.vertexBuffers[row] = oldVertexBuffer;
                this.vertexBufferSizes[row] = bufferSize;
            }
            else
            {
                this.vertexBuffers.Add(row, oldVertexBuffer);
                this.vertexBufferSizes.Add(row, bufferSize);
            }
        }

        private void UpdateIndexBuffer(int vertexBufferSize)
        {
            var indexBufferSize = (vertexBufferSize * 3) / 2;
            if (vertexBufferSize == 0) return;

            if (indexBuffer != null && (indexBuffer.IsContentLost || indexBuffer.IsDisposed || indexBuffer.IndexCount < indexBufferSize))
            {
                // Index Buffer is invalid or too small
                indexBuffer.Dispose();
                indexBuffer = null;
                vertexBufferSize = 4 * (vertexBufferSize * 3 / 8);   // So we don't need to increase as often
                indexBufferSize = (vertexBufferSize * 3) / 2;
            }
            else if (indexBuffer != null)
            {
                // Index buffer is already set up
                return;
            }

            // This will save repeated expansions when game is loaded
            if (indexBufferSize < initialIndexBufferSize) indexBufferSize = initialIndexBufferSize;

            var newBuffer = new int[indexBufferSize];
            for (int i = 0, j = 0; i <= indexBufferSize - 6; i += 6, j += 4)
            {
                newBuffer[i] = j;
                newBuffer[i + 1] = j + 1;
                newBuffer[i + 2] = j + 2;
                newBuffer[i + 3] = j + 2;
                newBuffer[i + 4] = j + 1;
                newBuffer[i + 5] = j + 3;
            }

            indexBuffer = new DynamicIndexBuffer(Graphics, typeof(int), indexBufferSize, BufferUsage.WriteOnly);
            indexBuffer.SetData(newBuffer);
        }

        public void InvalidateBuffers()
        {
            this.isBufferInvalidated = true;
        }

        public virtual void Draw(int row)
        {
            if (!this.vertexBufferSizes.ContainsKey(row) || this.vertexBufferSizes[row] == 0)
            {
                return;
            }

            DynamicVertexBuffer vertexBuffer = this.vertexBuffers[row];
            if (vertexBuffer == null || vertexBuffer.IsContentLost || vertexBuffer.IsDisposed || vertexBuffer.VertexCount == 0)
            {
                return;
            }

            if (indexBuffer == null || indexBuffer.IsContentLost || indexBuffer.IsDisposed)
            {
                return;
            }

            Graphics.SetVertexBuffer(vertexBuffer);
            Graphics.Indices = indexBuffer;
            var prevBlendState = Graphics.BlendState;
            Graphics.BlendState = this.blendState;
            this.effect.CurrentTechnique.Passes[0].Apply();
            Graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, this.vertexBufferSizes[row] / 2);
            Graphics.BlendState = prevBlendState;

            PerfMonitor.DrawCounter++;
        }

        protected void LoadBasicEffect(string texturePath)
        {
            this.LoadBasicEffect(Content.Load<Texture2D>(texturePath));
        }

        protected void LoadBasicEffect(Texture2D texture)
        {
            this.mainTexture = texture;
            this.textureSize = new Vector2(mainTexture.Width, mainTexture.Height);

            this.effect = new BasicEffect(Graphics)
            {
                Texture = mainTexture,
                TextureEnabled = true,
                World = Matrix.Identity,
                View = Matrix.Identity
            };
        }

        protected void LoadBuildingEffectForBlueprints(Texture2D texture)
        {
            this.mainTexture = texture;
            this.textureSize = new Vector2(texture.Width, texture.Height);
            this.effect = Content.Load<Effect>("Effects\\BuildingEffect").Clone();
            this.effect.Parameters["xNightTexture"].SetValue(texture);
        }

        protected void LoadGeneralEffectNew(Texture2D texture1, Texture2D texture2, string path = "Effects\\GeneralEffect")
        {
            this.texture1 = texture1;
            this.texture2 = texture2;
            this.textureSize = new Vector2(texture1.Width, texture1.Height);

            this.effect = Content.Load<Effect>(path).Clone();

            this.effect.Parameters["xTexture1"].SetValue(texture1);
            this.effect.Parameters["xTexture2"].SetValue(texture2);
            this.effect.Parameters["xAlpha"].SetValue(1f);
        }

        protected static Color GetOutsideLightingFactors(float alpha = 1f)
        {
            var r = World.WorldLight.MorningLightFactor;
            var g = World.WorldLight.EveningLightFactor;
            var t = r + g;
            if (t > 1f)
            {
                r /= t;
                g /= t;
            }

            var b = 1 - (g + r);
            return new Color(r, g, b, alpha);
        }
    }
}
