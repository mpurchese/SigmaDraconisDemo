namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;
    using Shared;

    public interface IThingWithTileHighlights : IThing
    {
        IEnumerable<TileHighlight> GetTilesToHighlight();
    }
}
