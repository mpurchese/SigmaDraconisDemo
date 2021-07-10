namespace SigmaDraconis.UI.Managers
{
    using Shared;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using World;
    using World.Buildings;
    using WorldInterfaces;

    public static class TileHighlightManager
    {
        private static int? virtualBlueprintId;
        private static int? selectedBuildingId;
        private static int? framesSinceLastCheck;

        // Highlighted tiles
        public static List<TileHighlight> TileHighlightsSteady { get; set; } = new List<TileHighlight>();
        public static List<TileHighlight> TileHighlightsPulsing { get; set; } = new List<TileHighlight>();

        public static void Reset()
        {
            TileHighlightsSteady.Clear();
            TileHighlightsPulsing.Clear();
            virtualBlueprintId = null;
            selectedBuildingId = null;
            framesSinceLastCheck = 0;
        }

        public static void Update()
        {
            var blueprint = World.VirtualBlueprint.FirstOrDefault();
            var building = PlayerWorldInteractionManager.SelectedThing as IThingWithTileHighlights;
            framesSinceLastCheck++;

            if (blueprint?.Id == virtualBlueprintId && building?.Id == selectedBuildingId && (framesSinceLastCheck < 13 || (building == null && blueprint == null))) return;

            virtualBlueprintId = blueprint?.Id;
            selectedBuildingId = building?.Id;
            framesSinceLastCheck = 0;
            var haveHighlights = TileHighlightsSteady.Any() || TileHighlightsPulsing.Any();

            TileHighlightsSteady.Clear();
            TileHighlightsPulsing.Clear();

            if (virtualBlueprintId.HasValue)
            {
                var tile = blueprint.MainTile;
                if (tile.TerrainType == TerrainType.Dirt && tile.ThingsAll.All(t => t.Definition?.BlocksConstruction == false))
                {
                    if (blueprint.ThingType == ThingType.WindTurbine)
                    {
                        // Highlight tiles that will be affected by wind blocking
                        var tx = tile.X;
                        var ty = tile.Y;
                        TileHighlightsSteady.Clear();
                        TileHighlightsPulsing.Clear();

                        for (var x = -2; x <= 2; x++)
                        {
                            for (var y = -2; y <= 2; y++)
                            {
                                var distance = Math.Sqrt((x * x) + (y * y));
                                HighlightWindTurbineBlueprintTile(tx + x, ty + y, distance);
                            }
                        }
                    }
                    else if (blueprint.ThingType == ThingType.OreScanner)
                    {
                        // Highlight tiles that can be scanned
                        var tx = tile.X;
                        var ty = tile.Y;
                        TileHighlightsSteady.Clear();
                        TileHighlightsPulsing.Clear();

                        var scanRadius = OreScanner.MaxRange;
                        for (var x = -scanRadius; x <= scanRadius; x++)
                        {
                            for (var y = -scanRadius; y <= scanRadius; y++)
                            {
                                HighlightOreScannerBlueprintTile(tx + x, ty + y, x, y, scanRadius);
                            }
                        }
                    }
                }
            }
            else if (building != null)
            {
                foreach (var highlight in building.GetTilesToHighlight())
                {
                    if (highlight.IsPulsing) TileHighlightsPulsing.Add(highlight);
                    else TileHighlightsSteady.Add(highlight);
                }
            }

            if (haveHighlights || TileHighlightsPulsing.Any() || TileHighlightsSteady.Any()) EventManager.RaiseEvent(EventType.HighlightedTiles, null);
        }

        private static void HighlightWindTurbineBlueprintTile(int x, int y, double distance)
        {
            var tile = World.GetSmallTile(x, y);
            if (tile == null) return;

            var modifier = Math.Min(128, tile.WindModifier);
            modifier = (int)(modifier + ((Math.Max(distance, 1.0)) - 1) * 64);
            TileHighlightsSteady.Add(new TileHighlight(tile.Index, false, 196 - modifier));
        }

        private static void HighlightOreScannerBlueprintTile(int x, int y, int xRel, int yRel, int radius)
        {
            var tile = World.GetSmallTile(x, y);
            if (tile == null || tile.IsMineResourceVisible || tile.TerrainType != TerrainType.Dirt) return;

            var rx = 10 - Math.Abs(xRel);
            var ry = 10 - Math.Abs(yRel);
            var r = Constants.OreScanRadiusMap[rx, ry];
            if (r == 0 || r > radius) return;

            TileHighlightsPulsing.Add(new TileHighlight(tile.Index, true, 196));
        }
    }
}
