namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;
    using Shared;

    public interface IBigTile
    {
        IBigTile TileToE { get; }
        IBigTile TileToN { get; }
        IBigTile TileToNE { get; }
        IBigTile TileToNW { get; }
        IBigTile TileToS { get; }
        IBigTile TileToSE { get; }
        IBigTile TileToSW { get; }
        IBigTile TileToW { get; }
        TerrainType TerrainType { get; set; }
        Dictionary<Direction, ISmallTile> SmallTiles { get; }

        string GetTextureName();
        void UpdateCoords();
    }
}