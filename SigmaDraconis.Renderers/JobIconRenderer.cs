namespace SigmaDraconis.Renderers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using World;
    using WorldControllers;
    using WorldInterfaces;

    public class JobIconRenderer : RendererBase, IRenderer
    {
        private readonly HashSet<int> nonEmptyRows = new HashSet<int>();
        private bool isGeologyVisible;
        public bool IsGeologyVisible
        {
            get => this.isGeologyVisible;
            set
            {
                if (this.isGeologyVisible != value)
                {
                    this.isGeologyVisible = value;
                    this.InvalidateGeologyRows();
                }
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();
            var texture = Content.Load<Texture2D>("Textures\\Icons\\JobIcons");
            this.effect = Content.Load<Effect>("Effects\\SimpleEffect").Clone();
            this.effect.Parameters["xTexture"].SetValue(texture);
        }

        private void InvalidateGeologyRows()
        {
            foreach (var i in GeologyController.TilesToSurvey) this.InvalidateRow(World.GetSmallTile(i).Row);
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var positions = new List<Vector2f>();
            var iconIndexes = new List<int>();
            var alphas = new List<float>();
            var ty = 0f;

            foreach (var tile in World.GetSmallTilesByRow(rowNum))
            {
                foreach (var thing in tile.ThingsPrimary)
                {
                    if (thing is IMine mine && mine.IsMineExhausted && !mine.IsDesignatedForRecycling)
                    {
                        // Empty mines
                        positions.Add(tile.CentrePosition - new Vector2f(4f, 11));
                        iconIndexes.Add(2);
                        alphas.Add(0.8f);
                    }
                    else if (thing is IBuildableThing b && b.IsDesignatedForRecycling)
                    {
                        // Buildings being deconstructed
                        var height = b.ThingType == ThingType.Roof ? 10 : 0;
                        var tilePositions = b.AllTiles.Select(t => t.CentrePosition).ToList();
                        positions.Add(new Vector2f(tilePositions.Average(t => t.X) - 4f, tilePositions.Average(t => t.Y) - 8f - height));
                        iconIndexes.Add(1);
                        alphas.Add(Math.Max(0f, 0.8f * (1f - (0.01f * b.RecycleProgress))));
                    }
                }

                if (this.isGeologyVisible && GeologyController.TilesToSurvey.Contains(tile.Index))
                {
                    positions.Add(tile.CentrePosition - new Vector2f(4f, 4f));
                    iconIndexes.Add(3);
                    alphas.Add(0.8f);
                }
            }

            // Resources being deconstructed and plants being harvested
            var resources = World.ResourcesForDeconstruction.Where(r => r.Value == rowNum).Select(r => r.Key).ToList();
            var plants = World.GetPlantsForHarvest(rowNum);

            var size = (positions.Count + resources.Count + plants.Count) * 4;
            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;

            for (int index = 0; index < positions.Count; index++)
            {
                var x = positions[index].X;
                var y = positions[index].Y;
                var width = 8f;
                var height = 8f;

                var tx = iconIndexes[index] * 0.25f;

                var c = new Color(0.5f * alphas[index], 0.5f * alphas[index], 0.5f * alphas[index], 0.5f * alphas[index]);
                vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx, ty), Color = c };
                vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(tx + 0.25f, ty), Color = c };
                vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(tx, ty + 0.5f), Color = c };
                vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(tx + 0.25f, ty + 0.5f), Color = c };

                i += 4;
            }

            foreach (var id in resources)
            {
                var thing = World.GetThing(id);
                var p = GetDeconstructIconPosition(thing);
                var x = p.X;
                var y = p.Y;
                var width = 8f;
                var height = 8f;

                var tx = 0.25f;

                var c = new Color(0.4f, 0.4f, 0.4f, 0.4f);
                vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx, ty), Color = c };
                vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(tx + 0.25f, ty), Color = c };
                vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(tx, ty + 0.5f), Color = c };
                vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(tx + 0.25f, ty + 0.5f), Color = c };

                i += 4;
            }

            foreach (var plant in plants)
            {
                var p = GetDeconstructIconPosition(plant);
                var x = p.X;
                var y = p.Y;
                var width = 8f;
                var height = 8f;

                var tx = 0f;

                var c = new Color(0.4f, 0.4f, 0.4f, 0.4f);
                vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx, ty), Color = c };
                vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(tx + 0.25f, ty), Color = c };
                vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(tx, ty + 0.5f), Color = c };
                vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(tx + 0.25f, ty + 0.5f), Color = c };

                i += 4;
            }

            this.SetBufferData(rowNum, vertex, size);

            if (i == 0 && this.nonEmptyRows.Contains(rowNum))
            {
                this.nonEmptyRows.Remove(rowNum);
            }
        }

        private static Vector2f GetDeconstructIconPosition(IThing thing)
        {
            if (thing is IRenderOffsettable && thing.AllTiles.Count == 1)
            {
                // May be more than one thing in tile to deconstruct (coast grass)
                var tilePosition = World.GetSmallTile(thing.MainTileIndex).CentrePosition;
                var things = thing.MainTile.ThingsAll.OfType<IRenderOffsettable>().Where(t => t.ThingType == thing.ThingType).ToList();
                return new Vector2f(tilePosition.X + things.Average(t => (float)t.RenderPositionOffset.X) - 4f, tilePosition.Y + (float)things.Average(t => t.RenderPositionOffset.Y) - 8f);
            }

            var tilePositions = thing.AllTiles.Select(t => World.GetSmallTile(t.Index).CentrePosition);
            return new Vector2f(tilePositions.Average(t => t.X) - 4f, tilePositions.Average(t => t.Y) - 8f);
        }
    }
}
