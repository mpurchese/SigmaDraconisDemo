namespace SigmaDraconis.WorldGenerator
{
    using System.Collections.Generic;
    using Shared;
    using World.Terrain;

    public class SmallTileTemplate
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Index { get; private set; }

        public List<IThingTemplate> ThingsPrimary { get; } = new List<IThingTemplate>();
        public List<IThingTemplate> ThingsAll { get; } = new List<IThingTemplate>();

        // Soil types control plant growth areas
        public BiomeType BiomeType { get; set; }
        public int SoilTypeCount { get; set; }

        private MineTileResource mineResource;
        public int MineResourceCount => this.mineResource != null && this.mineResource.Type != ItemType.None ? this.mineResource.Count : 0;
        public MineResourceDensity MineResourceDensity => this.mineResource != null && this.mineResource.Type != ItemType.None ? this.mineResource.Density : MineResourceDensity.None;
        public ItemType MineResourceType => this.mineResource != null ? this.mineResource.Type : ItemType.None;

        // Ground cover
        public int GroundCoverDensity { get; set; }
        public int GroundCoverMaxDensity { get; set; }
        public Direction GroundCoverDirection { get; set; } = (Direction)Rand.Next(4) + 4;

        public BigTileTemplate BigTile { get; set; }

        public SmallTileTemplate TileToN { get; private set; }
        public SmallTileTemplate TileToNE { get; private set; }
        public SmallTileTemplate TileToE { get; private set; }
        public SmallTileTemplate TileToSE { get; private set; }
        public SmallTileTemplate TileToS { get; private set; }
        public SmallTileTemplate TileToSW { get; private set; }
        public SmallTileTemplate TileToW { get; private set; }
        public SmallTileTemplate TileToNW { get; private set; }

        public TerrainType TerrainType { get; set; }

        public List<SmallTileTemplate> AdjacentTiles4
        {
            get
            {
                var result = new List<SmallTileTemplate>(4);
                if (this.TileToNE != null) result.Add(this.TileToNE);
                if (this.TileToSE != null) result.Add(this.TileToSE);
                if (this.TileToSW != null) result.Add(this.TileToSW);
                if (this.TileToNW != null) result.Add(this.TileToNW);
                return result;
            }
        }

        public List<SmallTileTemplate> AdjacentTiles8
        {
            get
            {
                var result = new List<SmallTileTemplate>(8);
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

        public SmallTileTemplate(BigTileTemplate bigTile, int index, int x, int y)
        {
            this.BigTile = bigTile;
            this.X = x;
            this.Y = y;
            this.Index = index;
        }

        public void LinkTiles(SmallTileTemplate n, SmallTileTemplate ne, SmallTileTemplate e, SmallTileTemplate se, SmallTileTemplate s, SmallTileTemplate sw, SmallTileTemplate w, SmallTileTemplate nw)
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

        public MineTileResource GetResources()
        {
            return this.mineResource != null && (this.mineResource.Type != ItemType.None || this.mineResource.IsVisible) ? this.mineResource.Clone() as MineTileResource : null;
        }

        public void SetResources(ItemType type, int count, MineResourceDensity density)
        {
            this.mineResource = new MineTileResource
            {
                Count = count,
                Density = density,
                Type = type
            };
        }
    }
}
