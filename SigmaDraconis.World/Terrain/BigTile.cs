namespace SigmaDraconis.World.Terrain
{
    using Draconis.Shared;
    using Shared;
    using WorldInterfaces;
    using System;
    using System.Collections.Generic;

    public class BigTile : IBigTile
    {
        public IBigTile TileToN { get; private set; }
        public IBigTile TileToNE { get; private set; }
        public IBigTile TileToE { get; private set; }
        public IBigTile TileToSE { get; private set; }
        public IBigTile TileToS { get; private set; }
        public IBigTile TileToSW { get; private set; }
        public IBigTile TileToW { get; private set; }
        public IBigTile TileToNW { get; private set; }

        public BigTile(int index, int x, int y, int row)
        {
            this.TerrainPosition = new Vector2i(x, y);
            this.TerrainRow = row;
            this.Index = index;

            this.SmallTiles = new Dictionary<Direction, ISmallTile>
            {
                { Direction.None, null },
                { Direction.N, null },
                { Direction.NE, null },
                { Direction.E, null },
                { Direction.SE, null },
                { Direction.S, null },
                { Direction.SW, null },
                { Direction.W, null },
                { Direction.NW, null }
            };
        }

        public override string ToString()
        {
            return $"{this.TerrainX}, {this.TerrainY}";
        }

        public int Index { get; private set; }

        public Vector2i TerrainPosition { get; }

        public int TerrainX => this.TerrainPosition.X;
        public int TerrainY => this.TerrainPosition.Y;

        public int TerrainRow { get; private set; }

        public TerrainType TerrainType { get; set; }

        public BigTileTextureIdentifier BigTileTextureIdentifier { get; set; }

        public Dictionary<Direction, ISmallTile> SmallTiles { get; private set; }

        public Vector2i CentrePosition { get; private set; }

        public void UpdateCoords()
        {
            var x = (32 * this.TerrainPosition.X) + (32 * this.TerrainPosition.Y);
            var y = (16 * this.TerrainPosition.Y) - (16 * this.TerrainPosition.X);
            this.CentrePosition = new Vector2i(x + 32, y + 16);
        }

        public void LinkTiles(BigTile n, BigTile ne, BigTile e, BigTile se, BigTile s, BigTile sw, BigTile w, BigTile nw)
        {
            this.TileToN = n;
            this.TileToNE = ne;
            this.TileToE = e;
            this.TileToSE = se;
            this.TileToS = s;
            this.TileToSW = sw;
            this.TileToW = w;
            this.TileToNW = nw;
        }

        public void UpdateSmallTileTerrainTypes()
        {
            foreach (var smallTile in this.SmallTiles.Values)
            {
                smallTile.TerrainType = this.TerrainType == TerrainType.Coast ? TerrainType.Dirt : this.TerrainType;
            }

            if (this.TerrainType != TerrainType.Coast) return;

            // Small tile terrain types for coastal tiles
            this.SmallTiles[Direction.None].TerrainType = TerrainType.Coast;
            switch (this.BigTileTextureIdentifier)
            {
                case BigTileTextureIdentifier.CoastSE1:
                case BigTileTextureIdentifier.CoastSE2:
                case BigTileTextureIdentifier.CoastSE3:
                case BigTileTextureIdentifier.CoastSE4:
                    this.SmallTiles[Direction.NE].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.E].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SE].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.S].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SW].TerrainType = TerrainType.Coast;
                    break;
                case BigTileTextureIdentifier.CoastSW1:
                case BigTileTextureIdentifier.CoastSW2:
                case BigTileTextureIdentifier.CoastSW3:
                case BigTileTextureIdentifier.CoastSW4:
                    this.SmallTiles[Direction.NW].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.W].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SW].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.S].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SE].TerrainType = TerrainType.Coast;
                    break;
                case BigTileTextureIdentifier.CoastNE1:
                case BigTileTextureIdentifier.CoastNE2:
                case BigTileTextureIdentifier.CoastNE3:
                case BigTileTextureIdentifier.CoastNE4:
                    this.SmallTiles[Direction.NW].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.N].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.NE].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.E].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SE].TerrainType = TerrainType.Coast;
                    break;
                case BigTileTextureIdentifier.CoastNW1:
                case BigTileTextureIdentifier.CoastNW2:
                case BigTileTextureIdentifier.CoastNW3:
                case BigTileTextureIdentifier.CoastNW4:
                    this.SmallTiles[Direction.NE].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.N].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.NW].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.W].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SW].TerrainType = TerrainType.Coast;
                    break;
                case BigTileTextureIdentifier.CoastN1:
                case BigTileTextureIdentifier.CoastN2:
                case BigTileTextureIdentifier.CoastN3:
                case BigTileTextureIdentifier.CoastN4:
                    this.SmallTiles[Direction.SE].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.E].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.NE].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.N].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.NW].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.W].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SW].TerrainType = TerrainType.Coast;
                    break;
                case BigTileTextureIdentifier.CoastN5:
                case BigTileTextureIdentifier.CoastN6:
                case BigTileTextureIdentifier.CoastN7:
                case BigTileTextureIdentifier.CoastN8:
                    this.SmallTiles[Direction.N].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.NE].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.NW].TerrainType = TerrainType.Coast;
                    break;
                case BigTileTextureIdentifier.CoastE1:
                case BigTileTextureIdentifier.CoastE2:
                case BigTileTextureIdentifier.CoastE3:
                case BigTileTextureIdentifier.CoastE4:
                    this.SmallTiles[Direction.SW].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.S].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SE].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.E].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.NE].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.N].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.NW].TerrainType = TerrainType.Coast;
                    break;
                case BigTileTextureIdentifier.CoastE5:
                case BigTileTextureIdentifier.CoastE6:
                case BigTileTextureIdentifier.CoastE7:
                case BigTileTextureIdentifier.CoastE8:
                    this.SmallTiles[Direction.E].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SE].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.NE].TerrainType = TerrainType.Coast;
                    break;
                case BigTileTextureIdentifier.CoastS1:
                case BigTileTextureIdentifier.CoastS2:
                case BigTileTextureIdentifier.CoastS3:
                case BigTileTextureIdentifier.CoastS4:
                    this.SmallTiles[Direction.NW].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.W].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SW].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.S].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SE].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.E].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.NE].TerrainType = TerrainType.Coast;
                    break;
                case BigTileTextureIdentifier.CoastS5:
                case BigTileTextureIdentifier.CoastS6:
                case BigTileTextureIdentifier.CoastS7:
                case BigTileTextureIdentifier.CoastS8:
                    this.SmallTiles[Direction.S].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SE].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.SW].TerrainType = TerrainType.Coast;
                    break;
                case BigTileTextureIdentifier.CoastW1:
                case BigTileTextureIdentifier.CoastW2:
                case BigTileTextureIdentifier.CoastW3:
                case BigTileTextureIdentifier.CoastW4:
                    this.SmallTiles[Direction.NE].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.N].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.NW].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.W].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SW].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.S].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SE].TerrainType = TerrainType.Coast;
                    break;
                case BigTileTextureIdentifier.CoastW5:
                case BigTileTextureIdentifier.CoastW6:
                case BigTileTextureIdentifier.CoastW7:
                case BigTileTextureIdentifier.CoastW8:
                    this.SmallTiles[Direction.W].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.SW].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.NW].TerrainType = TerrainType.Coast;
                    break;
                case BigTileTextureIdentifier.ChannelEW:
                    this.SmallTiles[Direction.NE].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.NW].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.SE].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.SW].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.E].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.W].TerrainType = TerrainType.Water;
                    break;
                case BigTileTextureIdentifier.ChannelNS:
                    this.SmallTiles[Direction.NE].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.NW].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.SE].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.SW].TerrainType = TerrainType.Coast;
                    this.SmallTiles[Direction.N].TerrainType = TerrainType.Water;
                    this.SmallTiles[Direction.S].TerrainType = TerrainType.Water;
                    break;
                default:
                    // Temporary default
                    foreach (var smallTile in this.SmallTiles.Values) smallTile.TerrainType = TerrainType.Water;
                    break;
            }
        }

        public string GetTextureName()
        {
            return Enum.GetName(typeof(BigTileTextureIdentifier), this.BigTileTextureIdentifier);
        }
    }

    public enum BigTileTextureIdentifier
    {
        None = 0,
        Flat1 = 1,
        Flat2 = 2,
        Flat3 = 3,
        Flat4 = 4,
        CoastSW1 = 5,
        CoastSW2 = 6,
        CoastSW3 = 7,
        CoastSW4 = 8,
        CoastSE1 = 9,
        CoastSE2 = 10,
        CoastSE3 = 11,
        CoastSE4 = 12,
        CoastNE1 = 13,
        CoastNE2 = 14,
        CoastNE3 = 15,
        CoastNE4 = 16,
        CoastNW1 = 17,
        CoastNW2 = 18,
        CoastNW3 = 19,
        CoastNW4 = 20,
        CoastN1 = 21,
        CoastN2 = 22,
        CoastN3 = 23,
        CoastN4 = 24,
        CoastN5 = 25,
        CoastN6 = 26,
        CoastN7 = 27,
        CoastN8 = 28,
        CoastE1 = 29,
        CoastE2 = 30,
        CoastE3 = 31,
        CoastE4 = 32,
        CoastE5 = 33,
        CoastE6 = 34,
        CoastE7 = 35,
        CoastE8 = 36,
        CoastS1 = 37,
        CoastS2 = 38,
        CoastS3 = 39,
        CoastS4 = 40,
        CoastS5 = 41,
        CoastS6 = 42,
        CoastS7 = 43,
        CoastS8 = 44,
        CoastW1 = 45,
        CoastW2 = 46,
        CoastW3 = 47,
        CoastW4 = 48,
        CoastW5 = 49,
        CoastW6 = 50,
        CoastW7 = 51,
        CoastW8 = 52,
        ChannelEW = 53,
        ChannelNS = 54,
        Water = 55,
        DeepSW1 = 56,
        DeepSW2 = 57,
        DeepSW3 = 58,
        DeepSW4 = 59,
        DeepSE1 = 60,
        DeepSE2 = 61,
        DeepSE3 = 62,
        DeepSE4 = 63,
        DeepNE1 = 64,
        DeepNE2 = 65,
        DeepNE3 = 66,
        DeepNE4 = 67,
        DeepNW1 = 68,
        DeepNW2 = 69,
        DeepNW3 = 70,
        DeepNW4 = 71,
        DeepN1 = 72,
        DeepN2 = 73,
        DeepN3 = 74,
        DeepN4 = 75,
        DeepN5 = 76,
        DeepN6 = 77,
        DeepN7 = 78,
        DeepN8 = 79,
        DeepE1 = 80,
        DeepE2 = 81,
        DeepE3 = 82,
        DeepE4 = 83,
        DeepE5 = 84,
        DeepE6 = 85,
        DeepE7 = 86,
        DeepE8 = 87,
        DeepS1 = 88,
        DeepS2 = 89,
        DeepS3 = 90,
        DeepS4 = 91,
        DeepS5 = 92,
        DeepS6 = 93,
        DeepS7 = 94,
        DeepS8 = 95,
        DeepW1 = 96,
        DeepW2 = 97,
        DeepW3 = 98,
        DeepW4 = 99,
        DeepW5 = 100,
        DeepW6 = 101,
        DeepW7 = 102,
        DeepW8 = 103,
        DeepChannelEW = 104,
        DeepChannelNS = 105,
        Deep = 106
    }
}
