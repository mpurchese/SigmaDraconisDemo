namespace SigmaDraconis.UI.Managers
{
    using Shared;
    using System.Collections.Generic;
    using System.Linq;
    using World;
    using WorldInterfaces;

    public static class SeeThroughTreeManager
    {
        private static int? mouseTileIndex;

        private static HashSet<int> SeeThroughThings { get; set; } = new HashSet<int>();
        private static HashSet<int> SeeThroughThingsReverting { get; set; } = new HashSet<int>();  // Fading back to normal

        public static void Reset()
        {
            SeeThroughThings.Clear();
            SeeThroughThingsReverting.Clear();
            mouseTileIndex = null;
        }

        public static void Update()
        {
            UpdateFades();

            if (MouseWorldPosition.Tile?.Index == mouseTileIndex) return;
            mouseTileIndex = MouseWorldPosition.Tile?.Index;

            var newThings = mouseTileIndex.HasValue
                ? World.GetThings<IThingHidesTiles>(ThingType.Tree).Where(t => !t.IsRecycling && t.GetHiddenTiles().Any(h => h.Index == mouseTileIndex)).ToDictionary(t => t.Id, t => t)
                : new Dictionary<int, IThingHidesTiles>();

            if (MouseWorldPosition.Tile?.TileToS is ISmallTile tile)
            {
                foreach (var thing in tile.ThingsPrimary.OfType<IThingHidesTiles>().Where(t => t.ThingType != ThingType.Tree && !t.IsRecycling && t.GetHiddenTiles().Any(h => h.Index == mouseTileIndex)))
                {
                    newThings.Add(thing.Id, thing);
                }

                if (tile.TileToS is ISmallTile tile2)
                {
                    foreach (var thing in tile2.ThingsPrimary.OfType<IThingHidesTiles>().Where(t => t.ThingType != ThingType.Tree && !t.IsRecycling && t.GetHiddenTiles().Any(h => h.Index == mouseTileIndex)))
                    {
                        if (!newThings.ContainsKey(thing.Id)) newThings.Add(thing.Id, thing);
                    }

                    if (tile2.TileToS is ISmallTile tile3)
                    {
                        foreach (var thing in tile3.ThingsPrimary.OfType<IThingHidesTiles>().Where(t => t.ThingType == ThingType.Grass && !t.IsRecycling && t.GetHiddenTiles().Any(h => h.Index == mouseTileIndex)))
                        {
                            if (!newThings.ContainsKey(thing.Id)) newThings.Add(thing.Id, thing);
                        }
                    }
                }

                if (MouseWorldPosition.Tile.TileToSW is ISmallTile tile4)
                {
                    foreach (var thing in tile4.ThingsPrimary.OfType<IThingHidesTiles>().Where(t => t.ThingType == ThingType.Grass && !t.IsRecycling && t.GetHiddenTiles().Any(h => h.Index == mouseTileIndex)))
                    {
                        if (!newThings.ContainsKey(thing.Id)) newThings.Add(thing.Id, thing);
                    }

                    if (tile4.TileToS is ISmallTile tile5)
                    {
                        foreach (var thing in tile5.ThingsPrimary.OfType<IThingHidesTiles>().Where(t => t.ThingType == ThingType.Grass && !t.IsRecycling && t.GetHiddenTiles().Any(h => h.Index == mouseTileIndex)))
                        {
                            if (!newThings.ContainsKey(thing.Id)) newThings.Add(thing.Id, thing);
                        }
                    }
                }

                if (MouseWorldPosition.Tile.TileToSE is ISmallTile tile6)
                {
                    foreach (var thing in tile6.ThingsPrimary.OfType<IThingHidesTiles>().Where(t => t.ThingType == ThingType.Grass && !t.IsRecycling && t.GetHiddenTiles().Any(h => h.Index == mouseTileIndex)))
                    {
                        if (!newThings.ContainsKey(thing.Id)) newThings.Add(thing.Id, thing);
                    }

                    if (tile6.TileToS is ISmallTile tile7)
                    {
                        foreach (var thing in tile7.ThingsPrimary.OfType<IThingHidesTiles>().Where(t => t.ThingType == ThingType.Grass && !t.IsRecycling && t.GetHiddenTiles().Any(h => h.Index == mouseTileIndex)))
                        {
                            if (!newThings.ContainsKey(thing.Id)) newThings.Add(thing.Id, thing);
                        }
                    }
                }
            }

            foreach (var id in SeeThroughThings.Where(i => !newThings.ContainsKey(i)).ToList())
            {
                var thing = World.GetThing(id);
                SeeThroughThings.Remove(id);
                SeeThroughThingsReverting.Add(id);
            }

            foreach (var t in newThings)
            {
                SeeThroughThings.Add(t.Key);
                SeeThroughThingsReverting.Remove(t.Key);
            }
        }

        private static void UpdateFades()
        {
            foreach (var id in SeeThroughThings.ToList())
            {
                if (!(World.GetThing(id) is IThingHidesTiles thing) || thing.IsRecycling)
                {
                    SeeThroughThings.Remove(id);
                    continue;
                }

                if (thing.RenderAlpha > 0.4f) thing.RenderAlpha -= 0.02f;
            }

            foreach (var id in SeeThroughThingsReverting.ToList())
            {
                if (!(World.GetThing(id) is IThingHidesTiles thing) || thing.IsRecycling)
                {
                    SeeThroughThingsReverting.Remove(id);
                    continue;
                }

                var alpha = thing.RenderAlpha + 0.04f;
                if (alpha >= 1f)
                {
                    SeeThroughThingsReverting.Remove(id);
                    alpha = 1f;
                }

                thing.RenderAlpha = alpha;
            }
        }
    }
}
