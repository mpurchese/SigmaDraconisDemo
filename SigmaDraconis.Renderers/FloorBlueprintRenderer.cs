namespace SigmaDraconis.Renderers
{
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Config;
    using Shared;
    using World;
    using World.Blueprints;

    public class FloorBlueprintRenderer : IRenderer, IDisposable
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
        private Vector2i textureSize;
        private int usedBufferSize = 0;
        private bool isBufferInvalidated = true;

        public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;

            this.effect = content.Load<Effect>("Effects\\BuildingEffect").Clone();
            this.ReloadContent();

            EventManager.Subscribe(EventType.Blueprint, delegate (object obj) { this.OnBlueprintUpdated(obj); });
            EventManager.Subscribe(EventType.VirtualBlueprint, delegate (object obj) { this.OnBlueprintUpdated(obj); });
            EventManager.Subscribe(EventType.RecycleBlueprint, delegate (object obj) { this.OnBlueprintUpdated(obj); });
        }

        public void ReloadContent()
        {
            var texturePath = WorldRenderer.Instance.TextureRes == 0 ? "Buildings\\LoRes\\FloorsA_1" : "Buildings\\FloorsA_1";
            var texture = WorldRenderer.Instance.GameTextureContent.Load<Texture2D>(texturePath);
            this.effect.Parameters["xNightTexture"].SetValue(texture);
            this.textureSize = new Vector2i(texture.Width, texture.Height);
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

            var blueprints = World.ConfirmedBlueprints.Values
                .Union(World.RecycleBlueprints.Values)
                .OfType<Blueprint>().Where(b => ThingTypeManager.IsRendererType(b.ThingType, RendererType.Floor))
                .OrderBy(b => b.ThingType == ThingType.LaunchPad ? 2 : 1).ToList();

            var vertexBufferSize = blueprints.Count * 5;
            var indexBufferSize = blueprints.Count * 12;
            if (vertexBufferSize == 0)
            {
                this.usedBufferSize = 0;
                return;
            }

            var arraySize = vertexBufferSize;
            if (this.vertexBuffer?.IsContentLost == false && !this.vertexBuffer.IsDisposed && this.vertexBuffer.VertexCount > vertexBufferSize)
                arraySize = this.vertexBuffer.VertexCount;

            VertexPositionColorTexture[] vertexArray = new VertexPositionColorTexture[arraySize];
            int i = 0;
            var textureScale = WorldRenderer.Instance.TextureRes == 1 ? 2f : 1f;

            foreach (var blueprint in blueprints)
            {
                var frame = TextureAtlasManager.GetFrame(TextureAtlasIdentifiers.Floor, blueprint.ThingType, blueprint.AnimationFrame);
                var scale = 0.25f;

                var tile = blueprint.MainTile;
                var x = tile.CentrePosition.X - (frame.Width * scale * 0.5f);
                var y = tile.CentrePosition.Y - (frame.Height * scale * 0.5f);

                var definition = ThingTypeManager.GetDefinition(blueprint.ThingType);
                if (definition.Size.X > 1) x += 10.67f;

                float width = frame.Width * scale;
                float height = frame.Height * scale;

                float tx = frame.X * textureScale / (float)this.textureSize.X;
                float ty = frame.Y * textureScale / (float)this.textureSize.Y;
                float tw = frame.Width * textureScale / (float)this.textureSize.X;
                float th = frame.Height * textureScale / (float)this.textureSize.Y;

                var c = new Color(blueprint.ColourR, blueprint.ColourG, blueprint.ColourB, blueprint.ColourA);

                vertexArray[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx, ty), Color = c };
                vertexArray[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(tx + tw, ty), Color = c };
                vertexArray[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(tx, ty + th), Color = c };
                vertexArray[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(tx + tw, ty + th), Color = c };

                i += 4;
            }

            // Set up index buffer
            var indexArray = new int[indexBufferSize];
            for (int k = 0, j = 0; j < vertexBufferSize; k += 6, j += 4)
            {
                indexArray[k] = j;
                indexArray[k + 1] = j + 1;
                indexArray[k + 2] = j + 2;
                indexArray[k + 3] = j + 2;
                indexArray[k + 4] = j + 1;
                indexArray[k + 5] = j + 3;
            }

            if (this.vertexBuffer == null || this.vertexBuffer.IsContentLost || this.vertexBuffer.IsDisposed || this.vertexBuffer.VertexCount < vertexBufferSize)
            {
                if (this.vertexBuffer?.IsDisposed == false)
                {
                    // Destroy the old buffer, it's invalidated or too small
                    this.vertexBuffer.Dispose();
                    this.vertexBuffer = null;
                }

                this.vertexBuffer = new DynamicVertexBuffer(this.graphics, VertexPositionColorTexture.VertexDeclaration, (int)(vertexBufferSize * bufferGrowthFactor), BufferUsage.WriteOnly);
            }

            this.vertexBuffer.SetData(vertexArray);
            this.usedBufferSize = blueprints.Count * 2;

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
            if (usedBufferSize == 0 || vertexBuffer == null || vertexBuffer.IsContentLost || vertexBuffer.IsDisposed || vertexBuffer.VertexCount == 0)
            {
                return;
            }

            if (indexBuffer == null || indexBuffer.IsContentLost || indexBuffer.IsDisposed)
            {
                return;
            }

            this.effect.CurrentTechnique = this.effect.Techniques["BlueprintTechnique"];
            this.effect.Parameters["xLightingFactors"].SetValue(new Vector4(1, 1, 1, 1));
            this.graphics.SetVertexBuffer(vertexBuffer);
            this.graphics.Indices = indexBuffer;
            var prevBlendState = this.graphics.BlendState;
            this.graphics.BlendState = BlendState.AlphaBlend;
            this.effect.CurrentTechnique.Passes[0].Apply();
            this.graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, this.usedBufferSize);
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
                if (this.effect?.IsDisposed == false) this.effect.Dispose();   // Cloned, we own this
                if (this.vertexBuffer != null) this.vertexBuffer.Dispose();
                if (this.indexBuffer != null) this.indexBuffer.Dispose();
            }
        }

        private void OnBlueprintUpdated(object sender)
        {
            if (sender is Blueprint t && t.MainTile != null && ThingTypeManager.IsRendererType(t.ThingType, RendererType.Floor))
            {
                this.UpdateBuffers();
            }
        }
    }
}
