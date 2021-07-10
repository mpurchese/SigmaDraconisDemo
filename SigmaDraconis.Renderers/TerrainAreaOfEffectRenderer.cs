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
    using WorldInterfaces;

    public class TerrainAreaOfEffectRenderer : IRenderer, IDisposable
    {
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;
        private Effect effect;
        private GraphicsDevice graphics;
        private ContentManager content;
        private readonly Vector2f scrollPosition = new Vector2i(0, 0);
        private float zoom;
        private int viewWidth;
        private int viewHeight;
        private IThing virtualBlueprint = null;
        private bool isBufferInvalidated = true;

        public bool IsVisible { get; set; } = true;

        public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.content = contentManager;

            this.effect = content.Load<Effect>("Effects\\TerrainOverlayEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(content.Load<Texture2D>("Textures\\Tiles\\GridHighlight"));
            this.effect.Parameters["xGlobalAlpha"].SetValue(1f);

            this.vertexBuffer = new VertexBuffer(this.graphics, VertexPositionColorTexture.VertexDeclaration, 8, BufferUsage.WriteOnly);
            this.indexBuffer = new IndexBuffer(this.graphics, typeof(int), 12, BufferUsage.WriteOnly);

            // Always have 2 primitives
            var indexArray = new int[6] { 0, 1, 2, 2, 3, 0 };
            this.indexBuffer.SetData(indexArray);
        }

        public void Update(Vector2f scrollPos, float zoom)
        {
            var worldVB = World.VirtualBlueprint.FirstOrDefault();
            if (worldVB == null || worldVB.MainTile == null || (worldVB.ThingType != ThingType.Mine && worldVB.ThingType != ThingType.DirectionalHeater)) worldVB = null;
            var invalidateBuffers = this.isBufferInvalidated || this.graphics.Viewport.Width != this.viewWidth || this.graphics.Viewport.Height != this.viewHeight
                || this.virtualBlueprint != worldVB;

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
                Matrix projection = Matrix.CreateOrthographicOffCenter(sx, sx + this.viewWidth, sy + this.viewHeight, sy, 0, 1) * Matrix.CreateScale(zoom);

                this.effect.Parameters["xViewProjection"].SetValue(projection);
            }

            this.scrollPosition.X = scrollPos.X;
            this.scrollPosition.Y = scrollPos.Y;
            this.zoom = zoom;

            if (invalidateBuffers)
            {
                this.virtualBlueprint = worldVB;
                this.UpdateBuffers();
            }
        }

        public void InvalidateBuffers()
        {
            this.isBufferInvalidated = true;
            this.virtualBlueprint = null;
        }

        public void UpdateBuffers()
        {
            this.isBufferInvalidated = false;

            VertexPositionColorTexture[] vertexArray = new VertexPositionColorTexture[4];

            var empty = true;
            if (this.virtualBlueprint is IBlueprint blueprint)
            {
                if (blueprint.ThingType == ThingType.DirectionalHeater)
                {
                    var tile = blueprint.MainTile.GetTileToDirection(blueprint.Direction);
                    if (tile != null)
                    {
                        var colour = Color.Bisque;
                        colour.A = 127;
                        var cx = tile.CentrePosition.X;
                        var cy = tile.CentrePosition.Y;
                        vertexArray[0] = new VertexPositionColorTexture { Position = new Vector3(cx - 32f, cy, 0), TextureCoordinate = new Vector2(3 / 24f, 3 / 11f), Color = colour };
                        vertexArray[1] = new VertexPositionColorTexture { Position = new Vector3(cx, cy - 16f, 0), TextureCoordinate = new Vector2(3 / 24f, 0), Color = colour };
                        vertexArray[2] = new VertexPositionColorTexture { Position = new Vector3(cx + 32f, cy, 0), TextureCoordinate = new Vector2(6 / 24f, 0), Color = colour };
                        vertexArray[3] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 16f, 0), TextureCoordinate = new Vector2(6 / 24f, 3 / 11f), Color = colour };
                        empty = false;
                    }
                }
                else
                {
                    var colour = Color.LightBlue;
                    var cx = blueprint.MainTile.CentrePosition.X;
                    var cy = blueprint.MainTile.CentrePosition.Y;
                    vertexArray[0] = new VertexPositionColorTexture { Position = new Vector3(cx - 53.33f, cy, 0), TextureCoordinate = new Vector2(6 / 24f, 5 / 11f), Color = colour };
                    vertexArray[1] = new VertexPositionColorTexture { Position = new Vector3(cx, cy - 26.67f, 0), TextureCoordinate = new Vector2(6 / 24f, 0), Color = colour };
                    vertexArray[2] = new VertexPositionColorTexture { Position = new Vector3(cx + 53.33f, cy, 0), TextureCoordinate = new Vector2(11 / 24f, 0), Color = colour };
                    vertexArray[3] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 26.67f, 0), TextureCoordinate = new Vector2(11 / 24f, 5 / 11f), Color = colour };
                    empty = false;
                }
            }

            if (empty)
            {
                vertexArray[0] = new VertexPositionColorTexture { Position = new Vector3(0, 0, 0), TextureCoordinate = new Vector2(0, 0), Color = Color.Transparent };
                vertexArray[1] = new VertexPositionColorTexture { Position = new Vector3(0, 0, 0), TextureCoordinate = new Vector2(0, 0), Color = Color.Transparent };
                vertexArray[2] = new VertexPositionColorTexture { Position = new Vector3(0, 0, 0), TextureCoordinate = new Vector2(0, 0), Color = Color.Transparent };
                vertexArray[3] = new VertexPositionColorTexture { Position = new Vector3(0, 0, 0), TextureCoordinate = new Vector2(0, 0), Color = Color.Transparent };
            }

            this.vertexBuffer.SetData(vertexArray);
        }

        public void Draw()
        {
            if (!this.IsVisible) return;

            this.effect.CurrentTechnique = this.effect.Techniques["MainTechnique"];
            this.graphics.SetVertexBuffer(vertexBuffer);
            this.graphics.Indices = indexBuffer;
            var prevBlendState = this.graphics.BlendState;
            this.graphics.BlendState = BlendState.AlphaBlend;
            this.effect.CurrentTechnique.Passes[0].Apply();
            this.graphics.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4);
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
