namespace SigmaDraconis.UI
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;

    using Draconis.Shared;
    using Draconis.UI;
    using Config;
    using Managers;
    using Language;
    using Settings;
    using Shared;

    using World;
    using World.Blueprints;
    using World.Buildings;
    using WorldInterfaces;
    using WorldControllers;

    public static class PlayerActivityBuild
    {
        public static ISmallTile BlueprintTargetTile { get; set; }
        public static ThingType? CurrentThingTypeToBuild { get; set; }
        public static Direction CurrentDirectionToBuild { get; set; } = Direction.SE;
        public static ISmallTile MouseDragStartTile = null;

        private static int currentLanguageId;
        private static readonly Dictionary<ItemType, string> cursorResourceCostStringTemplates = new Dictionary<ItemType, string>();

        public static void Update()
        {
            // For conduits:
            // If connected to network, then add minimum number of conduits needed to connect to the current network.  Otherwise just add one plus connections.

            // For other buildings:
            // If connected to network, the as for conduits then add building
            // Otherwise only add conduits if adjacent tile has a conduit or a building

            var prevVirtualBlueprints = Mouse.GetState().LeftButton == ButtonState.Pressed ? World.VirtualBlueprint.ToList() : new List<Blueprint>();
            var lander = World.GetThings(ThingType.Lander).FirstOrDefault();

            BuildHelper.CanBuildReason = "";

            if (currentLanguageId != UIStatics.CurrentLanguageId)
            {
                currentLanguageId = UIStatics.CurrentLanguageId;
                cursorResourceCostStringTemplates.Clear();
            }

            if (!cursorResourceCostStringTemplates.Any()) PopulateCursorResourceCostStringTemplates();

            // Cursor text
            var texts = new List<string>();
            var colours = new List<Color>();

            if (lander == null || !GameScreen.Instance.IsMouseOver)
            {
                if (UIStatics.CurrentMouseState.LeftButton == ButtonState.Released) BlueprintController.ClearVirtualBlueprint();
                return;
            }

            BlueprintController.ClearVirtualBlueprint();
            var canBuild = false;

            if (MouseWorldPosition.Tile != null && CurrentThingTypeToBuild != null && CurrentThingTypeToBuild != ThingType.None)
            {
                var definition = ThingTypeManager.GetDefinition(CurrentThingTypeToBuild.Value);
                BlueprintTargetTile = definition.Size.X == 2 ? MouseWorldPosition.Tile.TileToSW : MouseWorldPosition.Tile;
                if (BlueprintTargetTile == null) return;

                var network = World.ResourceNetwork;
                var onNetwork = false;

                if (network != null && CurrentThingTypeToBuild == ThingType.Wall && prevVirtualBlueprints.Any())
                {
                    var pv0 = prevVirtualBlueprints[0];
                    onNetwork = World.BuildableTiles.Contains(pv0.MainTileIndex) || World.BuildableTiles.Contains(pv0.MainTile.GetTileToDirection(pv0.Direction)?.Index ?? 0);
                }
                else if (network != null)
                {
                    var range = (definition.Size.X - 1) / 2;
                    var o = (definition.Size.X - 1) % 2;
                    onNetwork = true;
                    for (int x = BlueprintTargetTile.X - range; x <= BlueprintTargetTile.X + range + o; x++)
                    {
                        for (int y = BlueprintTargetTile.Y - range; y <= BlueprintTargetTile.Y + range + o; y++)
                        {
                            var t2 = World.GetSmallTile(x, y);
                            if (!World.BuildableTiles.Contains(t2.Index) && (CurrentThingTypeToBuild != ThingType.FuelFactory || t2.TerrainType != TerrainType.Coast)) onNetwork = false;
                        }
                    }
                }

                // Rotate wall or door depending on where the mouse is in the tile
                if (CurrentThingTypeToBuild.In(ThingType.Wall, ThingType.Door))
                {
                    CurrentDirectionToBuild = MouseWorldPosition.ClosestEdge;
                    if (CurrentDirectionToBuild == Direction.NW)
                    {
                        if (BlueprintTargetTile.GetTileToDirection(Direction.NW) != null) BlueprintTargetTile = BlueprintTargetTile.GetTileToDirection(Direction.NW);
                        CurrentDirectionToBuild = Direction.SE;
                    }
                    else if (CurrentDirectionToBuild == Direction.NE)
                    {
                        if (BlueprintTargetTile.GetTileToDirection(Direction.NE) != null) BlueprintTargetTile = BlueprintTargetTile.GetTileToDirection(Direction.NE);
                        CurrentDirectionToBuild = Direction.SW;
                    }

                    // Walls and doors can connect to the network on either side
                    if (!onNetwork)
                    {
                        var t2 = BlueprintTargetTile.GetTileToDirection(CurrentDirectionToBuild);
                        if (network.ContainsNode(BlueprintTargetTile.Index) || (t2 != null && network.ContainsNode(t2.Index))) onNetwork = true;
                    }
                }

                canBuild = onNetwork || (network != null && CurrentThingTypeToBuild == ThingType.Roof);

                if (CurrentThingTypeToBuild != ThingType.Roof && (!CurrentThingTypeToBuild.In(ThingType.Wall, ThingType.FoundationMetal, ThingType.FoundationStone) || MouseDragStartTile == null || MouseDragStartTile == BlueprintTargetTile))
                {
                    canBuild &= BuildHelper.CanBuild(BlueprintTargetTile, CurrentThingTypeToBuild.GetValueOrDefault(), CurrentDirectionToBuild);
                }

                if (CurrentThingTypeToBuild == ThingType.Roof)
                {
                    if (!canBuild)
                    {
                        BlueprintController.AddVirtualBuilding(BlueprintTargetTile, ThingType.Roof, false);
                    }
                    else
                    {
                        var roofTiles = CreateVirtualRoof(BlueprintTargetTile);
                        canBuild &= roofTiles.Any();

                        if (canBuild && roofTiles.Count <= Constants.MaxTilesPerRoom)
                        {
                            var roomTiles = roofTiles.ToList();
                            foreach (var tile in roofTiles)
                            {
                                if (tile.TileToNW != null) roomTiles.AddIfNew(tile.TileToNW);
                                if (tile.TileToN != null) roomTiles.AddIfNew(tile.TileToN);
                                if (tile.TileToNE != null) roomTiles.AddIfNew(tile.TileToNE);
                            }

                            if (roofTiles.Any(a => a.ThingsPrimary.Any(t => t.ThingType == ThingType.Roof)))
                            {
                                // Existing roof in the way
                                roofTiles.Clear();
                                canBuild = false;
                                BlueprintController.AddVirtualBuilding(BlueprintTargetTile, ThingType.Roof, false);
                            }
                            else
                            {
                                foreach (var tile in roofTiles)
                                {
                                    BlueprintController.AddVirtualBuilding(tile, ThingType.Roof, true);
                                }

                                //texts.Add($"Stone cost  : {roofTiles.Count}");
                                //texts.Add($"Energy cost : {roofTiles.Count * 0.1:N1} kWh");
                                //colours.Add(UIColour.GreenText);
                                //colours.Add(UIColour.GreenText);
                            }
                        }
                        else
                        {
                            roofTiles.Clear();
                            canBuild = false;
                            BlueprintController.AddVirtualBuilding(BlueprintTargetTile, ThingType.Roof, false);
                        }
                    }
                }
                else
                {
                    var animationFrame = 1;
                    if (CurrentThingTypeToBuild == ThingType.FuelFactory)
                    {
                        CurrentDirectionToBuild = Direction.NE;
                        if (BlueprintTargetTile.TerrainType == TerrainType.Dirt && BlueprintTargetTile.TileToNE?.TerrainType == TerrainType.Dirt && BlueprintTargetTile.TileToSE?.TerrainType == TerrainType.Coast && BlueprintTargetTile.TileToE?.TerrainType == TerrainType.Coast) CurrentDirectionToBuild = Direction.SE;
                        else if (BlueprintTargetTile.TileToNE?.TerrainType == TerrainType.Dirt && BlueprintTargetTile.TileToE?.TerrainType == TerrainType.Dirt && BlueprintTargetTile.TerrainType == TerrainType.Coast && BlueprintTargetTile.TileToSE?.TerrainType == TerrainType.Coast) CurrentDirectionToBuild = Direction.SW;
                        else if (BlueprintTargetTile.TileToSE?.TerrainType == TerrainType.Dirt && BlueprintTargetTile.TileToE?.TerrainType == TerrainType.Dirt && BlueprintTargetTile.TerrainType == TerrainType.Coast && BlueprintTargetTile.TileToNE?.TerrainType == TerrainType.Coast) CurrentDirectionToBuild = Direction.NW;
                    }
                    else if (CurrentThingTypeToBuild == ThingType.ShorePump)
                    {
                        // Auto-rotate pump
                        var direction = CurrentDirectionToBuild;
                        var byWater = false;
                        for (int i = 0; i < 4; i++)
                        {
                            var t2 = BlueprintTargetTile.GetTileToDirection(direction);
                            var t3 = t2?.GetTileToDirection(direction);
                            if (t2?.TerrainType == TerrainType.Coast && t3?.TerrainType == TerrainType.Water)
                            {
                                CurrentDirectionToBuild = direction;
                                byWater = true;
                                break;
                            }

                            direction++;
                            if ((int)direction > 7) direction = Direction.NE;
                        }

                        canBuild &= byWater;
                    }

                    var tooBig = false;
                    if (CurrentThingTypeToBuild.GetValueOrDefault().IsFoundation() && MouseDragStartTile != null && MouseDragStartTile != MouseWorldPosition.Tile)
                    {
                        // Drag to cover an area in floor.  Whole area must be in buildable area, but if any tiles already have foundation then we'll fill in the gaps.
                        var x1 = Math.Min(MouseDragStartTile.X, MouseWorldPosition.Tile.X);
                        var y1 = Math.Min(MouseDragStartTile.Y, MouseWorldPosition.Tile.Y);
                        var x2 = Math.Max(MouseDragStartTile.X, MouseWorldPosition.Tile.X);
                        var y2 = Math.Max(MouseDragStartTile.Y, MouseWorldPosition.Tile.Y);
                        if ((Math.Abs(x2 - x1) + 1) * (Math.Abs(y2 - y1) + 1) > 100) tooBig = true;
                        else
                        {
                            for (int y = y1; y <= y2; y++)
                            {
                                for (int x = x1; x <= x2; x++)
                                {
                                    var tile = World.GetSmallTile(x, y);
                                    canBuild &= tile?.TerrainType == TerrainType.Dirt && World.BuildableTiles.Contains(tile.Index);
                                }
                            }

                            for (int y = y1; y <= y2; y++)
                            {
                                for (int x = x1; x <= x2; x++)
                                {
                                    var tile = World.GetSmallTile(x, y);
                                    if (tile != null)
                                    {
                                        if (canBuild)
                                        {
                                            var canBuildInTile = tile.ThingsAll.All(t => t.Definition?.BlocksConstruction != true || t.Definition?.OptionalFoundation == true || t is IMoveableThing);
                                            if (canBuildInTile)
                                            {
                                                BlueprintController.AddVirtualBuilding(tile, CurrentThingTypeToBuild.GetValueOrDefault(), true, animationFrame);
                                            }
                                        }
                                        else BlueprintController.AddVirtualBuilding(tile, CurrentThingTypeToBuild.GetValueOrDefault(), false, animationFrame);
                                    }
                                }
                            }
                        }
                    }
                    else if (CurrentThingTypeToBuild == ThingType.Wall)
                    {
                        // Drag to build a wall
                        canBuild = AddWallBlueprints(prevVirtualBlueprints, canBuild);
                    }
                    else
                    {
                        BlueprintController.AddVirtualBuilding(BlueprintTargetTile, CurrentThingTypeToBuild.GetValueOrDefault(), canBuild, animationFrame, CurrentDirectionToBuild);
                    }

                    if (tooBig)
                    {
                        texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.AreaTooLarge));
                        colours.Add(UIColour.OrangeText);
                    }
                }

                // Check we have enough resources.  If we do, then reserve them.  Otherwise, disable construction.
                if (canBuild)
                {
                    var resourcesNeeded = BlueprintController.GetResourcesRequiredForVirtualBuildings();
                    var orderToCheck = new List<ItemType> { ItemType.Metal, ItemType.Stone, ItemType.BatteryCells, ItemType.Composites, ItemType.SolarCells, ItemType.Glass, ItemType.Compost };
                    var enoughByItemType = orderToCheck.ToDictionary(i => i, i => resourcesNeeded[i] == 0 || network.GetItemTotal(i) >= resourcesNeeded[i]);
                    var enoughResources = enoughByItemType.Values.All(v => v == true);

                    var energyNeeded = BlueprintController.GetEnergyRequiredForVirtualBuildings();
                    var enoughEnergy = network.CanTakeEnergy(energyNeeded);

                    canBuild = enoughResources && enoughEnergy;
                    foreach (var blueprint in World.VirtualBlueprint)
                    {
                        blueprint.CanBuild = canBuild;
                        blueprint.ColourA = canBuild ? 1f : 0.4f;
                    }

                    if (canBuild)
                    {
                        if (CurrentThingTypeToBuild == ThingType.Door && BlueprintTargetTile.ThingsPrimary.Any(t => t.ThingType == ThingType.Wall && t is IWall w && w.IsReady && w.Direction == CurrentDirectionToBuild))
                        {
                            texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.WallDoorConversionCost, energyNeeded.KWh));
                            colours.Add(UIColour.DefaultText);
                        }
                        else if (CurrentThingTypeToBuild == ThingType.Wall && BlueprintTargetTile.ThingsPrimary.Any(t => t is IDoor d && d.IsReady && d.Direction == CurrentDirectionToBuild))
                        {
                            texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.WallDoorConversionCost, energyNeeded.KWh));
                            colours.Add(UIColour.DefaultText);
                        }
                        else if (CurrentThingTypeToBuild.In(ThingType.PlanterStone, ThingType.PlanterHydroponics, ThingType.Cooker) && BlueprintTargetTile.AdjacentTiles4.All(t => !t.CanWalk))
                        {
                            texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.AccessBlocked));
                            colours.Add(UIColour.OrangeText);
                        }
                        else if (CurrentThingTypeToBuild.In(ThingType.Biolab, ThingType.MaterialsLab, ThingType.GeologyLab, ThingType.SleepPod) 
                            && World.VirtualBlueprint.Count == 1 
                            && BlueprintTargetTile.GetTileToDirection(World.VirtualBlueprint[0].Direction)?.CanWalk == false)
                        {
                            texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.AccessBlocked));
                            colours.Add(UIColour.OrangeText);
                        }
                        else if (canBuild)
                        {
                            foreach (var itemType in orderToCheck.Where(i => resourcesNeeded[i] > 0))
                            {
                                texts.Add(string.Format(cursorResourceCostStringTemplates[itemType], resourcesNeeded[itemType]));
                                colours.Add(UIColour.GreenText);
                            }
                            if (energyNeeded > 0)
                            {
                                texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.EnergyCost, energyNeeded.KWh));
                                colours.Add(UIColour.GreenText);
                            }
                        }
                    }
                    else if (!enoughResources)
                    {
                        foreach (var itemType in orderToCheck.Where(i => resourcesNeeded[i] > 0))
                        {
                            texts.Add(string.Format(cursorResourceCostStringTemplates[itemType], resourcesNeeded[itemType]));
                            colours.Add(enoughByItemType[itemType] ? UIColour.GreenText : UIColour.RedText);
                        }

                        if (energyNeeded > 0)
                        {
                            texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.EnergyCost, energyNeeded.KWh));
                            colours.Add(enoughEnergy ? UIColour.GreenText : UIColour.RedText);
                        }
                    }
                    else if (!enoughEnergy)
                    {
                        if (CurrentThingTypeToBuild == ThingType.Door && BlueprintTargetTile.ThingsPrimary.Any(t => t.ThingType == ThingType.Wall && (t as IRotatableThing)?.Direction == CurrentDirectionToBuild))
                        {
                            texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.WallDoorConversionCost, energyNeeded.KWh));
                            colours.Add(UIColour.RedText);
                        }
                        else if (CurrentThingTypeToBuild == ThingType.Wall && BlueprintTargetTile.ThingsPrimary.Any(t => t.ThingType == ThingType.Door && (t as IRotatableThing)?.Direction == CurrentDirectionToBuild))
                        {
                            texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.WallDoorConversionCost, energyNeeded.KWh));
                            colours.Add(UIColour.RedText);
                        }
                        else if (CurrentThingTypeToBuild == ThingType.Roof && colours.Count == 2)
                        {
                            colours[2] = Color.Red;
                        }
                        else
                        {
                            foreach (var itemType in orderToCheck.Where(i => resourcesNeeded[i] > 0))
                            {
                                texts.Add(string.Format(cursorResourceCostStringTemplates[itemType], resourcesNeeded[itemType]));
                                colours.Add(UIColour.GreenText);
                            }

                            texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.EnergyCost, energyNeeded.KWh));
                            colours.Add(UIColour.RedText);
                        }
                    }
                }

                var mine = World.VirtualBlueprint.FirstOrDefault(b => b.ThingType == ThingType.Mine);
                if (mine != null && mine.MainTile.ThingsAll.All(t => t.Definition?.BlocksConstruction == false))
                {
                    var resource = GetResourcesForMine(mine.MainTile);
                    if (resource.Any(r => r.Value > 0))
                    {
                        if (resource.ContainsKey(ItemType.Coal) && resource[ItemType.Coal] > 0)
                        {
                            texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.CoalCount, resource[ItemType.Coal]));
                            colours.Add(UIColour.LightBlueText);
                        }

                        if (resource.ContainsKey(ItemType.IronOre) && resource[ItemType.IronOre] > 0)
                        {
                            texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.OreCount, resource[ItemType.IronOre]));
                            colours.Add(UIColour.LightBlueText);
                        }

                        if (resource.ContainsKey(ItemType.Stone) && resource[ItemType.Stone] > 0)
                        {
                            texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.StoneCount, resource[ItemType.Stone]));
                            colours.Add(UIColour.LightBlueText);
                        }
                    }
                    else if (!mine.MainTile.IsMineResourceVisible || mine.MainTile.AdjacentTiles8.Any(t => !t.IsMineResourceVisible))
                    {
                        texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.NoKnownResources));
                        colours.Add(UIColour.OrangeText);
                    }
                    else if (texts.Any())
                    {
                        texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.NoResources));
                        colours.Add(UIColour.RedText);
                    }
                }

                var pump = World.VirtualBlueprint.FirstOrDefault(b => b.ThingType == ThingType.WaterPump);
                if (pump != null && pump.MainTile.ThingsAll.All(t => t.Definition?.BlocksConstruction == false))
                {
                    var extractionRate = GroundWaterController.GetTileExtractionRate(pump.MainTile, true);
                    texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.ExtractionRatePercent, extractionRate));
                    if (extractionRate >= 90) colours.Add(UIColour.GreenText);
                    else if (extractionRate >= 50) colours.Add(UIColour.YellowText);
                    else if (extractionRate >= 10) colours.Add(UIColour.OrangeText);
                    else colours.Add(UIColour.RedText);
                }

                if (canBuild && definition.CanRotate && !definition.AutoRotate && CurrentThingTypeToBuild != ThingType.FuelFactory && CurrentThingTypeToBuild != ThingType.ShorePump)
                {
                    var key1 = SettingsManager.GetKeysForAction("RotateBlueprint:Left").FirstOrDefault();
                    var key2 = SettingsManager.GetKeysForAction("RotateBlueprint:Right").FirstOrDefault();
                    texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.PressToRotate, key1, key2));
                    colours.Add(UIColour.DefaultText);
                }

                if (!canBuild)
                {
                    if (BuildHelper.CanBuildReason != "")
                    {
                        texts.Add(BuildHelper.CanBuildReason);
                        colours.Add(UIColour.RedText);
                    }

                    foreach (var blueprint in World.VirtualBlueprint)
                    {
                        blueprint.CanBuild = false;
                        blueprint.ColourA = 0.4f;
                        blueprint.RenderAlpha = 0.4f;
                    }
                }

                if (CurrentThingTypeToBuild == ThingType.OreScanner 
                    && (canBuild || (BlueprintTargetTile?.TerrainType == TerrainType.Dirt && BlueprintTargetTile.ThingsAll.All(t => t.Definition?.BlocksConstruction == false))))
                {
                    var tileCount = TileHighlightManager.TileHighlightsPulsing.Count;
                    texts.Add(LanguageHelper.GetForMouseCursor(StringsForMouseCursor.UnscannedTiles, tileCount));
                    colours.Add(tileCount > 0 ? UIColour.LightBlueText : UIColour.OrangeText);
                }
            }

            // Warnings for access being blocked
            if (canBuild && texts.Count < 5 && colours.All(c => c != UIColour.RedText))
            {
                var blueprint = World.VirtualBlueprint.FirstOrDefault();
                var definition = World.VirtualBlueprint.FirstOrDefault()?.Definition;
                if (blueprint != null && definition != null)
                {
                    var blockingTiles = new List<ISmallTile>();
                    var blockedTiles = new List<ISmallTile>();
                    if (definition.TileBlockModel.In(TileBlockModel.Circle, TileBlockModel.SmallCircle, TileBlockModel.Square) == true)
                    {
                        blockingTiles.AddRange(blueprint.AllTiles);
                        blockedTiles.AddRange(blueprint.AllTiles.SelectMany(t => t.AdjacentTiles8));
                    }
                    else if (blueprint.ThingType == ThingType.Wall)
                    {
                        foreach (var b in World.VirtualBlueprint)
                        {
                            var nextTile = b.MainTile.GetTileToDirection(b.Direction);
                            if (nextTile != null) blockedTiles.Add(nextTile);
                        }
                    }

                    foreach (var thing in blockedTiles.SelectMany(t => t.ThingsAll.OfType<IBuildableThing>()))
                    {
                        if (thing is IColonistInteractive i && i.GetAllAccessTiles().Any() && i.GetAllAccessTiles().All(t => blockingTiles.Contains(t)))
                        {
                            var text = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.BlocksAccessTo, thing.DisplayName);
                            if (!texts.Contains(text))
                            {
                                texts.Add(text);
                                colours.Add(UIColour.OrangeText);
                            }
                        }
                        else if (thing is IRepairableThing r && r.GetAllAccessTilesForRepair().Any() && r.GetAllAccessTilesForRepair().All(t => blockingTiles.Contains(t)))
                        {
                            var text = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.BlocksMaintenanceAccessTo, thing.DisplayName);
                            if (!texts.Contains(text))
                            {
                                texts.Add(text);
                                colours.Add(UIColour.OrangeText);
                            }
                        }
                    }
                }

                // Routine is repeated for walls, to check for blocking from the other direction
                if (blueprint != null && definition != null && blueprint.ThingType == ThingType.Wall)
                {
                    var blockingTiles = new List<ISmallTile>();
                    var blockedTiles = new List<ISmallTile>();
                    foreach (var b in World.VirtualBlueprint)
                    {
                        blockedTiles.Add(b.MainTile);
                        var nextTile = b.MainTile.GetTileToDirection(b.Direction);
                        if (nextTile != null) blockingTiles.Add(nextTile);
                    }

                    foreach (var thing in blockedTiles.SelectMany(t => t.ThingsAll.OfType<IBuildableThing>()))
                    {
                        if (thing is IColonistInteractive i && i.GetAllAccessTiles().Any() && i.GetAllAccessTiles().All(t => blockingTiles.Contains(t)))
                        {
                            var text = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.BlocksAccessTo, thing.DisplayName);
                            if (!texts.Contains(text))
                            {
                                texts.Add(text);
                                colours.Add(UIColour.OrangeText);
                            }
                        }
                        else if (thing is IRepairableThing r && r.GetAllAccessTilesForRepair().Any() && r.GetAllAccessTilesForRepair().All(t => blockingTiles.Contains(t)))
                        {
                            var text = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.BlocksMaintenanceAccessTo, thing.DisplayName);
                            if (!texts.Contains(text))
                            {
                                texts.Add(text);
                                colours.Add(UIColour.OrangeText);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < 5; i++)
            {
                MouseCursor.Instance.TextLine[i] = texts.Count > i ? texts[i] : "";
                MouseCursor.Instance.TextLineColour[i] = colours.Count > i ? colours[i] : UIColour.DefaultText;
            }

            foreach (var blueprint in World.VirtualBlueprint)
            {
                EventManager.RaiseEvent(EventType.VirtualBlueprint, EventSubType.Added, blueprint);
            }

            return;
        }

        private static bool AddWallBlueprints(List<Blueprint> prevVirtualBlueprints, bool canBuild)
        {
            if (CurrentThingTypeToBuild == ThingType.Wall && UIStatics.CurrentMouseState.LeftButton == ButtonState.Pressed && MouseDragStartTile != null)
            {
                var vb0 = prevVirtualBlueprints.Any() ? prevVirtualBlueprints[0] : null;

                // Add first section - direction is fixed.
                if (vb0 != null)
                {
                    canBuild &= BuildHelper.CanBuild(vb0.MainTile, ThingType.Wall, vb0.Direction);
                    BlueprintController.AddVirtualBuilding(vb0.MainTile, ThingType.Wall, canBuild, 1, vb0.Direction);
                }

                if (vb0 != null && vb0.Direction == Direction.SW && MouseWorldPosition.Tile.Y != vb0.MainTile.Y)
                {
                    // Row along same X
                    var miny = Math.Min(vb0.MainTile.Y, MouseWorldPosition.Tile.Y);
                    var maxy = Math.Max(vb0.MainTile.Y, MouseWorldPosition.Tile.Y);
                    for (int y = miny; y <= maxy; y++)
                    {
                        var tile = World.GetSmallTile(vb0.MainTile.X, y);
                        if (tile == null || (vb0.MainTile == tile && vb0.Direction == Direction.SW)) continue;
                        canBuild &= BuildHelper.CanBuild(tile, CurrentThingTypeToBuild.GetValueOrDefault(), Direction.SW);
                        if (tile.ThingsAll.All(t => t.ThingType != ThingType.Door || (t as IDoor).Direction != Direction.SW))  // Skip doors
                        {
                            BlueprintController.AddVirtualBuilding(tile, ThingType.Wall, canBuild, 1, Direction.SW);
                        }
                    }
                }
                else if (vb0 != null && vb0.Direction == Direction.SE && MouseWorldPosition.Tile.X != vb0.MainTile.X)
                {
                    // Row along same Y
                    var minx = Math.Min(vb0.MainTile.X, MouseWorldPosition.Tile.X);
                    var maxx = Math.Max(vb0.MainTile.X, MouseWorldPosition.Tile.X);
                    for (int x = minx; x <= maxx; x++)
                    {
                        var tile = World.GetSmallTile(x, vb0.MainTile.Y);
                        if (tile == null || (vb0.MainTile == tile && vb0.Direction == Direction.SE)) continue;
                        canBuild &= BuildHelper.CanBuild(tile, CurrentThingTypeToBuild.GetValueOrDefault(), Direction.SE);
                        if (tile.ThingsAll.All(t => t.ThingType != ThingType.Door || (t as IDoor).Direction != Direction.SE))  // Skip doors
                        {
                            BlueprintController.AddVirtualBuilding(tile, ThingType.Wall, canBuild, 1, Direction.SE);
                        }
                    }
                }
            }
            else
            {
                BlueprintController.AddVirtualBuilding(BlueprintTargetTile, CurrentThingTypeToBuild.GetValueOrDefault(), canBuild, 1, CurrentDirectionToBuild);
            }

            return canBuild;
        }

        private static List<ISmallTile> CreateVirtualRoof(ISmallTile targetTile)
        {
            var openNodes = new List<ISmallTile> { targetTile };
            var closedNodes = new HashSet<int>();
            var roofTiles = new List<ISmallTile>();
            var roomSize = 0;
            while (openNodes.Any() && roomSize <= Constants.MaxTilesPerRoom)
            {
                foreach (var node in openNodes.ToList())
                {
                    openNodes.Remove(node);
                    closedNodes.Add(node.Index);
                    roofTiles.Add(node);

                    roomSize++;

                    if (node.ThingsAll.Any(t => t.Definition?.CanBeIndoor != true && !t.ThingType.IsConduit() && !(t is IWall)))
                    {
                        return new List<ISmallTile>();
                    }

                    var isWallSE = node.ThingsPrimary.Any(t => t is IWall w && w.Direction == Direction.SE);
                    var isWallSW = node.ThingsPrimary.Any(t => t is IWall w && w.Direction == Direction.SW);
                    var isWallNE = node.TileToNE?.ThingsPrimary?.Any(t => t is IWall w && w.Direction == Direction.SW) == true;
                    var isWallNW = node.TileToNW?.ThingsPrimary?.Any(t => t is IWall w && w.Direction == Direction.SE) == true;

                    if (!isWallNE && !openNodes.Contains(node.TileToNE) && !closedNodes.Contains(node.TileToNE.Index)) openNodes.Add(node.TileToNE);
                    if (!isWallNW && !openNodes.Contains(node.TileToNW) && !closedNodes.Contains(node.TileToNW.Index)) openNodes.Add(node.TileToNW);
                    if (!isWallSE && !openNodes.Contains(node.TileToSE) && !closedNodes.Contains(node.TileToSE.Index)) openNodes.Add(node.TileToSE);
                    if (!isWallSW && !openNodes.Contains(node.TileToSW) && !closedNodes.Contains(node.TileToSW.Index)) openNodes.Add(node.TileToSW);
                }
            }

            return roofTiles;
        }

        private static Dictionary<ItemType, int> GetResourcesForMine(ISmallTile tile)
        {
            var result = new Dictionary<ItemType, int>();
            var tiles = tile.AdjacentTiles8;
            tiles.Add(tile);

            foreach (var t in tiles)
            {
                var resource = t.GetResources();
                if (resource != null && resource.IsVisible)
                {
                    if (!result.ContainsKey(resource.Type)) result.Add(resource.Type, t.MineResourceCount);
                    else result[resource.Type] += t.MineResourceCount;
                }
            }

            return result;
        }

        private static void PopulateCursorResourceCostStringTemplates()
        {
            cursorResourceCostStringTemplates.Add(ItemType.Metal, LanguageManager.Get<StringsForMouseCursor>(StringsForMouseCursor.MetalCost));
            cursorResourceCostStringTemplates.Add(ItemType.Stone, LanguageManager.Get<StringsForMouseCursor>(StringsForMouseCursor.StoneCost));
            cursorResourceCostStringTemplates.Add(ItemType.BatteryCells, LanguageManager.Get<StringsForMouseCursor>(StringsForMouseCursor.BatteryCellsCost));
            cursorResourceCostStringTemplates.Add(ItemType.Composites, LanguageManager.Get<StringsForMouseCursor>(StringsForMouseCursor.CompositesCost));
            cursorResourceCostStringTemplates.Add(ItemType.SolarCells, LanguageManager.Get<StringsForMouseCursor>(StringsForMouseCursor.SolarCellsCost));
            cursorResourceCostStringTemplates.Add(ItemType.Glass, LanguageManager.Get<StringsForMouseCursor>(StringsForMouseCursor.GlassCost));
            cursorResourceCostStringTemplates.Add(ItemType.Compost, LanguageManager.Get<StringsForMouseCursor>(StringsForMouseCursor.CompostCost));
        }
    }
}
