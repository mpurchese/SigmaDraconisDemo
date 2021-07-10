namespace SigmaDraconis.World
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Draconis.Shared;

    using Shared;

    using Blueprints;
    using Buildings;
    using Fauna;
    using Flora;
    using Prefabs;
    using WorldInterfaces;
    using ResourceNetworks;
    using Terrain;
    using Zones;

    public static class World
    {
        private static BigTileMap bigTileMap;
        private static SmallTileMap smallTileMap;

        public static WorldTime WorldTime = new WorldTime();
        public static WorldLight WorldLight = new WorldLight();

        public static GameVersion InitialGameVersion = new GameVersion(1, 0, 0);

        public static ClimateType ClimateType { get; set; } = ClimateType.Normal;
        public static int Temperature { get; set; } = 20;
        public static int Wind { get; set; } = 10;
        public static float WindDirection { get; set; } = 0f;

        private static bool isLoading;

        private static readonly Dictionary<ThingType, List<IThing>> thingsByType = new Dictionary<ThingType, List<IThing>>();
        private static readonly Dictionary<int, IThing> thingsById = new Dictionary<int, IThing>();
        private static readonly Dictionary<ThingType, Dictionary<int, List<IThing>>> thingsByTypeAndRow = new Dictionary<ThingType, Dictionary<int, List<IThing>>>();
        private static readonly Dictionary<int, List<IThing>> thingsByRow = new Dictionary<int, List<IThing>>();
        private static readonly Dictionary<int, int> thingRows = new Dictionary<int, int>();
        private static readonly Dictionary<int, IThing> thingsToUpdateEveryFrame = new Dictionary<int, IThing>();
        private static readonly Dictionary<int, int> foodCounts = new Dictionary<int, int>();

        // Optimisation to reduce ToList calls
        private static bool thingsToUpdateEveryFrameChanged;
        private static readonly List<IThing> thingsToUpdateEveryFrameList = new List<IThing>();

        // Blueprints
        public static List<Blueprint> VirtualBlueprint { get; set; } = new List<Blueprint>();
        public static Dictionary<int, Blueprint> ConfirmedBlueprints { get; set; } = new Dictionary<int, Blueprint>();
        public static Dictionary<int, Blueprint> RecycleBlueprints { get; set; } = new Dictionary<int, Blueprint>();    // These are for resource harvesting visuals

        // Buildable area
        public static HashSet<int> BuildableTiles { get; } = new HashSet<int>();
        public static HashSet<int> VirtualBuildableTiles { get; } = new HashSet<int>();   // When building network node

        // Prefabs
        public static PrefabCollection Prefabs { get; set; }

        // Natural resource recycle jobs, with terrain row
        public static Dictionary<int, int> ResourcesForDeconstruction = new Dictionary<int, int>();
        public static bool HasResourcesForDeconstructionBeenUsed { get; set; }   // For tutorial

        // Fruit harvest jobs
        private static readonly HashSet<int> plantsForHarvest = new HashSet<int>();
        private static readonly Dictionary<int, List<IFruitPlant>> plantsForHarvestByTerrainRow = new Dictionary<int, List<IFruitPlant>>();

        // This is used for controlling plant reseeding rates
        public static float InitialEmptyTileFraction { get; set; }

        // This is used by GroundCoverController
        public static Queue<Tuple<int, int>> TilesWithGroundCoverRemovedQueue { get; } = new Queue<Tuple<int, int>>();

        public static ResourceNetwork ResourceNetwork;

        public static bool CanDoGeology { get; private set; }
        public static bool CanHarvestFruit { get; private set; }
        public static bool AnyPlantsForHarvest => plantsForHarvest.Any();
        public static bool CanFarm { get; private set; }
        public static int DefaultCropId { get; set; } = 1;
        public static int LastTileSurveyedByGeologist { get; set; }

        public static IEnumerable<IColonist> Colonists => GetThings<IColonist>(ThingType.Colonist);

        public static void Init(int width, int height)
        {
            bigTileMap = new BigTileMap(width, height);
            smallTileMap = new SmallTileMap(bigTileMap);

            var thingTypes = Enum.GetValues(typeof(ThingType)).Cast<ThingType>();
            foreach (var thingType in thingTypes)
            {
                thingsByType.Add(thingType, new List<IThing>());
                thingsByTypeAndRow.Add(thingType, new Dictionary<int, List<IThing>>());
            }
        }

        public static void UpdateBigTileRenderLists()
        {
            bigTileMap.UpdateRenderLists();
        }

        public static int Width
        {
            get
            {
                return bigTileMap.Width;
            }
        }

        public static int Height
        {
            get
            {
                return bigTileMap.Height;
            }
        }

        public static int TerrainRowCount
        {
            get
            {
                return bigTileMap == null ? 0 : (2 * bigTileMap.Width) - 1;
            }
        }

        public static int SmallTileRowCount
        {
            get
            {
                return smallTileMap == null ? 0 : (2 * smallTileMap.Width) - 1;
            }
        }

        public static List<BigTile> BigTiles => bigTileMap.Tiles;
        public static List<BigTile> BigTilesWithDeepWaterEdge => bigTileMap.TilesWithDeepWaterEdge;
        public static List<BigTile> BigTilesWithWater => bigTileMap.TilesWithWater;
        public static List<BigTile> BigTilesWithLand => bigTileMap.TilesWithLand;

        public static List<ISmallTile> SmallTiles
        {
            get
            {
                return smallTileMap.Tiles.Values.ToList();
            }
        }

        public static List<List<ISmallTile>> SmallTilesByRow
        {
            get
            {
                return smallTileMap.TilesByRow;
            }
        }

        public static ISmallTile GetSmallTile(int index)
        {
            return index >= 0 && index < smallTileMap.Tiles.Count ? smallTileMap.Tiles[index] : null;
        }

        public static ISmallTile GetSmallTile(int x, int y)
        {
            return smallTileMap.GetTile(x, y);
        }

        public static ISmallTile GetSmallTile(Vector2i coordinates)
        {
            return smallTileMap.GetTile(coordinates.X, coordinates.Y);
        }

        public static List<ISmallTile> GetSmallTilesByRow(int row)
        {
            return smallTileMap.TilesByRow[row];
        }

        public static void Update()
        {
            WorldLight.Update(WorldTime);

            if (thingsToUpdateEveryFrameChanged)
            {
                thingsToUpdateEveryFrameList.Clear();
                thingsToUpdateEveryFrameList.AddRange(thingsToUpdateEveryFrame.Values);
                thingsToUpdateEveryFrameChanged = false;
            }

            foreach (var thing in thingsToUpdateEveryFrameList) thing.Update();
        }

        public static void AddThing(IThing thing)
        {
            var thingType = thing.ThingType;
            thingsByType[thingType].Add(thing);
            thingsById.Add(thing.Id, thing);
            thingRows.Add(thing.Id, thing.MainTile.Row);

            if (thing is IBuildableThing || Constants.ResourceStackTypes.ContainsValue(thingType)
                || thingType.In(ThingType.RedBug, ThingType.BlueBug, ThingType.Bee, ThingType.Fish, ThingType.Bird1, ThingType.Tortoise, ThingType.SnowTortoise, ThingType.TableMetal, ThingType.TableStone))
            {
                thingsToUpdateEveryFrame.Add(thing.Id, thing);
                thingsToUpdateEveryFrameChanged = true;
            }

            var row = thing.MainTile.Row;
            if (thingsByTypeAndRow[thingType].ContainsKey(row)) thingsByTypeAndRow[thingType][row].Add(thing);
            else thingsByTypeAndRow[thingType].Add(row, new List<IThing>() { thing });

            if (thingsByRow.ContainsKey(row)) thingsByRow[row].Add(thing);
            else thingsByRow.Add(row, new List<IThing>() { thing });

            EventManager.RaiseEvent(EventType.Thing, EventSubType.Added, thing);
            if (thing is Building building)
            {
                EventManager.RaiseEvent(EventType.Building, EventSubType.Added, building);
            }
            else if (thing is Plant plant)
            {
                EventManager.RaiseEvent(EventType.Plant, EventSubType.Added, plant);
            }
            else if (thing is Colonist colonist)
            {
                EventManager.RaiseEvent(EventType.Colonist, EventSubType.Added, colonist);
            }
            else if (thing is Animal animal)
            {
                EventManager.RaiseEvent(EventType.Animal, EventSubType.Added, animal);
            }

            if (!isLoading) thing.AfterAddedToWorld();
        }

        public static void UpdateThingPosition(Thing thing)
        {
            var thingType = thing.ThingType;
            var row = thing.MainTile.Row;
            var prevRow = thingRows.ContainsKey(thing.Id) ? thingRows[thing.Id] : 0;
            thingRows[thing.Id] = row;
            if (row != prevRow)
            {
                if (thingsByTypeAndRow[thingType].ContainsKey(prevRow) && thingsByTypeAndRow[thingType][prevRow].Contains(thing))
                {
                    thingsByTypeAndRow[thingType][prevRow].Remove(thing);
                }

                if (thingsByRow.ContainsKey(prevRow) && thingsByRow[prevRow].Contains(thing))
                {
                    thingsByRow[prevRow].Remove(thing);
                }

                if (thingsByTypeAndRow[thingType].ContainsKey(row)) thingsByTypeAndRow[thingType][row].Add(thing);
                else thingsByTypeAndRow[thingType].Add(row, new List<IThing>() { thing });

                if (thingsByRow.ContainsKey(row)) thingsByRow[row].Add(thing);
                else thingsByRow.Add(row, new List<IThing>() { thing });
            }
        }

        public static IThing GetThing(int id)
        {
            thingsById.TryGetValue(id, out IThing thing);
            return thing;
        }

        public static IEnumerable<IThing> GetThings(ThingType type)
        {
            return thingsByType[type];
        }

        public static IEnumerable<T> GetThings<T>(ThingType type) where T : IThing
        {
            return thingsByType[type].OfType<T>();
        }

        public static IEnumerable<IThing> GetThings(params ThingType[] types)
        {
            foreach (var type in types)
            {
                foreach (var thing in thingsByType[type])
                {
                    yield return thing;
                }
            }
        }

        public static IEnumerable<T> GetThings<T>(params ThingType[] types) where T : IThing
        {
            foreach (var type in types)
            {
                foreach (var thing in thingsByType[type].OfType<T>())
                {
                    yield return thing;
                }
            }
        }

        public static IEnumerable<IThing> GetThings(IEnumerable<ThingType> types)
        {
            foreach (var type in types)
            {
                foreach (var thing in thingsByType[type])
                {
                    yield return thing;
                }
            }
        }

        public static IEnumerable<IResourceStack> GetResourceStacks(ItemType itemType)
        {
            return GetThings<IResourceStack>(Constants.ResourceStackTypes[itemType]);
        }

        public static IEnumerable<IPlanter> GetPlanters()
        {
            return GetThings<IPlanter>(ThingType.PlanterStone, ThingType.PlanterHydroponics);
        }

        public static List<IThing> GetThingsByRow(int row, ThingType type)
        {
            return thingsByTypeAndRow[type].ContainsKey(row) ? thingsByTypeAndRow[type][row] : new List<IThing>();
        }

        public static List<IThing> GetThingsByRow(int row)
        {
            return thingsByRow.ContainsKey(row) ? thingsByRow[row] : new List<IThing>();
        }

        public static List<IFruitPlant> GetPlantsForHarvest(int row)
        {
            return plantsForHarvestByTerrainRow.ContainsKey(row) ? plantsForHarvestByTerrainRow[row] : new List<IFruitPlant>();
        }

        public static void RemoveThing(IThing thing)
        {
            RemoveThing(thing as Thing);
        }

        public static void RemoveThing(Thing thing)
        {
            thing.BeforeRemoveFromWorld();

            var thingType = thing.ThingType;

            thingsByType[thingType].Remove(thing);
            thingsById.Remove(thing.Id);

            var row = thing.MainTile.Row;
            if (thingsByTypeAndRow.ContainsKey(thingType) && thingsByTypeAndRow[thingType][row].Contains(thing)) thingsByTypeAndRow[thingType][row].Remove(thing);
            if (thingsByRow[row].Contains(thing)) thingsByRow[row].Remove(thing);
            if (thingRows.ContainsKey(thing.Id)) thingRows.Remove(thing.Id);
            if (thingsToUpdateEveryFrame.ContainsKey(thing.Id))
            {
                thingsToUpdateEveryFrameChanged = true;
                thingsToUpdateEveryFrame.Remove(thing.Id);
            }

            if (ResourcesForDeconstruction.ContainsKey(thing.Id))
            {
                ResourcesForDeconstruction.Remove(thing.Id);
                EventManager.EnqueueWorldPropertyChangeEvent(thing.Id, nameof(ResourcesForDeconstruction), thing.MainTile.Row, thing.ThingType);
            }

            if (plantsForHarvest.Contains(thing.Id))
            {
                plantsForHarvest.Remove(thing.Id);
                if (plantsForHarvestByTerrainRow.ContainsKey(row) && thing is IFruitPlant plant && plantsForHarvestByTerrainRow[row].Contains(plant)) plantsForHarvestByTerrainRow[row].Remove(plant);
                EventManager.EnqueueWorldPropertyChangeEvent(thing.Id, "plantsForHarvest", thing.MainTile.Row, thing.ThingType);
            }

            PathFinderBlockManager.RemoveBlocks(thing.Id);
            for (int i = 0; i < thing.AllTiles.Count; ++i)
            {
                var tile = thing.AllTiles[i];
                tile.RemoveThing(thing);
            }

            thing.AfterRemoveFromWorld();

            EventManager.RaiseEvent(EventType.Thing, EventSubType.Removed, thing);
            if (thing is Building building)
            {
                EventManager.RaiseEvent(EventType.Building, EventSubType.Removed, building);
            }
            else if (thing is IColonist colonist)
            {
                EventManager.RaiseEvent(EventType.Colonist, EventSubType.Removed, colonist);
            }
            else if (thing is Bug bug)
            {
                EventManager.RaiseEvent(EventType.Animal, EventSubType.Removed, bug);
            }
            else if (thing is Plant plant)
            {
                EventManager.RaiseEvent(EventType.Plant, EventSubType.Removed, plant);
            }
            else if (thing is IStackingArea stackingArea)
            {
                EventManager.RaiseEvent(EventType.StackingArea, EventSubType.Removed, stackingArea);
            }

            if ((thing as IThingWithShadow)?.ShadowModel?.HasShadowModel == true || thing.ThingType == ThingType.Tree || thing.ThingType == ThingType.Bush)
            {
                EventManager.RaiseEvent(EventType.Shadow, EventSubType.Removed, thing);
            }
        }

        public static List<IThing> GetAllThings()
        {
            return thingsByType.SelectMany(t => t.Value).ToList();
        }

        public static void UpdateCanHarvestFruit()
        {
            CanHarvestFruit = GetThings<IColonist>(ThingType.Colonist).Any(c => c.Skill == SkillType.Botanist && !c.IsDead) 
                && GetThings<ICooker>(ThingType.Cooker).Any(c => c.IsReady && !c.IsDesignatedForRecycling);
        }

        public static void UpdateCanFarm()
        {
            CanFarm = GetThings<IColonist>(ThingType.Colonist).Any(c => c.Skill == SkillType.Botanist && !c.IsDead)
                && GetThings<IPlanter>(ThingType.PlanterHydroponics, ThingType.PlanterStone).Any(c => c.IsReady && !c.IsDesignatedForRecycling);
        }

        public static void UpdateCanDoGeology()
        {
            CanDoGeology = GetThings<IColonist>(ThingType.Colonist).Any(c => c.Skill == SkillType.Geologist && !c.IsDead);
        }

        public static void HandleFruitPlantUpdate(IFruitPlant plant)
        {
            if (plant.MainTile == null) return;  // This happens during deserialization

            var row = plant.MainTile.Row;
            if (plant.CountFruitAvailable > 0 && plant.HarvestFruitPriority != WorkPriority.Disabled && !plantsForHarvest.Contains(plant.Id))
            {
                plantsForHarvest.Add(plant.Id);
                if (!plantsForHarvestByTerrainRow.ContainsKey(row)) plantsForHarvestByTerrainRow.Add(row, new List<IFruitPlant> { plant });
                else if (!plantsForHarvestByTerrainRow[row].Contains(plant)) plantsForHarvestByTerrainRow[row].Add(plant);

                EventManager.EnqueueWorldPropertyChangeEvent(plant.Id, "plantsForHarvest", row, plant.ThingType);
                EventManager.RaiseEvent(EventType.PlantsForHarvest, EventSubType.Added, plant);  // For AI
            }
            else if ((plant.CountFruitAvailable == 0 || plant.HarvestFruitPriority == WorkPriority.Disabled) && plantsForHarvest.Contains(plant.Id))
            {
                plantsForHarvest.Remove(plant.Id);
                if (plantsForHarvestByTerrainRow.ContainsKey(row) && plantsForHarvestByTerrainRow[row].Contains(plant)) plantsForHarvestByTerrainRow[row].Remove(plant);
                EventManager.EnqueueWorldPropertyChangeEvent(plant.Id, "plantsForHarvest", row, plant.ThingType);
                EventManager.RaiseEvent(EventType.PlantsForHarvest, EventSubType.Removed, plant);  // For AI
            }
        }

        public static void ExpandBuildableAreaAroundTile(ISmallTile tile)
        {
            foreach (var t in tile.AdjacentTiles8.SelectMany(x => x.AdjacentTiles4).SelectMany(x => x.AdjacentTiles4))
            {
                if ((t.TerrainType == TerrainType.Dirt || t.TerrainType == TerrainType.Coast) && !BuildableTiles.Contains(t.Index)) BuildableTiles.Add(t.Index);
            }

            EventManager.RaiseEvent(EventType.BuildableArea, null);
        }

        public static void RefreshBuildableArea(int? thingToExclude = null)
        {
            BuildableTiles.Clear();
            foreach (var c in GetThings(ThingType.ConduitNode, ThingType.Lander))
            {
                if (c.Id == thingToExclude) continue;
                foreach (var t in c.MainTile.AdjacentTiles8.SelectMany(x => x.AdjacentTiles4).SelectMany(x => x.AdjacentTiles4))
                {
                    if ((t.TerrainType == TerrainType.Dirt || t.TerrainType == TerrainType.Coast) && !BuildableTiles.Contains(t.Index)) BuildableTiles.Add(t.Index);
                }
            }

            EventManager.RaiseEvent(EventType.BuildableArea, null);
        }

        public static void ClearVirtualBlueprint()
        {
            VirtualBlueprint.Clear();
            if (VirtualBuildableTiles.Any())
            {
                VirtualBuildableTiles.Clear();
                EventManager.RaiseEvent(EventType.VirtualBuildableArea, null);
            }
        }

        public static void SetVirtualBuildableArea(ISmallTile centreTile)
        {
            VirtualBuildableTiles.Clear();
            foreach (var t in centreTile.AdjacentTiles8.SelectMany(x => x.AdjacentTiles4).SelectMany(x => x.AdjacentTiles4))
            {
                if ((t.TerrainType == TerrainType.Dirt || t.TerrainType == TerrainType.Coast) && !BuildableTiles.Contains(t.Index) && !VirtualBuildableTiles.Contains(t.Index))
                {
                    VirtualBuildableTiles.Add(t.Index);
                }
            }

            EventManager.RaiseEvent(EventType.VirtualBuildableArea, null);
        }

        public static void Clear(int mapSize = 0)
        {
            thingsById.Clear();
            thingRows.Clear();
            thingsToUpdateEveryFrame.Clear();
            ResourcesForDeconstruction.Clear();
            HasResourcesForDeconstructionBeenUsed = false;
            plantsForHarvest.Clear();
            plantsForHarvestByTerrainRow.Clear();
            thingsByRow.Clear();
            BuildableTiles.Clear();
            foodCounts.Clear();

            var thingTypes = Enum.GetValues(typeof(ThingType)).Cast<ThingType>();
            foreach (var thingType in thingTypes)
            {
                thingsByType[thingType] = new List<IThing>();
                thingsByTypeAndRow[thingType] = new Dictionary<int, List<IThing>>();
            }

            if (mapSize > 0)
            {
                bigTileMap = new BigTileMap(mapSize, mapSize);
                smallTileMap = new SmallTileMap(bigTileMap);
            }
            else
            {
                bigTileMap = null;
            }

            CanHarvestFruit = false;
            CanFarm = false;
            CanDoGeology = false;
            DefaultCropId = 1;
            TilesWithGroundCoverRemovedQueue.Clear();
        }

        public static void Load(WorldTime time, List<BigTile> tiles, List<Thing> things, List<KeyValuePair<int, int>> newFoodCounts)
        {
            isLoading = true;
            WorldTime = time;

            for (int i = 0; i < tiles.Count; i++)
            {
                BigTiles[i].TerrainType = tiles[i].TerrainType;
                BigTiles[i].BigTileTextureIdentifier = tiles[i].BigTileTextureIdentifier;
                BigTiles[i].UpdateCoords();
                BigTiles[i].UpdateSmallTileTerrainTypes();
            }

            UpdateBigTileRenderLists();

            // Things
            foreach (var thing in things.Where(t => !(t is Building)).ToList())
            {
                AddThing(thing);
            }

            foreach (var part in things.OfType<Building>())
            {
                AddThing(part);
            }

            // Call AfterAddedToWorld() for all at once as may interact with each other
            foreach (var thing in things) thing.AfterAddedToWorld();

            foodCounts.Clear();
            foreach (var kv in newFoodCounts) foodCounts.Add(kv.Key, kv.Value);

            isLoading = false;
        }

        public static void AddFood(int foodType, int count = 1)
        {
            if (!foodCounts.ContainsKey(foodType)) foodCounts.Add(foodType, 0);
            foodCounts[foodType] += count;
        }

        public static bool TakeFood(int foodType)
        {
            if (!foodCounts.ContainsKey(foodType) || foodCounts[foodType] == 0) return false;
            foodCounts[foodType]--;
            return true;
        }

        public static IEnumerable<KeyValuePair<int, int>> GetFoodCounts()
        {
            return foodCounts.Where(c => c.Value > 0);
        }

        public static int GetFoodCount(int foodType)
        {
            return foodCounts.ContainsKey(foodType) ? foodCounts[foodType] : 0;
        }
    }
}
