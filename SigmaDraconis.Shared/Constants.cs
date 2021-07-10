namespace SigmaDraconis.Shared
{
    using System.Collections.Generic;

    public class Constants
    {
        public const double FramesToConstructConduitMinor = 60;

        public const double LanderEnergyStorage = 20;
        public const int LanderResourceCapacity = 32;
        public const int LanderFoodCapacity = 8;
        public const int LanderItemsCapacity = 8;

        public const double SolarPanelEnergyProduction = 4;
        public const double LanderSolarPanelEnergyProduction = 4;

        public const double WindTurbineEnergyProduction = 10;
        public const double WindTurbineMinWind = 4;
        public const double WindTurbineMaxWind = 14;

        public const double BiolabEnergyUsage = 1f;
        public const double BiolabMinStartEnergy = 0.1f;
        public const double GeologyLabEnergyUsage = 1f;
        public const double GeologyLabMinStartEnergy = 0.1f;
        public const double MaterialsLabEnergyUsage = 1f;
        public const double MaterialsLabMinStartEnergy = 0.1f;

        public const double LampEnergyUsage = 1f;

        public const double FuelFactoryProductionRate = 1.0f;
        public const double FuelFactoryEnergyUsage = 20f;
        public const double FuelFactoryMinStartEnergy = 0.2f;

        public const double HydroponicsShelfEnergyUse = 1;
        public const double HydroponicsShelfMinStartEnergy = 0.01f;

        public const double SmelterProductionRate = 4.0f;
        public const double SmelterEnergyUsage = 10f;
        public const double SmelterMinStartEnergy = 0.1f;

        public const double BatteryEnergyStorage = 25;

        // Colonist
        public const double ColonistThirstRate = 1 / 480.0;
        public const double ColonistHungerRate = 1 / 960.0;
        public const double ColonistSleepingThirstRate = 1 / 960.0;
        public const double ColonistSleepingHungerRate = 1 / 1920.0;
        public const double ColonistStopWorkTiredness = 60;
        public const double ColonistStartSleepTiredness = 75;
        public const double ColonistStartSleepNoPodTiredness = 95;
        public const double ColonistStressRateWorking = 1.0;
        public const double ColonistStressRateWorkingWorkaholic = 0.9;
        public const double ColonistStressRateNotWorking = -3.0;
        public const double ColonistStressRateDrinkingKek = -15.0;
        public const int ColonistFramesToDrink = 100;
        public const int ColonistFramesToDrinkKek = 3600;
        public const int ColonistFramesKekHappiness = 3600 * 24;
        public const double ColonistDrinkHydrationPerFrame = 0.4;
        public const int ColonistFramesToEat = 200;
        public const double ColonistEatNourishmentPerFrame = 0.2;
        public const int ColonistMaxNormalWorkFrames = 12 * 3600;   // More than this is considered "long hours" and will reduce happiness
        public const int ColonistNewArrivalHappiness = 6;
        public const int ColonistNewArrivalBonusHours = 200;
        public const int ColonistNewColonyBonusHours = 200;
        public const int ColonistRoamCardTimeout = 3600 * 24;
        public const int ColonistRoamFramesForCard = 3600;
        public const int ColonistSocialCardTimeout = 3600 * 24;
        public const int ColonistSocialFramesForCard = 3600;
        public const int DeadColonistOrganicsYield = 4;
        public const int DeadColonistRecycleTime = 300;

        // Charcoal Maker
        public const int CharcoalMakerFramesToInitialise = 360;
        public const int CharcoalMakerFramesToProcess = 14400;
        public const int CharcoalMakerFramesToPauseResume = 360;

        // Cooker
        public const int CookerFramesToProcess = 1800;
        public const int CookerWaterUse = 100;
        public const double CookerEnergyUse = 4.0;
        public const double CookerMinStartEnergy = 0.4f;

        // Kek Factory
        public const int KekFactoryFramesToProcess = 1800;
        public const int KekFactoryWaterUse = 100;
        public const double KekFactoryEnergyUse = 4.0;
        public const double KekFactoryMinStartEnergy = 0.4f;

        // Fuel Factory
        public const int FuelFactoryFramesToProcess = 3600;
        public const int FuelFactoryFramesToPauseResume = 30;
        public const double FuelFactoryEnergyUse = 20.0;
        public const double FuelFactoryEnergyStore = 1.0;

        // Generator
        public const double GeneratorEnergyOutputCoal = 8.5f;
        public const double GeneratorEnergyOutputOrganics = 6f;
        public const double GeneratorEnergyOutputLiquidFuel = 9.5f;
        public const double GeneratorWaterUsePerKwH = 25.0;
        public const int GeneratorFramesToProcessCoal = 2400;
        public const int GeneratorFramesToProcessOrganics = 2400;
        public const int GeneratorFramesToProcessLiquidFuel = 7200;
        public const int GeneratorFramesToInitialise = 360;
        public const int GeneratorFramesToPauseResume = 360;

        // Biomass Power Plant
        public const double BiomassPowerEnergyOutput = 9f;
        public const int BiomassPowerFramesToProcessOrganics = 2400;
        public const int BiomassPowerFramesToProcessMush = 1536;
        public const int BiomassPowerFramesToInitialise = 720;
        public const int BiomassPowerFramesToPauseResume = 720;
        public const double BiomassPowerWaterUsePerKwH = 20.0;

        // Coal Power Plant
        public const double CoalPowerEnergyOutput = 18f;
        public const int CoalPowerFramesToProcess = 1400;
        public const int CoalPowerFramesToInitialise = 360;
        public const int CoalPowerFramesToPauseResume = 360;
        public const double CoalPowerWaterUsePerKwH = 20.0;

        // Hydrogen Burner
        public const double HydrogenBurnerEnergyOutput = 38f;
        public const int HydrogenBurnerFramesToProcess = 1800;
        public const int HydrogenBurnerFramesToInitialise = 128;
        public const int HydrogenBurnerFramesToPauseResume = 128;
        public const double HydrogenBurnerWaterUsePerKwH = 10.0;

        // Mine
        public const int MineFramesToProcessNoSurvey = 3600;
        public const int MineFramesToProcessVeryLowDensity = 3600;  // 1.0/hr
        public const int MineFramesToProcessLowDensity = 2250;      // 1.6/hr
        public const int MineFramesToProcessMediumDensity = 1636;   // 2.2/hr
        public const int MineFramesToProcessHighDensity = 1200;     // 3.0/hr
        public const int MineFramesToProcessVeryHighDensity = 1000; // 3.6/hr
        public const int MineFramesToPauseResume = 90;
        public const double MineEnergyUse = 8.0;
        public const double MineEnergyStore = 0.4;

        // Stone Furnace
        public const int StoneFurnaceFramesToInitialise = 360;
        public const int StoneFurnaceFramesToProcess = 7200;
        public const int StoneFurnaceFramesToPauseResume = 360;

        // Electric Furnace
        public const int ElectricFurnaceFramesToInitialise = 360;
        public const int ElectricFurnaceFramesToProcess = 1800;
        public const int ElectricFurnaceFramesToPauseResume = 360;
        public const double ElectricFurnaceEnergyUse = 8.0;
        public const double ElectricFurnaceEnergyStore = 0.4;

        // Mush Factory
        public const int MushFactoryFramesToProcess = 3600;
        public const int MushFactoryFramesToPauseResume = 360;
        public const double MushFactoryEnergyUse = 4.0;
        public const double MushFactoryEnergyStore = 0.2;
        public const double MushFactoryMinTemperature = -10;
        public const double MushFactoryTemperatureChangePerFrame = 0.01;
        public const int MushFactoryWaterUse = 100;

        // Battery Cell Factory
        public const int BatteryCellFactoryFramesToProcess = 3600;
        public const int BatteryCellFactoryFramesToPauseResume = 36;
        public const double BatteryCellFactoryEnergyUse = 3.0;
        public const double BatteryCellFactoryEnergyStore = 0.1;

        // Solar Cell Factory
        public const int SolarCellFactoryFramesToProcess = 7200;
        public const int SolarCellFactoryFramesToPauseResume = 72;
        public const double SolarCellFactoryEnergyUse = 8.0;
        public const double SolarCellFactoryEnergyStore = 0.4;

        // Composites Factory
        public const int CompositesFactoryFramesToProcess = 7200;
        public const int CompositesFactoryFramesToPauseResume = 72;
        public const double CompositesFactoryEnergyUse = 4.0;
        public const double CompositesFactoryEnergyStore = 0.2;

        // Glass Factory
        public const int GlassFactoryFramesToProcess = 3600;
        public const int GlassFactoryFramesToPauseResume = 36;
        public const double GlassFactoryEnergyUse = 4.0;
        public const double GlassFactoryEnergyStore = 0.2;

        // Compost Factory
        public const int CompostFactoryFramesToProcess = 14400;
        public const int CompostFactoryFramesToProcessImproved = 10800;
        public const int CompostFactoryFramesToPauseResume = 72;
        public const double CompostFactoryEnergyUse = 0.2;
        public const double CompostFactoryEnergyStore = 0.02;

        // Food Dispenser
        public const int FoodDispenserFramesToPrepare = 100;

        // Food Storage
        public const int FoodStorageCapacity = 16;

        // Kek Dispenser
        public const int KekDispenserFramesToPrepare = 60;
        public const int KekDispenserFramesToUse = 120;

        // Resource Processor
        public const int ResourceProcessorFramesToProcess = 240;

        // Water Dispenser
        public const int WaterDispenserFramesToPrepare = 100;

        // WaterPump
        public const int WaterPumpFramesToProcess = 18;
        public const int WaterPumpFramesToPauseResume = 60;
        public const double WaterPumpEnergyUse = 1.0;
        public const double WaterPumpEnergyStore = 0.02;
        public const int WaterPumpMinTemperature = -10;
        public const int WaterPumpCapacity = 100;
        public const double WaterPumpTemperatureChangePerFrame = 0.02;

        // Shore Pump
        public const int ShorePumpFramesToProcess = 9;
        public const int ShorePumpWaterGenRate = 400;
        public const int ShorePumpFramesToPauseResume = 60;
        public const double ShorePumpEnergyUse = 1.0;
        public const double ShorePumpEnergyStore = 0.02;
        public const int ShorePumpMinTemperature = -10;
        public const int ShorePumpCapacity = 100;
        public const double ShorePumpTemperatureChangePerFrame = 0.02;

        // Ground Water
        public const int MaxGroundWaterWet = 5600;
        public const int MaxGroundWaterNormal = 4000;
        public const int MaxGroundWaterDry = 2000;
        public const int GroundWaterReplenishRate = 60;   // Chances out of 1000 for each tile gaining 1 water each minute (60 is approx 500 hours)

        // Algae Pool
        public const int AlgaePoolWaterUse = 400;
        public const double AlgaeGrowthRate = 1 / 36000.0;
        public const double AlgaeGrowthRateImproved = 1 / 28800.0;
        public const int AlgaeYield = 4;
        public const int AlgaeYieldImproved = 5;

        // Ore Scanner
        public const double OreScannerEnergyUse = 2.0;
        public const double OreScannerEnergyStore = 0.1;
        public static int OreScannerFramesPerTileBasic = 1800;
        public static int OreScannerFramesPerTileImproved = 1350;
        public static int OreScannerRangeBasic = 6;
        public static int OreScannerRangeImproved = 8;

        // Misc
        public const int SiloCapacity = 16;
        public const int HydrogenStorageCapacity = 16;
        public const int ItemsStorageCapacity = 16;
        public const int WaterStorageCapacity = 800;
        public const int StartHour = 60;
        public const float FramesPerHour = 3600f;
        public const int MaxTilesPerRoom = 99;
        public const int FramesToSurveyTileResources = 1800;
        public const int RocketFuelToLaunch = 80;
        public const int MaxColonists = 10;
        public const StackingAreaMode DefaultStackingAreaMode = StackingAreaMode.TargetStackSize;
        public const int DefaultStackingAreaTargetCount = 10;

        public const int TerrainOverlayNoResourceColourR = 0;
        public const int TerrainOverlayNoResourceColourG = 0;
        public const int TerrainOverlayNoResourceColourB = 0;
        public const int TerrainOverlayCoalColourR = 0;
        public const int TerrainOverlayCoalColourG = 0;
        public const int TerrainOverlayCoalColourB = 0;
        public const int TerrainOverlayIronOreColourR = 172;
        public const int TerrainOverlayIronOreColourG = 40;
        public const int TerrainOverlayIronOreColourB = 0;
        public const int TerrainOverlayStoneColourR = 96;
        public const int TerrainOverlayStoneColourG = 96;
        public const int TerrainOverlayStoneColourB = 96;
        public const int TerrainOverlayStoneSnowColourR = 64;
        public const int TerrainOverlayStoneSnowColourG = 64;
        public const int TerrainOverlayStoneSnowColourB = 64;

        public const int MushFoodType = 999;

        public const float WaterColourR = 0.2f;
        public const float WaterColourG = 0.15f;
        public const float WaterColourB = 0.1f;

        public const int HoursToWakeColonist = 10;
        public const int HoursBetweenColonistWakes = 8;

        // Logging
        public const bool IsResourceStackLoggingEnabled = false;

        public static Dictionary<ItemType, ThingType> ResourceStackTypes = new Dictionary<ItemType, ThingType>
        {
            { ItemType.Biomass, ThingType.OrganicsStack },
            { ItemType.IronOre, ThingType.IronOreStack },
            { ItemType.Coal, ThingType.CoalStack },
            { ItemType.Metal, ThingType.IronStack },
            { ItemType.Stone, ThingType.StoneStack },
            { ItemType.Compost, ThingType.CompostStack }
        };

        public static Dictionary<ThingType, ItemType> ItemTypesByResourceStackType = new Dictionary<ThingType, ItemType>
        {
            { ThingType.OrganicsStack, ItemType.Biomass },
            { ThingType.IronOreStack, ItemType.IronOre },
            { ThingType.CoalStack, ItemType.Coal },
            { ThingType.IronStack, ItemType.Metal },
            { ThingType.StoneStack, ItemType.Stone },
            { ThingType.CompostStack, ItemType.Compost }
        };

        public static Dictionary<ItemType, int> ResourceStackMaxSizes = new Dictionary<ItemType, int>
        {
            { ItemType.Biomass, 20 },
            { ItemType.IronOre, 10 },
            { ItemType.Coal, 10 },
            { ItemType.Metal, 20 },
            { ItemType.Stone, 10 },
            { ItemType.Compost, 10 }
        };

        public static Dictionary<ItemType, ThingType> StorageTypesByItemType = new Dictionary<ItemType, ThingType>
        {
            { ItemType.None, ThingType.None },
            { ItemType.Metal, ThingType.Silo },
            { ItemType.Biomass, ThingType.Silo },
            { ItemType.IronOre, ThingType.Silo },
            { ItemType.LiquidFuel, ThingType.HydrogenStorage },
            { ItemType.Food, ThingType.FoodStorage },
            { ItemType.Coal, ThingType.Silo },
            { ItemType.Stone, ThingType.Silo },
            { ItemType.Mush, ThingType.FoodStorage },
            { ItemType.Kek, ThingType.FoodStorage },
            { ItemType.BatteryCells, ThingType.ItemsStorage },
            { ItemType.Composites, ThingType.ItemsStorage },
            { ItemType.Compost, ThingType.Silo },
            { ItemType.SolarCells, ThingType.ItemsStorage },
            { ItemType.Glass, ThingType.ItemsStorage },
            { ItemType.Water, ThingType.WaterStorage }
        };

        public static int[,] OreScanRadiusMap = new int[11, 11]
        {
            {  0,  0,  0,  0,  0, 10, 10, 10, 10, 10, 10 },
            {  0,  0,  0, 10, 10, 10,  9,  9,  9,  9,  9 },
            {  0,  0, 10, 10,  9,  9,  9,  8,  8,  8,  8 },
            {  0, 10, 10,  9,  9,  8,  8,  8,  7,  7,  7 },
            {  0, 10,  9,  9,  8,  8,  7,  7,  6,  6,  6 },
            { 10, 10,  9,  8,  8,  7,  6,  6,  5,  5,  5 },
            { 10,  9,  9,  8,  7,  6,  6,  5,  5,  4,  4 },
            { 10,  9,  8,  8,  7,  6,  5,  4,  4,  4,  3 },
            { 10,  9,  8,  7,  6,  5,  5,  4,  3,  3,  2 },
            { 10,  9,  8,  7,  6,  5,  4,  4,  3,  2,  1 },
            { 10,  9,  8,  7,  6,  5,  4,  3,  2,  1,  1 }
        };
    }
}
