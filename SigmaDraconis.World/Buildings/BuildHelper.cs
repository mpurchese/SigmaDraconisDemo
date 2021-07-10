namespace SigmaDraconis.World.Buildings
{
    using Draconis.Shared;
    using System.Linq;
    using Config;
    using Language;
    using Shared;
    using WorldInterfaces;

    public class BuildHelper
    {
        public static int MaxConduitPathLength = 64;
        public static string CanBuildReason = "";

        public static bool CanBuild(ISmallTile tile, ThingType thingType, Direction direction = Direction.None)
        {
            CanBuildReason = "";

            if (tile == null || (tile.TerrainType != TerrainType.Dirt && !thingType.In(ThingType.FuelFactory, ThingType.Wall, ThingType.Door, ThingType.Roof)))
            {
                return false;
            }

            // Fuel factory must be built with two tiles on land and two on coast
            if (thingType == ThingType.FuelFactory &&
                !((tile.TerrainType == TerrainType.Dirt && tile.TileToSE?.TerrainType == TerrainType.Dirt && tile.TileToNE?.TerrainType == TerrainType.Coast && tile.TileToE?.TerrainType == TerrainType.Coast)
                || (tile.TerrainType == TerrainType.Dirt && tile.TileToSE?.TerrainType == TerrainType.Coast && tile.TileToNE?.TerrainType == TerrainType.Dirt && tile.TileToE?.TerrainType == TerrainType.Coast)
                || (tile.TerrainType == TerrainType.Coast && tile.TileToSE?.TerrainType == TerrainType.Dirt && tile.TileToNE?.TerrainType == TerrainType.Coast && tile.TileToE?.TerrainType == TerrainType.Dirt)
                || (tile.TerrainType == TerrainType.Coast && tile.TileToSE?.TerrainType == TerrainType.Coast && tile.TileToNE?.TerrainType == TerrainType.Dirt && tile.TileToE?.TerrainType == TerrainType.Dirt)))
            {
                return false;
            }

            if (thingType == ThingType.RocketGantry)
            {
                return tile.ThingsPrimary.Any(t => (t as LaunchPad)?.IsReady == true);
            }

            if (thingType == ThingType.Rocket)
            {
                return tile.ThingsPrimary.Any(t => (t as RocketGantry)?.IsReady == true);
            }

            var definition = ThingTypeManager.GetDefinition(thingType);
            if (definition == null) return false;

            if (!definition.IsAdjacentBuildAllowed && World.GetThings<IBuildableThing>(thingType).Any(t => tile.AdjacentTiles8.Intersect(t.AllTiles).Any()))
            {
                CanBuildReason = GetString(StringsForMouseCursor.TooCloseToAnother, LanguageManager.GetNameLower(thingType));
                return false;
            }

            if (!definition.IsBuildByCoastAllowed && tile.AdjacentTiles8.Any(t => t.TerrainType == TerrainType.Coast))
            {
                CanBuildReason = GetString(StringsForMouseCursor.CantBuildByCoast, LanguageManager.GetNameLower(thingType));
                return false;
            }

            if (thingType == ThingType.Wall || thingType == ThingType.Door)
            {
                var canBuild = CanBuildWallInTile(tile, thingType, direction, out string reason);
                CanBuildReason = reason;
                return canBuild;
            }

            var range = (definition.Size.X - 1) / 2;
            var o = (definition.Size.X - 1) % 2;
            var foundations = 0;
            var coast = 0;
            if (thingType == ThingType.ConduitNode)
            {
                // Conduit nodes only blocked by other plants, rocks, and other conduit nodes
                if (tile.ThingsAll.Any(t => t is IConduitNode || (t.Definition?.BlocksConstruction == true && !(t is IBuildableThing) && !(t is IAnimal) && !(t is IStackingArea) && !(t is IResourceStack))))
                {
                    return false;
                }

                CanBuildReason = "";
                return true;
            }
            else if (definition.IsLaunchPadRequired)
            {
                CanBuildReason = tile.ThingsPrimary.OfType<IBuildableThing>().Any(t => t.ThingType == ThingType.LaunchPad && t.IsReady && !t.IsDesignatedForRecycling)
                    && tile.ThingsPrimary.All(t => t.ThingType != ThingType.RocketGantry)
                    ? ""
                    : GetString(StringsForMouseCursor.BuildOnLaunchPad);
                return CanBuildReason == "";
            }
            else if (definition.IsRocketGantryRequired)
            {
                CanBuildReason = tile.ThingsPrimary.OfType<IBuildableThing>().Any(t => t.ThingType == ThingType.RocketGantry && t.IsReady && !t.IsDesignatedForRecycling)
                    && tile.ThingsPrimary.All(t => t.ThingType != ThingType.Rocket)
                    ? ""
                    : GetString(StringsForMouseCursor.BuildOnLaunchPad);
                return CanBuildReason == "";
            }
            else
            {
                for (int x = tile.X - range; x <= tile.X + range + o; x++)
                {
                    for (int y = tile.Y - range; y <= tile.Y + range + o; y++)
                    {
                        var t2 = World.GetSmallTile(x, y);
                        if (t2.ThingsAll.Any(t => (t.Definition?.BlocksConstruction == true || t.ThingType == ThingType.Roof)
                            && !(t.Definition?.OptionalFoundation == true && thingType.IsFoundation())      // Can build foundation under a dispenser, dropoff etc.
                            && !(thingType.IsFoundation() && t.ThingType.IsFoundation())                     // Anything can go on a foundation, except another foundation
                            && !(definition.CanBeIndoor && t.ThingType == ThingType.Roof)                                      // Roofs don't block things that can be built indoors.
                            && (thingType != ThingType.ConduitNode || !(t is IAnimal))))                                        // Network nodes can be built even if there is an animal in the tile            
                        {
                            // Something in the way
                            return false;
                        }

                        if (t2.ThingsPrimary.OfType<IBuildableThing>().Any(t => !t.IsDesignatedForRecycling && t.ThingType.IsFoundation()
                            && (t.IsReady || thingType != ThingType.LaunchPad))) // Launch pad needs *completed* foundation, otherwise launchpad blueprint will block access to the conduit blueprints
                        {
                            foundations++;
                        }

                        if (t2.TerrainType == TerrainType.Coast)
                        {
                            coast++;
                        }
                    }
                }
            }

            if (definition.CoastTilesRequired > coast)
            {
                CanBuildReason = GetString(StringsForMouseCursor.RequiresCoast);
            }
            else if (definition.AdjacentCoastTilesRequired > 0 && !IsByWater(tile))
            {
                CanBuildReason = GetString(StringsForMouseCursor.BuildNextToWater);
            }
            else if (definition.FoundationsRequired > foundations)
            {
                CanBuildReason = definition.FoundationsRequired > 1 
                    ? GetString(StringsForMouseCursor.RequiresFoundationX, definition.FoundationsRequired) 
                    : GetString(StringsForMouseCursor.RequiresFoundation);
            }
            else if (thingType == ThingType.AlgaePool && definition.BuildingLayer == BuildingLayer.Floor && foundations > 0)
            {
                CanBuildReason = GetString(StringsForMouseCursor.CantBuildOnFoundation);
            }
            else if (thingType == ThingType.AlgaePool && foundations > 0 && foundations < definition.Size.X * definition.Size.Y)
            {
                CanBuildReason = GetString(StringsForMouseCursor.CantBuildOnPartialFoundation);
            }

            return CanBuildReason == "";
        }

        public static bool BuildingExists(ISmallTile tile, ThingType thingType, Direction directon = Direction.None)
        {
            if (tile == null)
            {
                return false;
            }

            return BuildingExistsInner(tile, thingType, directon);
        }

        private static bool BuildingExistsInner(ISmallTile tile, ThingType thingType, Direction directon)
        {
            if (thingType.In(ThingType.Wall, ThingType.Door) && directon != Direction.None)
            {
                // Two walls or doors can be built in the same tile, if they are in different directions
                return tile.ThingsAll.Any(t => t.ThingType == thingType && t.MainTileIndex == tile.Index && (t as IRotatableThing)?.Direction == directon)
                    || World.ConfirmedBlueprints.Values.Any(t => t.ThingType == thingType && t.MainTile == tile && t.Direction == directon);
            }

            return tile.ThingsAll.Any(t => t.ThingType == thingType && t.MainTileIndex == tile.Index)
                || World.ConfirmedBlueprints.Values.Any(t => t.ThingType == thingType && t.MainTile == tile);
        }

        private static bool CanBuildWallInTile(ISmallTile tile, ThingType type, Direction direction, out string reason)
        {
            reason = "";

            var tile2 = tile.GetTileToDirection(direction);
            if (tile2 == null) return false;

            var hasFoundation = tile.ThingsAll.Any(t => t.ThingType.IsFoundation() && (t as IBuildableThing)?.IsDesignatedForRecycling == false)
                || tile2.ThingsAll.Any(t => t.ThingType.IsFoundation() && (t as IBuildableThing)?.IsDesignatedForRecycling == false);

            // Egdes of algae pool also count as foundation
            if (!hasFoundation && tile.ThingsAll.FirstOrDefault(t => t.ThingType == ThingType.AlgaePool) is IAlgaePool ap && tile2.ThingsAll.All(t => t.ThingType != ThingType.AlgaePool || t != ap)) hasFoundation = true;
            if (!hasFoundation && tile2.ThingsAll.FirstOrDefault(t => t.ThingType == ThingType.AlgaePool) is IAlgaePool ap2 && tile.ThingsAll.All(t => t.ThingType != ThingType.AlgaePool || t != ap2)) hasFoundation = true;

            if (!hasFoundation) reason = GetString(StringsForMouseCursor.RequiresFoundation);

            return hasFoundation && tile.ThingsPrimary.All(t => t.ThingType != type || (t as IRotatableThing)?.Direction != direction);
        }

        private static bool IsByWater(ISmallTile tile)
        {
            for (int i = 4; i < 8; i++)
            {
                var t2 = tile.GetTileToDirection((Direction)i);
                if (t2 == null) continue;

                var t3 = t2.GetTileToDirection((Direction)i);
                if (t3?.TerrainType == TerrainType.Water && t2.TerrainType == TerrainType.Coast) return true;
            }

            return false;
        }


        private static string GetString(StringsForMouseCursor key)
        {
            return LanguageManager.Get<StringsForMouseCursor>(key);
        }

        private static string GetString(StringsForMouseCursor key, object arg0)
        {
            return LanguageManager.Get<StringsForMouseCursor>(key, arg0);
        }
    }
}
