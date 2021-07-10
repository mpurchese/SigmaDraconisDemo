namespace SigmaDraconis.WorldGenerator
{
    using System.Collections.Generic;
    using Draconis.Shared;
    using Shared;
    using World.Terrain;

    public class BigTileTemplate
    {
        public int Index { get; private set; }
        public Vector2i TerrainPosition { get; }

        public int TerrainX => this.TerrainPosition.X;
        public int TerrainY => this.TerrainPosition.Y;

        public BigTileTemplate TileToN { get; private set; }
        public BigTileTemplate TileToNE { get; private set; }
        public BigTileTemplate TileToE { get; private set; }
        public BigTileTemplate TileToSE { get; private set; }
        public BigTileTemplate TileToS { get; private set; }
        public BigTileTemplate TileToSW { get; private set; }
        public BigTileTemplate TileToW { get; private set; }
        public BigTileTemplate TileToNW { get; private set; }

        public BigTileTextureIdentifier BigTileTextureIdentifier { get; set; }
        public TerrainType TerrainType { get; set; }

        public Dictionary<Direction, SmallTileTemplate> SmallTiles { get; private set; }

        public List<BigTileTemplate> AdjacentTiles8
        {
            get
            {
                var result = new List<BigTileTemplate>(8);
                if (this.TileToNE != null) result.Add(this.TileToNE);
                if (this.TileToSE != null) result.Add(this.TileToSE);
                if (this.TileToSW != null) result.Add(this.TileToSW);
                if (this.TileToNW != null) result.Add(this.TileToNW);
                if (this.TileToN != null) result.Add(this.TileToN);
                if (this.TileToE != null) result.Add(this.TileToE);
                if (this.TileToS != null) result.Add(this.TileToS);
                if (this.TileToW != null) result.Add(this.TileToW);
                return result;
            }
        }


        public BigTileTemplate(int index, int x, int y)
        {
            this.TerrainPosition = new Vector2i(x, y);
            this.Index = index;

            this.SmallTiles = new Dictionary<Direction, SmallTileTemplate>
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

        public void LinkTiles(BigTileTemplate n, BigTileTemplate ne, BigTileTemplate e, BigTileTemplate se, BigTileTemplate s, BigTileTemplate sw, BigTileTemplate w, BigTileTemplate nw)
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
    }
}
