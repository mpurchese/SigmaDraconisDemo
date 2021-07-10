namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;

    public interface IThingHidesTiles : IThing
    {
        IEnumerable<ISmallTile> GetHiddenTiles();
    }
}
