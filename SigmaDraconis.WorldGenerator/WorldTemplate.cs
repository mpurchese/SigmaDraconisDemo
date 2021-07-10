namespace SigmaDraconis.WorldGenerator
{
    using System.Collections.Generic;
    using System.Linq;
    using Shared;

    public class WorldTemplate
    {
        public List<BigTileTemplate> BigTiles = new List<BigTileTemplate>();
        public Dictionary<int, SmallTileTemplate> SmallTiles = new Dictionary<int, SmallTileTemplate>();
        private readonly List<RockTemplate> rocks = new List<RockTemplate>();
        private readonly List<PlantTemplate> plants = new List<PlantTemplate>();
        private int bigSize;
        private int size;

        public void Clear()
        {
            this.BigTiles.Clear();
            this.SmallTiles.Clear();
            this.rocks.Clear();
            this.plants.Clear();
        }

        public void Clear(int size)
        {
            this.Clear();

            this.bigSize = size;
            this.size = size * 3;

            for (int y = 0; y < this.bigSize; y++)
            {
                for (int x = 0; x < this.bigSize; x++)
                {
                    var bigTile = new BigTileTemplate((y * this.bigSize) + x, x, y);
                    this.BigTiles.Add(bigTile);
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
            }

            this.LinkBigTiles();
            this.LinkSmallTiles();
        }

        private void MakeSmallTile(BigTileTemplate parent, Direction direction)
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

            var index = (y * this.size) + x;
            var smallTile = new SmallTileTemplate(parent, index, x, y);
            this.SmallTiles.Add(index, smallTile);
            parent.SmallTiles[direction] = smallTile;
        }

        private void LinkBigTiles()
        {
            foreach (var tile in this.BigTiles)
            {
                var x = tile.TerrainX;
                var y = tile.TerrainY;
                tile.LinkTiles(
                    this.GetBigTile(x + 1, y - 1),
                    this.GetBigTile(x + 1, y),
                    this.GetBigTile(x + 1, y + 1),
                    this.GetBigTile(x, y + 1),
                    this.GetBigTile(x - 1, y + 1),
                    this.GetBigTile(x - 1, y),
                    this.GetBigTile(x - 1, y - 1),
                    this.GetBigTile(x, y - 1)
                );
            }
        }

        private void LinkSmallTiles()
        {
            foreach (var tile in this.SmallTiles.Values)
            {
                var x = tile.X;
                var y = tile.Y;
                tile.LinkTiles(
                    this.GetSmallTile(x + 1, y - 1),
                    this.GetSmallTile(x + 1, y),
                    this.GetSmallTile(x + 1, y + 1),
                    this.GetSmallTile(x, y + 1),
                    this.GetSmallTile(x - 1, y + 1),
                    this.GetSmallTile(x - 1, y),
                    this.GetSmallTile(x - 1, y - 1),
                    this.GetSmallTile(x, y - 1)
                );
            }
        }

        public void AddRock(SmallTileTemplate tile, ThingType thingType, ItemType resourceType)
        {
            var rock = new RockTemplate(tile, thingType, resourceType);
            this.rocks.Add(rock);

            tile.ThingsPrimary.Add(rock);
            tile.ThingsAll.Add(rock);

            if (thingType == ThingType.RockLarge)
            {
                if (tile.TileToNE != null) tile.TileToNE.ThingsAll.Add(rock);
                if (tile.TileToE != null) tile.TileToE.ThingsAll.Add(rock);
                if (tile.TileToSE != null) tile.TileToSE.ThingsAll.Add(rock);
            }
        }

        public void AddPlant(SmallTileTemplate tile, ThingType thingType)
        {
            var plant = new PlantTemplate(tile, thingType);
            this.plants.Add(plant);

            tile.ThingsPrimary.Add(plant);
            tile.ThingsAll.Add(plant);

            if (thingType == ThingType.Bush)
            {
                if (tile.TileToNE != null) tile.TileToNE.ThingsAll.Add(plant);
                if (tile.TileToE != null) tile.TileToE.ThingsAll.Add(plant);
                if (tile.TileToSE != null) tile.TileToSE.ThingsAll.Add(plant);
            }
        }

        public IReadOnlyCollection<RockTemplate> GetRocks()
        {
            return this.rocks.ToList();
        }

        public IReadOnlyCollection<PlantTemplate> GetPlants()
        {
            return this.plants.ToList();
        }

        public SmallTileTemplate GetSmallTile(int index)
        {
            return index >= 0 && index < this.SmallTiles.Count ? this.SmallTiles[index] : null;
        }

        public SmallTileTemplate GetSmallTile(int x, int y)
        {
            return (x >= 0 && y >= 0 && x < this.size && y < this.size) ? this.SmallTiles[x + (y * this.size)] : null;
        }

        public BigTileTemplate GetBigTile(int x, int y)
        {
            return (x >= 0 && y >= 0 && x < this.bigSize && y < this.bigSize) ? this.BigTiles[x + (y * this.bigSize)] : null;
        }
    }
}
