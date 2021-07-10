namespace SigmaDraconis.Renderers
{
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using UI;
    using UI.Managers;
    using World;
    using WorldInterfaces;

    public class TerrainTileHighlightRenderer : IRenderer, IDisposable
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
        private ISmallTile highlightedTile = null;
        private Direction edgeDirection = Direction.SW;
        private IThing selectedThing = null;
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

            // Always have 4 primitives: Two for the highlighted tile and two for the selected tile
            var indexArray = new int[12] { 0, 1, 2, 2, 3, 0, 4, 5, 6, 6, 7, 4 };
            this.indexBuffer.SetData(indexArray);
        }

        public void Update(Vector2f scrollPos, float zoom)
        {
            var invalidateBuffers = this.isBufferInvalidated || this.graphics.Viewport.Width != this.viewWidth || this.graphics.Viewport.Height != this.viewHeight
                || this.selectedThing != PlayerWorldInteractionManager.SelectedThing
                || PlayerWorldInteractionManager.SelectedThing != null && (
                    EventManager.MovedBugs.Contains(this.selectedThing.Id)
                    || EventManager.MovedColonists.Contains(this.selectedThing.Id)
                    || EventManager.MovedAnimals.Contains(this.selectedThing.Id));

            var direction = Direction.None;
            var newHighlightedTile = MouseWorldPosition.Tile;
            if (PlayerWorldInteractionManager.CurrentThingTypeToBuild?.In(ThingType.Wall, ThingType.Door) == true)
            {
                direction = PlayerActivityBuild.CurrentDirectionToBuild;
                newHighlightedTile = PlayerActivityBuild.BlueprintTargetTile;
            }
            else if (MouseWorldPosition.IsEdge && MouseWorldPosition.Tile != null)
            {
                var tile = MouseWorldPosition.Tile;
                if (MouseWorldPosition.ClosestEdge == Direction.SW && tile.ThingsPrimary.Any(t => t is IWall wall && wall.Direction == Direction.SW)) direction = Direction.SW;
                else if (MouseWorldPosition.ClosestEdge == Direction.SE && tile.ThingsPrimary.Any(t => t is IWall wall && wall.Direction == Direction.SE)) direction = Direction.SE;
                else if (MouseWorldPosition.ClosestEdge == Direction.NE && tile.TileToNE != null && tile.TileToNE.ThingsPrimary.Any(t => t is IWall wall && wall.Direction == Direction.SW))
                {
                    newHighlightedTile = tile.TileToNE;
                    direction = Direction.SW;
                }
                else if (MouseWorldPosition.ClosestEdge == Direction.NW && tile.TileToNW != null && tile.TileToNW.ThingsPrimary.Any(t => t is IWall wall && wall.Direction == Direction.SE))
                {
                    newHighlightedTile = tile.TileToNW;
                    direction = Direction.SE;
                }
            }

            if (direction != this.edgeDirection)
            {
                this.edgeDirection = direction;
                invalidateBuffers = true;
            }

            if (newHighlightedTile != this.highlightedTile)
            {
                this.highlightedTile = newHighlightedTile;
                invalidateBuffers = true;
            }

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
                this.selectedThing = PlayerWorldInteractionManager.SelectedThing;
                this.UpdateBuffers();
            }
        }

        public void InvalidateBuffers()
        {
            this.isBufferInvalidated = true;
            this.selectedThing = null;
            this.highlightedTile = null;
        }

        public void UpdateBuffers()
        {
            this.isBufferInvalidated = false;

            VertexPositionColorTexture[] vertexArray = new VertexPositionColorTexture[8];

            var colour = Color.LightBlue;

            if (this.highlightedTile != null)
            {
                var cx = this.highlightedTile.CentrePosition.X;
                var cy = this.highlightedTile.CentrePosition.Y;

                if (this.edgeDirection == Direction.SE)
                {
                    vertexArray[0] = new VertexPositionColorTexture { Position = new Vector3(cx - 21.33f, cy + 5.33f, 0), TextureCoordinate = new Vector2(4 / 24f, 7 / 11f), Color = colour };
                    vertexArray[1] = new VertexPositionColorTexture { Position = new Vector3(cx + 10.67f, cy - 10.67f, 0), TextureCoordinate = new Vector2(4 / 24f, 4 / 11f), Color = colour };
                    vertexArray[2] = new VertexPositionColorTexture { Position = new Vector3(cx + 32f, cy, 0), TextureCoordinate = new Vector2(6 / 24f, 4 / 11f), Color = colour };
                    vertexArray[3] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 16f, 0), TextureCoordinate = new Vector2(6 / 24f, 7 / 11f), Color = colour };
                }
                else if (this.edgeDirection == Direction.SW)
                {
                    vertexArray[0] = new VertexPositionColorTexture { Position = new Vector3(cx - 32f, cy, 0), TextureCoordinate = new Vector2(4 / 24f, 4 / 11f), Color = colour };
                    vertexArray[1] = new VertexPositionColorTexture { Position = new Vector3(cx - 10.67f, cy - 10.67f, 0), TextureCoordinate = new Vector2(6 / 24f, 4 / 11f), Color = colour };
                    vertexArray[2] = new VertexPositionColorTexture { Position = new Vector3(cx + 21.33f, cy + 5.33f, 0), TextureCoordinate = new Vector2(6 / 24f, 7 / 11f), Color = colour };
                    vertexArray[3] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 16f, 0), TextureCoordinate = new Vector2(4 / 24f, 7 / 11f), Color = colour };
                }
                else
                {
                    vertexArray[0] = new VertexPositionColorTexture { Position = new Vector3(cx - 32f, cy, 0), TextureCoordinate = new Vector2(0, 3 / 11f), Color = colour };
                    vertexArray[1] = new VertexPositionColorTexture { Position = new Vector3(cx, cy - 16f, 0), TextureCoordinate = new Vector2(0, 0), Color = colour };
                    vertexArray[2] = new VertexPositionColorTexture { Position = new Vector3(cx + 32f, cy, 0), TextureCoordinate = new Vector2(3 / 24f, 0), Color = colour };
                    vertexArray[3] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 16f, 0), TextureCoordinate = new Vector2(3 / 24f, 3 / 11f), Color = colour };
                }
            }
            else
            {
                vertexArray[0] = new VertexPositionColorTexture { Position = new Vector3(0, 0, 0), TextureCoordinate = new Vector2(0, 0), Color = Color.Transparent };
                vertexArray[1] = new VertexPositionColorTexture { Position = new Vector3(0, 0, 0), TextureCoordinate = new Vector2(0, 0), Color = Color.Transparent };
                vertexArray[2] = new VertexPositionColorTexture { Position = new Vector3(0, 0, 0), TextureCoordinate = new Vector2(0, 0), Color = Color.Transparent };
                vertexArray[3] = new VertexPositionColorTexture { Position = new Vector3(0, 0, 0), TextureCoordinate = new Vector2(0, 0), Color = Color.Transparent };
            }

            if (this.selectedThing is Thing thing && thing.ThingType != ThingType.Roof)
            {
                var cx = thing.MainTile.CentrePosition.X;
                var cy = thing.MainTile.CentrePosition.Y;

                var thingSize = thing.AllTiles.Count();
                if (thingSize == 4)
                {
                    vertexArray[4] = new VertexPositionColorTexture { Position = new Vector3(cx - 32f, cy, 0), TextureCoordinate = new Vector2(0, 7 / 11f), Color = colour };
                    vertexArray[5] = new VertexPositionColorTexture { Position = new Vector3(cx + 10.67f, cy - 21.33f, 0), TextureCoordinate = new Vector2(0, 3 / 11f), Color = colour };
                    vertexArray[6] = new VertexPositionColorTexture { Position = new Vector3(cx + 53.33f, cy, 0), TextureCoordinate = new Vector2(4 / 24f, 3 / 11f), Color = colour };
                    vertexArray[7] = new VertexPositionColorTexture { Position = new Vector3(cx + 10.67f, cy + 21.33f, 0), TextureCoordinate = new Vector2(4 / 24f, 7 / 11f), Color = colour };
                }
                else if (thingSize == 9)
                {
                    vertexArray[4] = new VertexPositionColorTexture { Position = new Vector3(cx - 53.33f, cy, 0), TextureCoordinate = new Vector2(6 / 24f, 5 / 11f), Color = colour };
                    vertexArray[5] = new VertexPositionColorTexture { Position = new Vector3(cx, cy - 26.67f, 0), TextureCoordinate = new Vector2(6 / 24f, 0), Color = colour };
                    vertexArray[6] = new VertexPositionColorTexture { Position = new Vector3(cx + 53.33f, cy, 0), TextureCoordinate = new Vector2(11 / 24f, 0), Color = colour };
                    vertexArray[7] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 26.67f, 0), TextureCoordinate = new Vector2(11 / 24f, 5 / 11f), Color = colour };
                }
                else if (thingSize == 16)
                {
                    vertexArray[4] = new VertexPositionColorTexture { Position = new Vector3(cx - 53.33f, cy, 0), TextureCoordinate = new Vector2(11 / 24f, 6 / 11f), Color = colour };
                    vertexArray[5] = new VertexPositionColorTexture { Position = new Vector3(cx + 10.67f, cy - 32f, 0), TextureCoordinate = new Vector2(11 / 24f, 0), Color = colour };
                    vertexArray[6] = new VertexPositionColorTexture { Position = new Vector3(cx + 74.67f, cy, 0), TextureCoordinate = new Vector2(17 / 24f, 0), Color = colour };
                    vertexArray[7] = new VertexPositionColorTexture { Position = new Vector3(cx + 10.67f, cy + 32f, 0), TextureCoordinate = new Vector2(17 / 24f, 6 / 11f), Color = colour };
                }
                else if (thingSize == 25)
                {
                    vertexArray[4] = new VertexPositionColorTexture { Position = new Vector3(cx - 74.67f, cy, 0), TextureCoordinate = new Vector2(17 / 24f, 7 / 11f), Color = colour };
                    vertexArray[5] = new VertexPositionColorTexture { Position = new Vector3(cx, cy - 37.33f, 0), TextureCoordinate = new Vector2(17 / 24f, 0), Color = colour };
                    vertexArray[6] = new VertexPositionColorTexture { Position = new Vector3(cx + 74.67f, cy, 0), TextureCoordinate = new Vector2(1, 0), Color = colour };
                    vertexArray[7] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 37.33f, 0), TextureCoordinate = new Vector2(1, 7 / 11f), Color = colour };
                }
                else if (this.selectedThing is IWall wall)
                {
                    if (wall.Direction == Direction.SE)
                    {
                        vertexArray[4] = new VertexPositionColorTexture { Position = new Vector3(cx - 21.33f, cy + 5.33f, 0), TextureCoordinate = new Vector2(10 / 24f, 7 / 12f), Color = colour };
                        vertexArray[5] = new VertexPositionColorTexture { Position = new Vector3(cx + 10.67f, cy - 10.67f, 0), TextureCoordinate = new Vector2(7 / 24f, 7 / 12f), Color = colour };
                        vertexArray[6] = new VertexPositionColorTexture { Position = new Vector3(cx + 32f, cy, 0), TextureCoordinate = new Vector2(7 / 24f, 5 / 7f), Color = colour };
                        vertexArray[7] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 16f, 0), TextureCoordinate = new Vector2(10 / 24f, 5 / 7f), Color = colour };
                    }
                    else if (wall.Direction == Direction.SW)
                    {
                        vertexArray[4] = new VertexPositionColorTexture { Position = new Vector3(cx - 32f, cy, 0), TextureCoordinate = new Vector2(7 / 24f, 7 / 12f), Color = colour };
                        vertexArray[5] = new VertexPositionColorTexture { Position = new Vector3(cx - 10.67f, cy - 10.67f, 0), TextureCoordinate = new Vector2(7 / 24f, 5 / 7f), Color = colour };
                        vertexArray[6] = new VertexPositionColorTexture { Position = new Vector3(cx + 21.33f, cy + 5.33f, 0), TextureCoordinate = new Vector2(10 / 24f, 5 / 7f), Color = colour };
                        vertexArray[7] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 16f, 0), TextureCoordinate = new Vector2(10 / 24f, 7 / 12f), Color = colour };
                    }
                }
                else if (this.selectedThing.ThingType == ThingType.Lander)
                {
                    vertexArray[4] = new VertexPositionColorTexture { Position = new Vector3(cx - 53.33f, cy, 0), TextureCoordinate = new Vector2(11 / 24f, 6 / 11f), Color = colour };
                    vertexArray[5] = new VertexPositionColorTexture { Position = new Vector3(cx, cy - 26.67f, 0), TextureCoordinate = new Vector2(11 / 24f, 1), Color = colour };
                    vertexArray[6] = new VertexPositionColorTexture { Position = new Vector3(cx + 53.33f, cy, 0), TextureCoordinate = new Vector2(16 / 24f, 1), Color = colour };
                    vertexArray[7] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 26.67f, 0), TextureCoordinate = new Vector2(16 / 24f, 6 / 11f), Color = colour };
                }
                else
                {
                    if (this.selectedThing is IMoveableThing mt)
                    {
                        cx += mt.PositionOffset.X;
                        cy += mt.PositionOffset.Y;
                    }

                    vertexArray[4] = new VertexPositionColorTexture { Position = new Vector3(cx - 32f, cy, 0), TextureCoordinate = new Vector2(3 / 24f, 3 / 11f), Color = colour };
                    vertexArray[5] = new VertexPositionColorTexture { Position = new Vector3(cx, cy - 16f, 0), TextureCoordinate = new Vector2(3 / 24f, 0), Color = colour };
                    vertexArray[6] = new VertexPositionColorTexture { Position = new Vector3(cx + 32f, cy, 0), TextureCoordinate = new Vector2(6 / 24f, 0), Color = colour };
                    vertexArray[7] = new VertexPositionColorTexture { Position = new Vector3(cx, cy + 16f, 0), TextureCoordinate = new Vector2(6 / 24f, 3 / 11f), Color = colour };
                }
            }
            else
            {
                vertexArray[4] = new VertexPositionColorTexture { Position = new Vector3(0, 0, 0), TextureCoordinate = new Vector2(0, 0), Color = Color.Transparent };
                vertexArray[5] = new VertexPositionColorTexture { Position = new Vector3(0, 0, 0), TextureCoordinate = new Vector2(0, 0), Color = Color.Transparent };
                vertexArray[6] = new VertexPositionColorTexture { Position = new Vector3(0, 0, 0), TextureCoordinate = new Vector2(0, 0), Color = Color.Transparent };
                vertexArray[7] = new VertexPositionColorTexture { Position = new Vector3(0, 0, 0), TextureCoordinate = new Vector2(0, 0), Color = Color.Transparent };
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
