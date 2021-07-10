﻿namespace SigmaDraconis.Renderers
{
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using World;
    using World.Blueprints;
    using WorldInterfaces;

    public class LaunchPadRenderer : IRenderer, IDisposable
    {
        private static readonly float bufferGrowthFactor = 1.5f;

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

        public int Layer { get; set; } = 1;

        public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;

            this.effect = content.Load<Effect>("Effects\\GeneralEffect").Clone();
            this.ReloadContent();

            EventManager.Subscribe(EventType.Building, delegate (object obj) { this.OnBuildingPartUpdated(obj); });
            EventManager.Subscribe(EventType.VirtualBlueprint, delegate (object obj) { this.OnBuildingPartUpdated(obj); });
        }

        public void ReloadContent()
        {
            var texture1 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\LaunchPad_1" : "Buildings\\LaunchPad_1");
            var texture2 = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\LaunchPad_2" : "Buildings\\LaunchPad_2");
            this.effect.Parameters["xTexture1"].SetValue(texture1);
            this.effect.Parameters["xTexture2"].SetValue(texture2);
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

            var buildings = this.Layer == 2
                ? World.GetThings(ThingType.LaunchPad).Where(b => b.RenderAlpha > 0.999f).OrderBy(c => c.MainTile.Row).ToList()
                : World.GetThings(ThingType.LaunchPad).Union(World.VirtualBlueprint.Where(b => b.ThingType == ThingType.LaunchPad)).OrderBy(c => c.MainTile.Row).ToList();

            var size = buildings.Count * 4;
            if (size == 0)
            {
                this.usedBufferSize = 0;
                return;
            }

            VertexPositionColorTexture[] vertexArray = new VertexPositionColorTexture[size];
            int i = 0;

            foreach (var building in buildings)
            {
                var layer = this.Layer;
                if (layer == 1 && (building is IBlueprint || (building as IThing).RenderAlpha <= 0.999f)) layer = 0;
                var scale = 0.25f;

                var tile = building.MainTile;
                var x = tile.CentrePosition.X - (262 * scale * 0.5f);
                var y = tile.CentrePosition.Y - (131 * scale * 0.5f);

                float width = 262 * scale;
                float height = 131 * scale;

                float tx = layer / 3f;
                float ty = 0;
                float tw = 1 / 3f;
                float th = 1f;

                var c = GetVertexColor(building);
                vertexArray[i] = new VertexPositionColorTexture(new Vector3(x, y, 0), c, new Vector2(tx, ty));
                vertexArray[i + 1] = new VertexPositionColorTexture(new Vector3(x + width, y, 0), c, new Vector2(tx + tw, ty));
                vertexArray[i + 2] = new VertexPositionColorTexture(new Vector3(x, y + height, 0), c, new Vector2(tx, ty + th));
                vertexArray[i + 3] = new VertexPositionColorTexture(new Vector3(x + width, y + height, 0), c, new Vector2(tx + tw, ty + th));

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

        private void OnBuildingPartUpdated(object sender)
        {
            var thing = sender as Thing;
            if (thing?.ThingType == ThingType.LaunchPad)
            {
                this.UpdateBuffers();
            }
        }

        private static Color GetVertexColor(IThing thing)
        {
            var alpha = thing is Blueprint ? (thing as Blueprint).ColourA : thing.RenderAlpha;
            return new Color(0f, 0f, 1f, alpha);
        }
    }
}