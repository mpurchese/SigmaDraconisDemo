namespace SigmaDraconis.World.Terrain
{
    using System.Collections.Generic;
    using System.Linq;
    using Shared;

    public class BigTileMap
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public List<BigTile> Tiles { get; private set; }
        public List<BigTile> TilesWithDeepWaterEdge { get; private set; } = new List<BigTile>();   // For deepwater renderer
        public List<BigTile> TilesWithWater { get; private set; } = new List<BigTile>();           // For underwater renderer
        public List<BigTile> TilesWithLand { get; private set; } = new List<BigTile>();            // For abovewater renderer

        public BigTileMap(int width, int height)
        {
            this.Tiles = new List<BigTile>(width * height);

            this.Width = width;
            this.Height = height;

            for (int y = 0; y < width; y++)
            {
                for (int x = 0; x < height; x++)
                {
                    var newTile = new BigTile((y * width) + x, x, y, y + (width - 1) - x);
                    this.Tiles.Add(newTile);
                }
            }

            this.LinkTiles();
        }

        public BigTile GetTile(int x, int y)
        {
            return (x >= 0 && y >= 0 && x < this.Width && y < this.Height) ? this.Tiles[x + (y * this.Width)] : null;
        }

        public void UpdateRenderLists()
        {
            this.TilesWithDeepWaterEdge = this.Tiles.Where(t => t.TerrainType == TerrainType.DeepWaterEdge && t.BigTileTextureIdentifier != BigTileTextureIdentifier.None).OrderBy(t => t.TerrainRow).ToList();
            this.TilesWithWater = this.Tiles.Where(t => (t.TerrainType == TerrainType.Water || t.TerrainType == TerrainType.Coast) && t.BigTileTextureIdentifier != BigTileTextureIdentifier.None).OrderBy(t => t.TerrainRow).ToList();
            this.TilesWithLand = this.Tiles.Where(t => (t.TerrainType == TerrainType.Dirt || t.TerrainType == TerrainType.Coast) && t.BigTileTextureIdentifier != BigTileTextureIdentifier.None).OrderBy(t => t.TerrainRow).ToList();
        }

        private void LinkTiles()
        {
            foreach (var tile in this.Tiles)
            {
                var x = tile.TerrainX;
                var y = tile.TerrainY;
                tile.LinkTiles(
                    this.GetTile(x + 1, y - 1),
                    this.GetTile(x + 1, y),
                    this.GetTile(x + 1, y + 1),
                    this.GetTile(x, y + 1),
                    this.GetTile(x - 1, y + 1),
                    this.GetTile(x - 1, y),
                    this.GetTile(x - 1, y - 1),
                    this.GetTile(x, y - 1)
                );
            }
        }
    }
}
