namespace SigmaDraconis.Renderers
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;
    using Shared;
    using World;
    using WorldInterfaces;

    public class CropIconRenderer : RendererBase, IRenderer
    {
        private readonly HashSet<int> nonEmptyRows = new HashSet<int>();
        private bool isVisible;
        public bool IsVisible
        {
            get => this.isVisible;
            set
            {
                if (this.isVisible != value)
                {
                    this.isVisible = value;
                    this.InvalidateAll();
                }
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();
            this.texture1 = Content.Load<Texture2D>("Textures\\Icons\\CropOverlayIcons");
            this.effect = Content.Load<Effect>("Effects\\SimpleEffect").Clone();
            this.effect.CurrentTechnique = this.effect.Techniques["AlphaTechnique"];
            this.effect.Parameters["xTexture"].SetValue(this.texture1);
        }

        private void InvalidateAll()
        {
            foreach (var row in World.GetThings(ThingType.PlanterStone, ThingType.PlanterHydroponics).Select(t => t.MainTile.Row).Distinct())
            {
                if (!this.nonEmptyRows.Contains(row)) this.nonEmptyRows.Add(row);
            }

            foreach (var row in this.nonEmptyRows) this.InvalidateRow(row);
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            if (zoom != this.zoom) this.InvalidateAll();
            base.Update(time, scrollPos, zoom, isPaused);
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var planters = this.zoom >= 2.1f ? World.GetThings<IPlanter>(ThingType.PlanterStone, ThingType.PlanterHydroponics).Where(p => p.IsReady && p.MainTile.Row == rowNum).ToList() : new List<IPlanter>();

            var size = planters.Sum(p => (p.SelectedCropTypeId == p.CurrentCropTypeId ? 4 : 12) + (p.RemoveCrop && p.CurrentCropTypeId > 0 ? 4 : 0));

            VertexPositionColorTexture[] vertex = new VertexPositionColorTexture[size];
            int i = 0;

            var z = Mathf.Max(32f / this.zoom, 4f);
            var alpha = this.zoom > 4f ? 1f : 0.5f * (this.zoom - 2f);
            var tw = this.texture1.Height / (float)this.texture1.Width;
            foreach (var planter in planters)
            {
                var p = planter.MainTile.CentrePosition;
                var x = p.X - (z / 2f);
                var y = p.Y - (z * 1.25f);
                var width = z;
                var height = z;

                if (planter.CurrentCropTypeId != planter.SelectedCropTypeId) x -= z * 0.75f;

                var tx1 = tw * (planter.CurrentCropTypeId + 2);
                var tx2 = tx1 + tw;

                var c = new Color(alpha, alpha, alpha, alpha);
                vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx1, 0), Color = c };
                vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(tx2, 0), Color = c };
                vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(tx1, 1), Color = c };
                vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(tx2, 1), Color = c };
                i += 4;

                if (planter.RemoveCrop && planter.CurrentCropTypeId > 0)
                {
                    tx1 = tw;
                    tx2 = tx1 + tw;

                    vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx1, 0), Color = c };
                    vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(tx2, 0), Color = c };
                    vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(tx1, 1), Color = c };
                    vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(tx2, 1), Color = c };
                    i += 4;
                }

                if (planter.CurrentCropTypeId != planter.SelectedCropTypeId)
                {
                    x = p.X - (z / 2f);
                    tx1 = 0;
                    tx2 = tw;

                    vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx1, 0), Color = c };
                    vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(tx2, 0), Color = c };
                    vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(tx1, 1), Color = c };
                    vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(tx2, 1), Color = c };
                    i += 4;

                    x = p.X + (z / 4f);
                    tx1 = tw * (planter.SelectedCropTypeId + 2);
                    tx2 = tx1 + tw;

                    vertex[i] = new VertexPositionColorTexture { Position = new Vector3(x, y, 0), TextureCoordinate = new Vector2(tx1, 0), Color = c };
                    vertex[i + 1] = new VertexPositionColorTexture { Position = new Vector3(x + width, y, 0), TextureCoordinate = new Vector2(tx2, 0), Color = c };
                    vertex[i + 2] = new VertexPositionColorTexture { Position = new Vector3(x, y + height, 0), TextureCoordinate = new Vector2(tx1, 1), Color = c };
                    vertex[i + 3] = new VertexPositionColorTexture { Position = new Vector3(x + width, y + height, 0), TextureCoordinate = new Vector2(tx2, 1), Color = c };
                    i += 4;
                }
            }

            this.SetBufferData(rowNum, vertex, size);

            if (i == 0 && this.nonEmptyRows.Contains(rowNum))
            {
                this.nonEmptyRows.Remove(rowNum);
            }
        }
    }
}
