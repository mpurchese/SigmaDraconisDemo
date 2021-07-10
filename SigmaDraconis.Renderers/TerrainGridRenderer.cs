namespace SigmaDraconis.Renderers
{
    using System;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using World;

    public class TerrainGridRenderer : RendererBase, IRenderer
    {
        private bool isVisible = true;

        public static TerrainGridRenderer Instance { get; private set; }

        public TerrainGridRenderer()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                throw new ApplicationException("TerrainGridRenderer already created");
            }
        }

        public bool IsVisible
        {
            get
            {
                return this.isVisible;
            }
            set
            {
                if (this.isVisible != value)
                {
                    this.isVisible = value;
                    this.isBufferInvalidated = true;
                }
            }
        }

        public override void LoadContent()
        {
            base.LoadContent();
            this.LoadBasicEffect("Textures\\Tiles\\Grid");
        }

        public override void Update(WorldTime time, Vector2f scrollPos, float zoom, bool isPaused = false)
        {
            if (this.IsVisible)
            {
                base.Update(time, scrollPos, zoom);
            }
            else if (this.isBufferInvalidated)
            {
                this.ClearBuffers();
                this.isBufferInvalidated = false;
            }
        }

        protected override void GenerateBuffersForRow(int rowNum)
        {
            var row = World.SmallTilesByRow[rowNum];
            var size = row.Count * 4;
            VertexPositionTexture[] vertex = new VertexPositionTexture[size];
            int j = 0;
            foreach (var tile in row)
            {
                var c = tile.CentrePosition;
                vertex[j].Position = new Vector3(c.X - 10.667f, c.Y, 0);
                vertex[j + 1].Position = new Vector3(c.X, c.Y - 5.333f, 0);
                vertex[j + 2].Position = new Vector3(c.X, c.Y + 5.333f, 0);
                vertex[j + 3].Position = new Vector3(c.X + 10.667f, c.Y, 0);
                vertex[j + 0].TextureCoordinate = new Vector2(0, 0);
                vertex[j + 1].TextureCoordinate = new Vector2(1, 0);
                vertex[j + 2].TextureCoordinate = new Vector2(0, 1);
                vertex[j + 3].TextureCoordinate = new Vector2(1, 1);
                j += 4;
            }

            this.SetBufferData(rowNum, vertex, size, 1f);
        }

        public override void Draw(int row)
        {
            if (this.IsVisible)
            {
                base.Draw(row);
            }
        }
    }
}
