namespace SigmaDraconis.World.Terrain
{
    using System.Collections.Generic;
    using Shared;
    using WorldInterfaces;

    public class SmallTileMap
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public Dictionary<int, ISmallTile> Tiles { get; private set; }
        public List<List<ISmallTile>> TilesByRow { get; private set; } = new List<List<ISmallTile>>();

        public SmallTileMap(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            this.Tiles = new Dictionary<int, ISmallTile>(width * height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var index = (y * width) + x;
                    var smallTile = new SmallTile(null, index, x, y, y + (height - 1) - x);
                    this.Tiles.Add(index, smallTile);
                }
            }

            this.LinkTiles();
        }

        public SmallTileMap(BigTileMap bigMap)
        {
            this.Width = bigMap.Width * 3;
            this.Height = bigMap.Height * 3;
            this.Tiles = new Dictionary<int, ISmallTile>(this.Width * this.Height);

            foreach (var bigTile in bigMap.Tiles)
            {
                this.MakeSmallTile(bigTile, Direction.None);
                this.MakeSmallTile(bigTile, Direction.N);
                this.MakeSmallTile(bigTile, Direction.NE);
                this.MakeSmallTile(bigTile, Direction.E);
                this.MakeSmallTile(bigTile, Direction.SE);
                this.MakeSmallTile(bigTile, Direction.S);
                this.MakeSmallTile(bigTile, Direction.SW);
                this.MakeSmallTile(bigTile, Direction.W);
                this.MakeSmallTile(bigTile, Direction.NW);
            }

            this.LinkTiles();
        }

        private void MakeSmallTile(BigTile parent, Direction direction)
        {
            var x = parent.TerrainX * 3;
            var y = parent.TerrainY * 3;
            switch (direction)
            {
                case Direction.None:
                    x += 1;
                    y += 1;
                    break;
                case Direction.N:
                    x += 2;
                    break;
                case Direction.NE:
                    x += 2;
                    y += 1;
                    break;
                case Direction.E:
                    x += 2;
                    y += 2;
                    break;
                case Direction.SE:
                    x += 1;
                    y += 2;
                    break;
                case Direction.S:
                    y += 2;
                    break;
                case Direction.SW:
                    y += 1;
                    break;
                case Direction.NW:
                    x += 1;
                    break;
            }

            var index = (y * this.Width) + x;
            var smallTile = new SmallTile(parent, index, x, y, y + (this.Height - 1) - x);
            this.Tiles.Add(index, smallTile);
            parent.SmallTiles[direction] = smallTile;
        }

        public ISmallTile GetTile(int x, int y)
        {
            return (x >= 0 && y >= 0 && x < this.Width && y < this.Height) ? this.Tiles[x + (y * this.Width)] : null;
        }

        private void LinkTiles()
        {
            foreach (var tile in this.Tiles.Values)
            {
                var x = tile.X;
                var y = tile.Y;
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

                var row = tile.Row;
                while (this.TilesByRow.Count < row + 1)
                {
                    this.TilesByRow.Add(new List<ISmallTile>());
                }

                this.TilesByRow[row].Add(tile);
            }
        }
    }
}
