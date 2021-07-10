namespace SigmaDraconis.UI
{
    using Draconis.UI;
    using System.Collections.Generic;
    using System.Linq;
    using Config;
    using Language;
    using Settings;
    using Shared;
    using World;
    using World.Rooms;
    using WorldInterfaces;

    public static class PlayerActivityDeconstruct
    {
        private static bool canDoAction;
        private static int currentLanguageId;
        private static readonly Dictionary<ItemType, string> itemTypeNamesUpper = new Dictionary<ItemType, string>();

        public static void HandleLeftClick()
        {
            Update();
            if (GameScreen.Instance.HighlightedThing is IRecyclableThing r && r.CanRecycle() && canDoAction)
            {
                Deconstruct(GameScreen.Instance.HighlightedThing);
            }
            else if (GameScreen.Instance.HighlightedThing is IResourceStack rs && rs.ItemCount == 0)
            {
                // Remove empty stack
                Deconstruct(GameScreen.Instance.HighlightedThing);
            }
            else if (GameScreen.Instance.HighlightedThing is IStackingArea)
            {
                World.RemoveThing(GameScreen.Instance.HighlightedThing);
            }
        }

        public static void Deconstruct(IThing thing)
        {
            if (thing is IResourceStack rs && rs.ItemCount == 0)
            {
                // Remove empty resource stack
                foreach (var blueprint in World.ConfirmedBlueprints.Where(b => b.Value.MainTile == thing.MainTile && b.Value.ThingType == thing.ThingType))
                {
                    World.ConfirmedBlueprints.Remove(blueprint.Key);
                    EventManager.RaiseEvent(EventType.Blueprint, EventSubType.Removed, blueprint.Value);
                    break;
                }

                World.RemoveThing(thing);
            }
            else if (thing is IPlant || thing is IRock || thing.ThingType == ThingType.LandingPod || thing.ThingType == ThingType.Colonist)
            {
                if (thing.RecyclePriority != WorkPriority.Disabled)
                {
                    thing.RecyclePriority = WorkPriority.Disabled;
                    if (World.ResourcesForDeconstruction.ContainsKey(thing.Id)) World.ResourcesForDeconstruction.Remove(thing.Id);
                    EventManager.RaiseEvent(EventType.ResourcesForDeconstruction, EventSubType.Removed, thing);  // For AI
                }
                else
                {
                    thing.RecyclePriority = WorkPriority.Low;
                    if (!World.ResourcesForDeconstruction.ContainsKey(thing.Id)) World.ResourcesForDeconstruction.Add(thing.Id, thing.MainTile.Row);
                    EventManager.RaiseEvent(EventType.ResourcesForDeconstruction, EventSubType.Added, thing);  // For AI
                    if (thing is IFruitPlant fp) fp.SetHarvestFruitPriority(WorkPriority.Disabled);
                }

                World.HasResourcesForDeconstructionBeenUsed = true;
                EventManager.EnqueueWorldPropertyChangeEvent(thing.Id, nameof(World.ResourcesForDeconstruction), thing.MainTile.Row, thing.ThingType);  // For renderer
            }
            else if (thing.ThingType == ThingType.Roof && thing is IBuildableThing roof)
            {
                // Get connected roofs
                var roofs = new List<IBuildableThing> { roof };
                var openNodes = new List<ISmallTile> { roof.MainTile };
                var closedNodes = new HashSet<int> { roof.MainTileIndex };
                while (openNodes.Any())
                {
                    var list = openNodes.ToList();
                    openNodes.Clear();
                    foreach (var n in list)
                    {
                        // Check adjacent tiles for roofs not already in list
                        var roofNE = n.TileToNE.ThingsPrimary.Where(t => t.ThingType == ThingType.Roof).OfType<IBuildableThing>().FirstOrDefault();
                        var roofSE = n.TileToSE.ThingsPrimary.Where(t => t.ThingType == ThingType.Roof).OfType<IBuildableThing>().FirstOrDefault();
                        var roofSW = n.TileToSW.ThingsPrimary.Where(t => t.ThingType == ThingType.Roof).OfType<IBuildableThing>().FirstOrDefault();
                        var roofNW = n.TileToNW.ThingsPrimary.Where(t => t.ThingType == ThingType.Roof).OfType<IBuildableThing>().FirstOrDefault();

                        if (roofNE != null && !closedNodes.Contains(roofNE.MainTileIndex) && roofNE.MainTile.ThingsPrimary.OfType<IWall>().All(w => w.Direction != Direction.SW))
                        {
                            openNodes.Add(roofNE.MainTile);
                            closedNodes.Add(roofNE.MainTileIndex);
                            roofs.Add(roofNE);
                        }

                        if (roofNW != null && !closedNodes.Contains(roofNW.MainTileIndex) && roofNW.MainTile.ThingsPrimary.OfType<IWall>().All(w => w.Direction != Direction.SE))
                        {
                            openNodes.Add(roofNW.MainTile);
                            closedNodes.Add(roofNW.MainTileIndex);
                            roofs.Add(roofNW);
                        }

                        if (roofSE != null && !closedNodes.Contains(roofSE.MainTileIndex) && n.ThingsPrimary.OfType<IWall>().All(w => w.Direction != Direction.SE))
                        {
                            openNodes.Add(roofSE.MainTile);
                            closedNodes.Add(roofSE.MainTileIndex);
                            roofs.Add(roofSE);
                        }

                        if (roofSW != null && !closedNodes.Contains(roofSW.MainTileIndex) && n.ThingsPrimary.OfType<IWall>().All(w => w.Direction != Direction.SW))
                        {
                            openNodes.Add(roofSW.MainTile);
                            closedNodes.Add(roofSW.MainTileIndex);
                            roofs.Add(roofSW);
                        }
                    }
                }

                // Begin deconstruction
                foreach (var r in roofs)
                {
                    DeconstructBuilding(r);
                }
            }
            else if (thing is IBuildableThing building)
            {
                DeconstructBuilding(building);
            }
        }

        public static void Update()
        {
            for (int i = 0; i < 5; i++) MouseCursor.Instance.TextLine[i] = "";

            var network = World.ResourceNetwork;
            if (network == null) return;

            if (!(GameScreen.Instance.HighlightedThing is Thing thing)) 
            {
                return;
            }
            else if (thing is IStackingArea sa)
            {
                MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.ClickToRemoveStackingArea);
                MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.DarkGreenText : UIColour.GreenText;
                canDoAction = true;
            }
            else if (thing is IResourceStack rs && rs.ItemCount == 0)
            {
                MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.ClickToRemoveEmptyStack, thing.DisplayNameLower);
                MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.DarkGreenText : UIColour.GreenText;
            }
            else if (thing.Definition.CanRecycle == false && !(thing is IColonist c && c.IsDead))
            {
                MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.CantDeconstructThingType, thing.DisplayNameLower);
                MouseCursor.Instance.TextLineColour[0] = UIColour.RedText;
            }
            else if (thing.IsRecycling)
            {
                MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.AlreadyDeconstructingThingType, thing.DisplayNameLower);
                MouseCursor.Instance.TextLineColour[0] = UIColour.RedText;
            }
            else if ((thing as IFactoryBuilding)?.FactoryStatus == FactoryStatus.InProgress)
            {
                MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.SwitchOffToEnableDeconstruction);
                MouseCursor.Instance.TextLineColour[0] = UIColour.RedText;
            }
            else if (thing is IPlant plant)
            {
                if (plant.RecyclePriority != WorkPriority.Disabled)
                {
                    MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.ClickToCancelDeconstruction);
                    MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.WhiteText : UIColour.DefaultText;
                    canDoAction = true;
                }
                else
                {
                    var biomass = thing.GetDeconstructionYield(ItemType.Biomass);
                    MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructGeneral1, thing.ShortNameLower, biomass, GetItemTypeName(ItemType.Biomass));
                    MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.DarkGreenText : UIColour.GreenText;
                    canDoAction = true;

                    if ((plant as IFruitPlant)?.CountFruitAvailable > 0)
                    {
                        MouseCursor.Instance.TextLine[1] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.FruitWillBeDestroyed);
                        MouseCursor.Instance.TextLineColour[1] = UIColour.OrangeText;
                    }
                    else if ((plant as IFruitPlant)?.CanFruit == true)
                    {
                        MouseCursor.Instance.TextLine[1] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.PlantProducesEdibleFruit);
                        MouseCursor.Instance.TextLineColour[1] = UIColour.OrangeText;
                    }
                    else if (!plant.GetAllAccessTiles().Any())
                    {
                        MouseCursor.Instance.TextLine[1] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.LocationNotAccessible);
                        MouseCursor.Instance.TextLineColour[1] = UIColour.OrangeText;
                    }
                }
            }
            else if (thing is IRock rock)
            {
                if (rock.RecyclePriority != WorkPriority.Disabled)
                {
                    MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.ClickToCancelDeconstruction);
                    MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.WhiteText : UIColour.DefaultText;
                    canDoAction = true;
                }
                else
                {
                    var ore = thing.GetDeconstructionYield(ItemType.IronOre);
                    if (ore > 0) MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructRock, ore, GetItemTypeName(ItemType.IronOre));
                    else
                    {
                        var coal = thing.GetDeconstructionYield(ItemType.Coal);
                        if (coal > 0) MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructRock, coal, GetItemTypeName(ItemType.Coal));
                        else
                        {
                            var stone = thing.GetDeconstructionYield(ItemType.Stone);
                            if (stone > 0) MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructRock, stone, GetItemTypeName(ItemType.Stone));
                        }
                    }

                    MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.DarkGreenText : UIColour.GreenText;
                    canDoAction = true;

                    if (!rock.GetAllAccessTiles().Any())
                    {
                        MouseCursor.Instance.TextLine[1] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.LocationNotAccessible);
                        MouseCursor.Instance.TextLineColour[1] = UIColour.OrangeText;
                    }
                }
            }
            else if (thing is ILandingPod pod)
            {
                if (!pod.IsEmpty || pod.MainTile.ThingsAll.Any(t => t.ThingType == ThingType.Colonist))
                {
                    MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.PodNotEmpty);
                    MouseCursor.Instance.TextLineColour[0] = UIColour.RedText;
                    canDoAction = false;
                }
                else if (pod.RecyclePriority != WorkPriority.Disabled)
                {
                    MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.ClickToCancelDeconstruction);
                    MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.WhiteText : UIColour.DefaultText;
                    canDoAction = true;
                }
                else
                {
                    var metal = thing.GetDeconstructionYield(ItemType.Metal);
                    var metalStr = LanguageManager.Get<ItemType>(ItemType.Metal).ToUpperInvariant();
                    MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructGeneral1, thing.DisplayNameLower, metal, metalStr);
                    MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.DarkGreenText : UIColour.GreenText;
                    canDoAction = true;

                    if (!pod.GetAllAccessTiles().Any())
                    {
                        MouseCursor.Instance.TextLine[1] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.LocationNotAccessible);
                        MouseCursor.Instance.TextLineColour[1] = UIColour.OrangeText;
                    }
                }
            }
            else if (thing is IColonist colonist)
            {
                if (colonist.RecyclePriority != WorkPriority.Disabled)
                {
                    MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.ClickToCancelDeconstruction);
                    MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.WhiteText : UIColour.DefaultText;
                    canDoAction = true;
                }
                else
                {
                    var organics = thing.GetDeconstructionYield(ItemType.Biomass);
                    MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.RecycleDeadColonistOrganics, organics);
                    MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.DarkGreenText : UIColour.GreenText;
                    canDoAction = true;
                }
            }
            else if (!(thing is IRecyclableThing rt))
            {
                MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.CantDeconstructThingType, thing.DisplayNameLower);
                MouseCursor.Instance.TextLineColour[0] = UIColour.RedText;
            }
            else if (!(thing as IRecyclableThing).CanRecycle())
            {
                var reason = (thing as IRecyclableThing).CanRecycleReason;
                MouseCursor.Instance.TextLine[0] = string.IsNullOrEmpty(reason)
                    ? LanguageHelper.GetForMouseCursor(StringsForMouseCursor.CantDeconstructThingType, thing.DisplayNameLower)
                    : LanguageHelper.GetForMouseCursor(StringsForMouseCursor.CantDeconstructThingTypeReason, thing.DisplayNameLower, (thing as IRecyclableThing).CanRecycleReason);
                MouseCursor.Instance.TextLineColour[0] = UIColour.RedText;
            }
            else
            {
                var metal = thing.GetDeconstructionYield(ItemType.Metal);
                var stone = thing.GetDeconstructionYield(ItemType.Stone);
                var batteryCells = thing.GetDeconstructionYield(ItemType.BatteryCells);
                var composites = thing.GetDeconstructionYield(ItemType.Composites);
                var solarCells = thing.GetDeconstructionYield(ItemType.SolarCells);
                var glass = thing.GetDeconstructionYield(ItemType.Glass);
                var compost = thing.GetDeconstructionYield(ItemType.Compost);
                canDoAction = false;
                if (thing.ThingType == ThingType.Roof && thing is IBuildableThing building)
                {
                    // Can only deconstruct whole room at a time
                    var roofs = RoomManager.GetAllRoofsForRoom(building.MainTileIndex);
                    if (roofs.Any(r => r.IsRecycling))
                    {
                        MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.AlreadyDeconstructingThingType, thing.DisplayNameLower);
                        MouseCursor.Instance.TextLineColour[0] = UIColour.RedText;
                        return;
                    }
                    else
                    {
                        var count = roofs?.Count ?? 0;
                        glass *= count;
                        MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructRoof, count, glass, GetItemTypeName(ItemType.Glass));
                        MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.DarkGreenText : UIColour.GreenText;

                        var key = SettingsManager.GetKeysForAction("ToggleRoof").FirstOrDefault();
                        MouseCursor.Instance.TextLine[1] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.PressKeyToHideRoof, key);
                        MouseCursor.Instance.TextLineColour[1] = World.ClimateType == ClimateType.Snow ? UIColour.WhiteText : UIColour.DefaultText;
                    }
                }
                else if (thing.ThingType == ThingType.Rocket)
                {
                    MouseCursor.Instance.TextLine[0] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructRocket);
                    MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.DarkGreenText : UIColour.GreenText;
                }
                else
                {
                    var name = thing.DisplayNameLower;
                    if (metal > 0 && stone > 0) MouseCursor.Instance.TextLine[0] = GetDeconstructString(thing, ItemType.Metal, metal, ItemType.Stone, stone);
                    else if (metal > 0 && batteryCells > 0) MouseCursor.Instance.TextLine[0] = GetDeconstructString(thing, ItemType.Metal, metal, ItemType.BatteryCells, batteryCells);
                    else if (metal > 0 && composites > 0) MouseCursor.Instance.TextLine[0] = GetDeconstructString(thing, ItemType.Metal, metal, ItemType.Composites, composites);
                    else if (metal > 0 && solarCells > 0) MouseCursor.Instance.TextLine[0] = GetDeconstructString(thing, ItemType.Metal, metal, ItemType.SolarCells, solarCells);
                    else if (stone > 0 && compost > 0) MouseCursor.Instance.TextLine[0] = GetDeconstructString(thing, ItemType.Stone, stone, ItemType.Compost, compost);
                    else if (metal > 0) MouseCursor.Instance.TextLine[0] = GetDeconstructString(thing, ItemType.Metal, metal);
                    else if (stone > 0) MouseCursor.Instance.TextLine[0] = GetDeconstructString(thing, ItemType.Stone, stone);
                    else if (glass > 0) MouseCursor.Instance.TextLine[0] = GetDeconstructString(thing, ItemType.Glass, glass);
                    MouseCursor.Instance.TextLineColour[0] = World.ClimateType == ClimateType.Snow ? UIColour.DarkGreenText : UIColour.GreenText;
                }

                var resourceCapacity = network.ResourcesCapacity - network.CountResources;
                var itemCapacity = network.ItemsCapacity - network.CountItems;

                var otherRecyclingThings = World.GetThings(ThingTypeManager.BuildableThingTypes).OfType<IRecyclableThing>().Where(t => t.IsDesignatedForRecycling);
                foreach (var t in otherRecyclingThings.Where(r => r != null).OfType<IRecyclableThing>())
                {
                    var y = t.GetDeconstructionYield();
                    resourceCapacity -= y.Where(kv => Constants.StorageTypesByItemType[kv.Key] == ThingType.Silo).Sum(kv => kv.Value);
                    itemCapacity -= y.Where(kv => Constants.StorageTypesByItemType[kv.Key] == ThingType.ItemsStorage).Sum(kv => kv.Value);
                }

                var yield = thing.GetDeconstructionYield();
                var resourceYield = yield.Where(kv => Constants.StorageTypesByItemType[kv.Key] == ThingType.Silo).Sum(kv => kv.Value);
                var itemYield = yield.Where(kv => Constants.StorageTypesByItemType[kv.Key] == ThingType.ItemsStorage).Sum(kv => kv.Value);

                if ((resourceYield > 0 && resourceYield > resourceCapacity) || (itemYield > 0 && itemYield > itemCapacity))
                {
                    MouseCursor.Instance.TextLine[1] = LanguageHelper.GetForMouseCursor(StringsForMouseCursor.LowStorageMayLoseResources);
                    MouseCursor.Instance.TextLineColour[1] = UIColour.OrangeText;
                }

                canDoAction = true;
            }
        }

        private static string GetDeconstructString(IThing thing, ItemType itemType, int itemCount)
        {
            return LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructGeneral1, thing.DisplayNameLower, itemCount, GetItemTypeName(itemType));
        }

        private static string GetDeconstructString(IThing thing, ItemType itemType1, int itemCount1, ItemType itemType2, int itemCount2)
        {
            return LanguageHelper.GetForMouseCursor(StringsForMouseCursor.DeconstructGeneral2, thing.DisplayNameLower, itemCount1, GetItemTypeName(itemType1), itemCount2, GetItemTypeName(itemType2));
        }

        private static void DeconstructBuilding(IBuildableThing building)
        {
            if (building.IsDesignatedForRecycling && !building.IsRecycling)
            {
                // Didn't commit job yet, so can cancel easily
                building.CancelRecycle();
            }
            else
            {
                building.Recycle();
            }
        }

        private static string GetItemTypeName(ItemType itemType)
        {
            if (currentLanguageId != UIStatics.CurrentLanguageId)
            {
                currentLanguageId = UIStatics.CurrentLanguageId;
                itemTypeNamesUpper.Clear();
            }

            if (!itemTypeNamesUpper.ContainsKey(itemType)) itemTypeNamesUpper.Add(itemType, LanguageManager.Get<ItemType>(itemType).ToUpperInvariant());
            return itemTypeNamesUpper[itemType];
        }
    }
}
