namespace SigmaDraconis.WorldControllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Shared;
    using World;
    using WorldInterfaces;

    public static class ResourceMapController
    {
        //public static HashSet<int> MappedTiles { get; set; }
        public static bool RendererUpdateFlag { get; set; }

        public static void Init()
        {
            //MappedTiles = new HashSet<int>();
            //foreach (var tile in World.SmallTiles.Where(t => t.TerrainType == TerrainType.Dirt))
            //{
            //    MappedTiles.Add(tile.Index);
            //}

            RendererUpdateFlag = true;
        }
    }
}
