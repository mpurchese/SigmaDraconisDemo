namespace SigmaDraconis.Config
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Draconis.Shared;
    using Shadows;
    using Smoke;
    using Shared;

    public static class ThingTypeManager
    {
        private static readonly Dictionary<ThingType, ThingTypeDefinition> definitions = new Dictionary<ThingType, ThingTypeDefinition>();
        public static readonly Dictionary<BuildingLayer, ThingType[]> BuildableThingTypesByLayer = new Dictionary<BuildingLayer, ThingType[]>();
        public static ThingType[] BuildableThingTypes;
        public static ThingType[] RepairableThingTypes;
        public static ThingType[] PlantThingTypes;
        public static HashSet<ThingType> SelectableThingTypes;
        public static HashSet<ThingType> DeconstructableThingTypes;

        private delegate ThingTypeDefinition ParseDelegate(string fileName, int lineNumber, string[] fields);

        public static void Load()
        {
            var buildableThingTypes = new List<ThingType>();
            buildableThingTypes.AddRange(LoadBuildingDefinitions("Buildings"));
            buildableThingTypes.AddRange(LoadBuildingDefinitions("Furniture"));
            LoadPlantAndRockDefinitions("Plants");
            LoadPlantAndRockDefinitions("Rocks");
            LoadGeneralDefinitions("Animals");
            LoadGeneralDefinitions("ResourceStacks");

            BuildableThingTypes = buildableThingTypes.ToArray();
            RepairableThingTypes = definitions.Where(kv => kv.Value.FramesToBreak > 0).Select(kv => kv.Key).ToArray();
            PlantThingTypes = definitions.Where(kv => kv.Value.IsPlant).Select(kv => kv.Key).ToArray();
            foreach (var layer in buildableThingTypes.Select(t => definitions[t].BuildingLayer).Distinct())
            {
                BuildableThingTypesByLayer.Add(layer, buildableThingTypes.Where(d => definitions[d].BuildingLayer == layer).ToArray());
            }

            var nonSelectableTypes = new HashSet<ThingType> { ThingType.None, ThingType.ConduitMajor, ThingType.ConduitMinor, ThingType.Bird1, ThingType.Bird2, ThingType.Fish, ThingType.Rocket, ThingType.RocketGantry, ThingType.Roof };
            SelectableThingTypes = definitions.Keys.Except(nonSelectableTypes).ToHashSet();

            var nonDeconstructableTypes = new HashSet<ThingType> { ThingType.None, ThingType.BlueBug, ThingType.RedBug, ThingType.ConduitMajor, ThingType.ConduitMinor, ThingType.Bird1, ThingType.Bird2, ThingType.Fish, ThingType.SnowTortoise };
            DeconstructableThingTypes = definitions.Keys.Except(nonDeconstructableTypes).ToHashSet();
        }

        private static List<ThingType> LoadBuildingDefinitions(string folder)
        {
            var thingTypes = new List<ThingType>();
            var path = Path.Combine("Config", folder);
            var files = Directory.GetFiles(path, "*.txt");
            foreach (var file in files)
            {
                using (var sr = File.OpenText(file))
                {
                    var lineNumber = 0;
                    var thingType = ThingType.None;
                    var buildingLayer = BuildingLayer.Normal;
                    var rendererTypes = new List<RendererType>();
                    var rendererLayers = new List<int>();
                    var tileBlockModel = TileBlockModel.None;
                    var blocksConstruction = true;
                    var optionalFoundation = false;
                    var isLaunchPadRequired = false;
                    var isRocketGantryRequired = false;
                    var isCookerRequired = false;
                    var isTableRequired = false;
                    var canRecycle = true;
                    var foundationsRequired = 0;
                    var adjacentCoastTilesRequired = 0;
                    var coastTilesRequired = 0;
                    var canBuildIndoor = false;
                    var canBuildOutdoor = false;
                    var windBlockFactor = 0;
                    var soundFileName = (string)null;
                    var soundVolume = 0f;
                    var soundFade = 0.02f;
                    var energyCost = 0.0;
                    var metalCost = 0;
                    var stoneCost = 0;
                    var batteryCellsCost = 0;
                    var compositesCost = 0;
                    var solarCellsCost = 0;
                    var glassCost = 0;
                    var compostCost = 0;
                    var timeCost = 1;
                    var canRotate = false;
                    var defaultDirection = Direction.SE;
                    var autoRotate = false;
                    var isNameable = true;
                    var sizeX = 1;
                    var sizeY = 1;
                    var conduitType = ThingType.None;
                    var failed = false;
                    var framesToBreak = 0;
                    var minTemperature = -100;
                    var isAdjacentBuildAllowed = true;
                    var isBuildBuildByCoastAllowed = true;

                    while (!sr.EndOfStream && !failed)
                    {
                        lineNumber++;
                        try
                        {
                            var line = sr.ReadLine();
                            if (line.Contains('#')) line = line.Substring(0, line.IndexOf('#'));
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var fields = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            switch (fields[0])
                            {
                                case "TYPE":
                                    if (!Enum.TryParse(fields[1], out thingType))
                                    {
                                        Logger.Instance.Log("ThingTypeManager", $"Warning: Did not load file {file} - ThingType {fields[1]} not recognosed.");
                                        Logger.Instance.Flush();
                                        failed = true;
                                    }
                                    break;
                                case "SIZE":
                                    sizeX = int.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                                    sizeY = int.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                                    break;
                                case "LAYER":
                                    buildingLayer = (BuildingLayer)Enum.Parse(typeof(BuildingLayer), fields[1]);
                                    break;
                                case "RENDERER":
                                    rendererTypes.Add((RendererType)Enum.Parse(typeof(RendererType), fields[1]));
                                    var layer = (fields.Length >= 4 && fields[2] == "LAYER") ? int.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture) : 1;
                                    rendererLayers.Add(layer);
                                    break;
                                case "BLOCKSCONSTRUCTION": blocksConstruction = fields[1].ToUpperInvariant() == "TRUE"; break;
                                case "TILEBLOCK": tileBlockModel = (TileBlockModel)Enum.Parse(typeof(TileBlockModel), fields[1]); break;
                                case "RECYCLABLE": canRecycle = fields[1].ToUpperInvariant() == "TRUE"; break;
                                case "ROTATABLE": 
                                    canRotate = fields[1].ToUpperInvariant() == "TRUE";
                                    if (fields.Length >= 4 && fields[2] == "DEFAULTDIRECTION") defaultDirection = (Direction)Enum.Parse(typeof(Direction), fields[3]);
                                    break;
                                case "ALLOWADJACENTBUILD": isAdjacentBuildAllowed = fields[1].ToUpperInvariant() == "TRUE"; break;
                                case "ALLOWBUILDBYCOAST": isBuildBuildByCoastAllowed = fields[1].ToUpperInvariant() == "TRUE"; break;
                                case "AUTOROTATE": autoRotate = fields[1].ToUpperInvariant() == "TRUE"; break;
                                case "NAMEABLE": isNameable = fields[1].ToUpperInvariant() == "TRUE"; break;
                                case "BUILDABLE":
                                    for (int i = 1; i < fields.Length; i++)
                                    {
                                        switch (fields[i])
                                        {
                                            case "INSIDE": canBuildIndoor = true; break;
                                            case "OUTSIDE": canBuildOutdoor = true; break;
                                        }
                                    }
                                    break;
                                case "REPAIRABLE":
                                    if (fields.Length == 3 && fields[1] == "HOURSTOBREAK") framesToBreak = int.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture) * 3600;
                                    break;
                                case "TEMPERATURE":
                                    if (fields.Length == 3 && fields[1] == "MIN") minTemperature = int.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                                    break;
                                case "COST":
                                    for (int i = 1; i < fields.Length; i++)
                                    {
                                        switch (fields[i])
                                        {
                                            case "ENERGY": energyCost = double.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                            case "METAL": metalCost = int.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                            case "STONE": stoneCost = int.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                            case "BATTERYCELLS": batteryCellsCost = int.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                            case "COMPOSITES": compositesCost = int.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                            case "SOLARCELLS": solarCellsCost = int.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                            case "GLASS": glassCost = int.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                            case "COMPOST": compostCost = int.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                            case "TIME": timeCost = int.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                        }
                                    }
                                    break;
                                case "REQUIRES":
                                    for (int i = 1; i < fields.Length; i += 2)
                                    {
                                        switch (fields[i])
                                        {
                                            case "FOUNDATION": foundationsRequired = int.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                            case "COAST": coastTilesRequired = int.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                            case "ADJACENTCOAST": adjacentCoastTilesRequired = int.Parse(fields[i + 1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                            case "COOKER": isCookerRequired = true; break;
                                            case "LAUNCHPAD": isLaunchPadRequired = true; break;
                                            case "ROCKETGANTRY": isRocketGantryRequired = true; break;
                                            case "TABLE": isTableRequired = true; break;
                                        }
                                    }
                                    break;
                                case "OPTIONAL":
                                    if (fields[1] == "FOUNDATION") optionalFoundation = true;
                                    break;
                                case "CONDUIT": conduitType = (ThingType)Enum.Parse(typeof(ThingType), fields[1]); break;
                                case "WINDBLOCK": windBlockFactor = int.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                case "SOUND":
                                    soundFileName = fields[1];
                                    soundVolume = float.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture); 
                                    if (fields.Length == 6) soundFade = float.Parse(fields[5], NumberStyles.Any, CultureInfo.InvariantCulture);
                                    break;
                                case "BEGIN":
                                    if (fields[1] == "SHADOW")
                                    {
                                        var shadowLines = new List<string>();
                                        while (!sr.EndOfStream)
                                        {
                                            var l = sr.ReadLine();
                                            if (l.StartsWith("END SHADOW")) break;
                                            shadowLines.Add(l);
                                        }

                                        if (shadowLines.Any()) ShadowManager.Load(thingType, shadowLines);
                                    }
                                    else if (fields[1] == "SMOKE")
                                    {
                                        var smokeLines = new List<string>();
                                        while (!sr.EndOfStream)
                                        {
                                            var l = sr.ReadLine();
                                            if (l.StartsWith("END SMOKE")) break;
                                            smokeLines.Add(l);
                                        }

                                        if (smokeLines.Any()) SmokeManager.Load(thingType, smokeLines);
                                    }
                                    break;
                            }
                        }
                        catch
                        {
                            throw new Exception($"Error on line {lineNumber} of {file}");
                        }
                    }

                    var definition = new ThingTypeDefinition()
                    {
                        BuildingLayer = buildingLayer,
                        RendererTypes = rendererTypes,
                        RendererLayers = rendererLayers,
                        TileBlockModel = tileBlockModel,
                        BlocksConstruction = blocksConstruction,
                        OptionalFoundation = optionalFoundation,
                        CanRecycle = canRecycle,
                        FoundationsRequired = foundationsRequired,
                        AdjacentCoastTilesRequired = adjacentCoastTilesRequired,
                        CoastTilesRequired = coastTilesRequired,
                        CanBeOutdoor = canBuildOutdoor,
                        CanBeIndoor = canBuildIndoor,
                        WindBlockFactor = windBlockFactor,
                        EnergyCost = Energy.FromKwH(energyCost),
                        ConstructionTimeMinutes = timeCost,
                        Size = new Vector2i(sizeX, sizeY),
                        CanBuild = canBuildIndoor || canBuildOutdoor,
                        CanRotate = canRotate,
                        DefaultBuildDirection = defaultDirection,
                        AutoRotate = autoRotate,
                        IsNameable = isNameable,
                        IsAdjacentBuildAllowed = isAdjacentBuildAllowed,
                        IsBuildByCoastAllowed = isBuildBuildByCoastAllowed,
                        IsCookerRequired = isCookerRequired,
                        IsLaunchPadRequired = isLaunchPadRequired,
                        IsRocketGantryRequired = isRocketGantryRequired,
                        IsTableRequired = isTableRequired,
                        ConduitType = conduitType,
                        FramesToBreak = framesToBreak,
                        MinTemperature = minTemperature,
                        SoundFileName = soundFileName,
                        SoundVolume = soundVolume,
                        SoundFade = soundFade
                    };

                    definition.ConstructionCosts.Add(ItemType.Metal, metalCost);
                    definition.ConstructionCosts.Add(ItemType.Stone, stoneCost);
                    definition.ConstructionCosts.Add(ItemType.BatteryCells, batteryCellsCost);
                    definition.ConstructionCosts.Add(ItemType.Composites, compositesCost);
                    definition.ConstructionCosts.Add(ItemType.SolarCells, solarCellsCost);
                    definition.ConstructionCosts.Add(ItemType.Glass, glassCost);
                    definition.ConstructionCosts.Add(ItemType.Compost, compostCost);

                    definitions.Add(thingType, definition);
                    thingTypes.Add(thingType);
                }
            }

            return thingTypes;
        }

        private static void LoadPlantAndRockDefinitions(string folder)
        {
            var path = Path.Combine("Config", folder);
            if (!Directory.Exists(path)) return;

            var files = Directory.GetFiles(path, "*.txt");
            foreach (var file in files)
            {
                using (var sr = File.OpenText(file))
                {
                    var lineNumber = 0;
                    var thingType = ThingType.None;
                    var isPlant = false;
                    var rendererTypes = new List<RendererType>();
                    var rendererLayers = new List<int>();
                    var tileBlockModel = TileBlockModel.Circle;
                    var blocksConstruction = true;
                    var canBeIndoors = true;
                    var canRotate = false;
                    var canRecycle = true;
                    var windBlockFactor = 0;
                    var sizeX = 1;
                    var sizeY = 1;
                    int? cropDefinitionId = null;

                    while (!sr.EndOfStream)
                    {
                        lineNumber++;
                        try
                        {
                            var line = sr.ReadLine();
                            if (line.Contains('#')) line = line.Substring(0, line.IndexOf('#'));
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var fields = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            switch (fields[0])
                            {
                                case "TYPE":
                                    thingType = (ThingType)Enum.Parse(typeof(ThingType), fields[1]);
                                    break;
                                case "CLASS":
                                    if (fields[1].ToLowerInvariant() == "plant") isPlant = true;
                                    break;
                                case "SIZE":
                                    sizeX = int.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                                    sizeY = int.Parse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                                    break;
                                case "RENDERER":
                                    rendererTypes.Add((RendererType)Enum.Parse(typeof(RendererType), fields[1]));
                                    var layer = (fields.Length >= 4 && fields[2] == "LAYER") ? int.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture) : 1;
                                    rendererLayers.Add(layer);
                                    break;
                                case "ROTATABLE": canRotate = fields[1].ToUpperInvariant() == "TRUE"; break;
                                case "BLOCKSCONSTRUCTION": blocksConstruction = fields[1].ToUpperInvariant() == "TRUE"; break;
                                case "TILEBLOCK": tileBlockModel = (TileBlockModel)Enum.Parse(typeof(TileBlockModel), fields[1]); break;
                                case "RECYCLABLE": canRecycle = fields[1].ToUpperInvariant() == "TRUE"; break;
                                case "WINDBLOCK": windBlockFactor = int.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                case "CROPTYPE": cropDefinitionId = int.Parse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture); break;
                                case "CANBEINDOORS": canBeIndoors = fields[1].ToUpperInvariant() == "TRUE"; break;
                                case "BEGIN":
                                    if (fields[1] == "SHADOW")
                                    {
                                        var shadowLines = new List<string>();
                                        while (!sr.EndOfStream)
                                        {
                                            var l = sr.ReadLine();
                                            if (l.StartsWith("END SHADOW")) break;
                                            shadowLines.Add(l);
                                        }

                                        if (shadowLines.Any()) ShadowManager.Load(thingType, shadowLines);
                                    }
                                    break;
                            }
                        }
                        catch
                        {
                            throw new Exception($"Error on line {lineNumber} of {file}");
                        }
                    }

                    var definition = new ThingTypeDefinition()
                    {
                        IsPlant = isPlant,
                        RendererTypes = rendererTypes,
                        RendererLayers = rendererLayers,
                        TileBlockModel = tileBlockModel,
                        BlocksConstruction = blocksConstruction,
                        CanRotate = canRotate,
                        CanRecycle = canRecycle,
                        CanBeIndoor = canBeIndoors,
                        CropDefinitionId = cropDefinitionId,
                        WindBlockFactor = windBlockFactor,
                        Size = new Vector2i(sizeX, sizeY)
                    };

                    definitions.Add(thingType, definition);
                }
            }
        }

        private static void LoadGeneralDefinitions(string folder)
        {
            var path = Path.Combine("Config", folder);
            if (!Directory.Exists(path)) return;

            var files = Directory.GetFiles(path, "*.txt");
            foreach (var file in files)
            {
                using (var sr = File.OpenText(file))
                {
                    var lineNumber = 0;
                    var thingType = ThingType.None;
                    var rendererTypes = new List<RendererType>();
                    var rendererLayers = new List<int>();
                    var tileBlockModel = TileBlockModel.None;
                    var optionalFoundation = false;

                    while (!sr.EndOfStream)
                    {
                        lineNumber++;
                        try
                        {
                            var line = sr.ReadLine();
                            if (line.Contains('#')) line = line.Substring(0, line.IndexOf('#'));
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            var fields = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            switch (fields[0])
                            {
                                case "TYPE":
                                    thingType = (ThingType)Enum.Parse(typeof(ThingType), fields[1]);
                                    break;
                                case "RENDERER":
                                    rendererTypes.Add((RendererType)Enum.Parse(typeof(RendererType), fields[1]));
                                    var layer = (fields.Length >= 4 && fields[2] == "LAYER") ? int.Parse(fields[3], NumberStyles.Any, CultureInfo.InvariantCulture) : 1;
                                    rendererLayers.Add(layer);
                                    break;
                                case "TILEBLOCK": tileBlockModel = (TileBlockModel)Enum.Parse(typeof(TileBlockModel), fields[1]); break;
                                case "OPTIONAL":
                                    if (fields[1] == "FOUNDATION") optionalFoundation = true;
                                    break;
                                case "BEGIN":
                                    if (fields[1] == "SHADOW")
                                    {
                                        var shadowLines = new List<string>();
                                        while (!sr.EndOfStream)
                                        {
                                            var l = sr.ReadLine();
                                            if (l.StartsWith("END SHADOW")) break;
                                            shadowLines.Add(l);
                                        }

                                        if (shadowLines.Any()) ShadowManager.Load(thingType, shadowLines);
                                    }
                                    break;
                            }
                        }
                        catch
                        {
                            throw new Exception($"Error on line {lineNumber} of {file}");
                        }
                    }

                    var definition = new ThingTypeDefinition()
                    {
                        RendererTypes = rendererTypes,
                        RendererLayers = rendererLayers,
                        TileBlockModel = tileBlockModel,
                        CanRecycle = false,
                        Size = new Vector2i(1, 1),
                        OptionalFoundation = optionalFoundation,
                    };

                    definitions.Add(thingType, definition);
                }
            }
        }

        public static ThingTypeDefinition GetDefinition(ThingType thingType, bool exceptionIfMissing = true)
        {
            if (!definitions.ContainsKey(thingType))
            {
                if (exceptionIfMissing) throw new Exception($"Missing definition for '{Enum.GetName(typeof(ThingType), thingType)}'");
                return null;
            }

            return definitions[thingType];
        }

        //public static string GetDisplayName(ThingType thingType)
        //{
        //    if (!definitions.ContainsKey(thingType)) return "";
        //    return definitions[thingType].DisplayName;
        //}

        //public static string GetDisplayNamePlural(ThingType thingType)
        //{
        //    if (!definitions.ContainsKey(thingType)) return "";
        //    var plural = definitions[thingType].DisplayNamePlural;
        //    if (string.IsNullOrEmpty(plural)) plural = definitions[thingType].DisplayName + "s";
        //    return plural;
        //}

        public static Energy GetEnergyCost(ThingType thingType)
        {
            if (!definitions.ContainsKey(thingType)) return 0;
            return definitions[thingType].EnergyCost;
        }

        public static bool IsRendererType(ThingType thingType, RendererType rendererType)
        {
            return definitions.ContainsKey(thingType) && definitions[thingType].RendererTypes.Contains(rendererType);
        }
    }
}
