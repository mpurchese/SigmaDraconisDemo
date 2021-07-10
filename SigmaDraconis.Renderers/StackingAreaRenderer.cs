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
    using SigmaDraconis.WorldInterfaces;
    using System.Collections.Generic;

    public class StackingAreaRenderer : IRenderer, IDisposable
    {
        private static readonly float bufferGrowthFactor = 1.5f;
        private static readonly Dictionary<ItemType, Color> colours = new Dictionary<ItemType, Color> {
            { ItemType.Metal, new Color(0.5f, 0.5f, 0.8f, 0.2f) },
            { ItemType.Stone, new Color(0.8f, 0.5f, 0.5f, 0.2f) },
            { ItemType.IronOre, new Color(0.5f, 0.0f, 0.0f, 0.25f) },
            { ItemType.Coal, new Color(0f, 0f, 0f, 0.4f) }, 
            { ItemType.Biomass, new Color(0, 0.5f, 0f, 0.25f) }, 
            { ItemType.Compost, new Color(0.2f, 0.15f, 0.15f, 0.8f) }
        };

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

        public Effect LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;

            this.effect = content.Load<Effect>("Effects\\TerrainOverlayEffect").Clone();
            this.effect.Parameters["xGlobalAlpha"].SetValue(1f);
            this.ReloadContent();

            return this.effect;
        }

        public void ReloadContent()
        {
            var texture = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\StackingArea" : "Buildings\\StackingArea");
            this.effect.Parameters["xTexture"].SetValue(texture);
            this.textureSize = new Vector2(texture.Width, texture.Height);
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
            this.effect.Parameters["xGlobalAlpha"].SetValue(1f + World.WorldLight.LightFactorT);  // Alpha boost for midday

            if (invalidateBuffers)
            {
                this.UpdateBuffers();
            }
        }

        public void UpdateBuffers()
        {
            this.isBufferInvalidated = false;

            var stackingAreas = World.GetThings<IThing>(ThingType.StackingArea).Concat(World.VirtualBlueprint.Where(b => b.ThingType == ThingType.StackingArea)).ToList();
            var size = stackingAreas.Count * 4;
            if (size == 0)
            {
                this.usedBufferSize = 0;
                return;
            }

            VertexPositionColorTexture[] vertexArray = new VertexPositionColorTexture[size];
            int i = 0;

            foreach (var stackingArea in stackingAreas)
            {
                var c = stackingArea is IStackingArea sa ? colours[sa.ItemType] : colours[(ItemType)(stackingArea as IBlueprint).AnimationFrame];

                var x = stackingArea.MainTile.CentrePosition.X - 11f;
                var y = stackingArea.MainTile.CentrePosition.Y - 5.625f;

                vertexArray[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(0f, 0f));
                vertexArray[i + 1] = new VertexPositionColorTexture(new Vector3(x + 22f, y, 0), c, new Vector2(1f, 0f));
                vertexArray[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + 11.25f, 0), c, new Vector2(0f, 1f));
                vertexArray[i + 3] = new VertexPositionColorTexture(new Vector3(x + 22f, y + 11.25f, 0), c, new Vector2(1f, 1f));

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

            this.effect.CurrentTechnique = this.effect.Techniques["MainTechnique"];

            //var light = World.WorldLight;
            //var darkFactor = 0.5f * (1f - (light.LightFactorT + light.LightFactorS + light.LightFactorN + light.LightFactorE + light.LightFactorW));
            //this.effect.Parameters["xLightingDirectionFactors"].SetValue(new Vector4(light.LightFactorW + darkFactor, light.LightFactorT + light.LightFactorS, light.LightFactorE + darkFactor, 1));
            //this.effect.Parameters["xLightingFactorBlueness"].SetValue(light.LightFactorBlueness);
            //this.effect.Parameters["xAlpha"].SetValue(1f);

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
