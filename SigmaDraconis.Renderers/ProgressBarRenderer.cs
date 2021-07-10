namespace SigmaDraconis.Renderers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Shared;
    using World;
    using WorldInterfaces;

    public class ProgressBarRenderer : RendererBase
    {
        private readonly HashSet<int> activeRows = new HashSet<int>();

        public override void LoadContent()
        {
            base.LoadContent();

            var texture = new Texture2D(Graphics, 1, 1);
            texture.SetData(new Color[1] { Color.White });

            this.LoadBasicEffect("Textures\\Misc\\ProgressBar");
        }

        protected override void GenerateBuffersForInvalidatedRows()
        {
            foreach (var r in this.activeRows) this.InvalidateRow(r);
            base.GenerateBuffersForInvalidatedRows();
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var row = World.GetSmallTilesByRow(rowNum);

            var coords = new List<Tuple<float, float, float>>();
            foreach (var thing in row.SelectMany(r => r.ThingsPrimary))
            {
                if (thing is IPlanter planter && planter.JobProgress.HasValue)
                {
                    var progress = planter.JobProgress.Value;
                    coords.Add(new Tuple<float, float, float>(planter.MainTile.CentrePosition.X, planter.MainTile.CentrePosition.Y - 6f, progress));
                }
                else if (thing is IFruitPlant plant && plant.HarvestJobProgress.HasValue && plant.CountFruitAvailable > 0)
                {
                    var progress = plant.HarvestJobProgress.Value;
                    if (plant.AllTiles.Count > 1)
                    {
                        var x = plant.AllTiles.Average(t => t.CentrePosition.X);
                        var y = plant.AllTiles.Average(t => t.CentrePosition.Y);
                        coords.Add(new Tuple<float, float, float>(x, y - 6f, progress));
                    }
                    else coords.Add(new Tuple<float, float, float>(plant.MainTile.CentrePosition.X, plant.MainTile.CentrePosition.Y - 6f, progress));
                }
                else if (thing is IColonist c && c.ActivityType == ColonistActivityType.Geology && c.MainTile.MineResourceSurveyProgress > 0.0 && !c.MainTile.IsMineResourceVisible)
                {
                    var progress = (float)c.MainTile.MineResourceSurveyProgress;
                    coords.Add(new Tuple<float, float, float>(c.RenderPos.X, c.RenderPos.Y - 12, progress));
                }
            }

            var size = coords.Count() * 12;
            VertexPositionTexture[] vertex = new VertexPositionTexture[size];
            int i = 0;

            if (size > 0 && !this.activeRows.Contains(rowNum)) this.activeRows.Add(rowNum);
            else if (size == 0 && this.activeRows.Contains(rowNum)) this.activeRows.Remove(rowNum);

            var s1 = 6f;
            var s2 = 5.625f;
            foreach (var coord in coords)
            {
                float x = coord.Item1;
                float y = coord.Item2;
                var progress = coord.Item3 * 11.25f;

                vertex[i] = new VertexPositionTexture { Position = new Vector3(x - s1, y - 0.667f, 0), TextureCoordinate = new Vector2(0, 12 / 32f) };
                vertex[i + 1] = new VertexPositionTexture { Position = new Vector3(x + s1, y - 0.667f, 0), TextureCoordinate = new Vector2(1, 12 / 32f) };
                vertex[i + 2] = new VertexPositionTexture { Position = new Vector3(x - s1, y + 0.667f, 0), TextureCoordinate = new Vector2(0, 18 / 32f) };
                vertex[i + 3] = new VertexPositionTexture { Position = new Vector3(x + s1, y + 0.667f, 0), TextureCoordinate = new Vector2(1, 18 / 32f) };

                vertex[i + 4] = new VertexPositionTexture { Position = new Vector3(x - s2, y - 0.333f, 0), TextureCoordinate = new Vector2(0.5f, 27 / 32f) };
                vertex[i + 5] = new VertexPositionTexture { Position = new Vector3(x + progress - s2, y - 0.333f, 0), TextureCoordinate = new Vector2(0.5f, 27 / 32f) };
                vertex[i + 6] = new VertexPositionTexture { Position = new Vector3(x - s2, y + 0.333f, 0), TextureCoordinate = new Vector2(0.5f, 27 / 32f) };
                vertex[i + 7] = new VertexPositionTexture { Position = new Vector3(x + progress - s2, y + 0.333f, 0), TextureCoordinate = new Vector2(0.5f, 27 / 32f) };

                vertex[i + 8] = new VertexPositionTexture { Position = new Vector3(x - s1, y - 0.667f, 0), TextureCoordinate = new Vector2(0, 0) };
                vertex[i + 9] = new VertexPositionTexture { Position = new Vector3(x + s1, y - 0.667f, 0), TextureCoordinate = new Vector2(1, 0) };
                vertex[i + 10] = new VertexPositionTexture { Position = new Vector3(x - s1, y + 0.667f, 0), TextureCoordinate = new Vector2(0, 6 / 32f) };
                vertex[i + 11] = new VertexPositionTexture { Position = new Vector3(x + s1, y + 0.667f, 0), TextureCoordinate = new Vector2(1, 6 / 32f) };
                i += 12;
            }

            this.SetBufferData(rowNum, vertex, size);
        }
    }
}
