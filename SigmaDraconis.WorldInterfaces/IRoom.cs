using System.Collections.Generic;

namespace SigmaDraconis.WorldInterfaces
{
    public interface IRoom
    {
        bool IsComplete { get; }
        float Light { get; set; }
        float ArtificialLight { get; set; }
        double Temperature { get; set; }
        double HeatLossRate { get; }
        List<int> RoofIDs { get; set; }
        List<int> TileIDs { get; }
        List<IBuildableThing> Roofs { get; set; }
        List<ISmallTile> Tiles { get; }

        void SetTiles(IEnumerable<ISmallTile> tiles);
        void AddTiles(IEnumerable<ISmallTile> tiles);
    }
}