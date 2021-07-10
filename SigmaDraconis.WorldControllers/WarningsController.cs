namespace SigmaDraconis.WorldControllers
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using Cards.Interface;
    using Config;
    using Language;
    using Shared;
    using World;
    using WorldInterfaces;

    public static class WarningsController
    {
        private static readonly Dictionary<WarningType, HashSet<string>> warningsByType = new Dictionary<WarningType, HashSet<string>>();
        private static readonly List<WarningMessage> displayMessages = new List<WarningMessage>();
        private static readonly Energy minEnergy = Energy.FromKwH(0.5);
        private static bool showPowerWarning = false;
        private static bool showFoodStorageFullWarning = false;
        private static bool showItemStorageFullWarning = false;
        private static bool showSiloWarning = false;
        private static bool showLowFoodWarning = false;
        private static bool showNoFoodWarning = false;
        private static bool showNoCookerWarning = false;
        private static bool showNoKekFactoryWarning = false;
        private static bool showNoKekDispenserWarning = false;

        /// <summary>
        /// Gets or sets flag indicating whether the display needs updating
        /// </summary>
        public static bool IsDisplayInvalidated { get; set; }

        public static bool IsShownWaterPumpNeededWarning { get; private set; }

        /// <summary>
        /// Update every few frames
        /// </summary>
        public static void Update()
        {
            var network = World.ResourceNetwork;
            if (network == null) return;

            var mushChurnTooCold = World.GetThings<IFactoryBuilding>(ThingType.MushFactory).Any(f => f.IsSwitchedOn && f.FactoryStatus == FactoryStatus.TooCold);
            var mushChurnName = LanguageManager.GetName(ThingType.MushFactory);
            if (mushChurnTooCold) Add(WarningType.TooCold, mushChurnName);
            else Remove(WarningType.TooCold, mushChurnName);

            var pumpTooCold = World.GetThings<IFactoryBuilding>(ThingType.WaterPump).Any(f => f.IsSwitchedOn && f.FactoryStatus == FactoryStatus.TooCold);
            var pumpName = LanguageManager.GetName(ThingType.WaterPump);
            if (pumpTooCold) Add(WarningType.TooCold, pumpName);
            else Remove(WarningType.TooCold, pumpName);

            var shorePumpTooCold = World.GetThings<IFactoryBuilding>(ThingType.ShorePump).Any(f => f.IsSwitchedOn && f.FactoryStatus == FactoryStatus.TooCold);
            var shorePumpName = LanguageManager.GetName(ThingType.ShorePump);
            if (shorePumpTooCold) Add(WarningType.TooCold, shorePumpName);
            else Remove(WarningType.TooCold, shorePumpName);

            foreach (var thingType in ThingTypeManager.RepairableThingTypes)
            {
                var needsRepair = World.GetThings<IFactoryBuilding>(thingType).Any(f => f.IsSwitchedOn && (f as IRepairableThing)?.MaintenanceLevel < 0.1);
                if (needsRepair) Add(WarningType.NeedsRepair, LanguageManager.GetName(thingType));
                else Remove(WarningType.NeedsRepair, LanguageManager.GetName(thingType));
            }

            if (IsShownWaterPumpNeededWarning != (World.WorldTime.TotalHoursPassed > 4 && !World.GetThings(ThingType.WaterPump).Any() && !World.GetThings(ThingType.ShorePump).Any()))
            {
                IsShownWaterPumpNeededWarning = !IsShownWaterPumpNeededWarning;
                IsDisplayInvalidated = true;
            }

            var colonists = World.GetThings<IColonist>(ThingType.Colonist).ToList();
            foreach (var colonist in colonists)
            {
                if (colonist.Cards.Contains(CardType.Unhappy)) Add(WarningType.Unhappy, colonist.ShortName);
                else Remove(WarningType.Unhappy, colonist.ShortName);

                if (colonist.Cards.Contains(CardType.VeryUnhappy)) Add(WarningType.VeryUnhappy, colonist.ShortName);
                else Remove(WarningType.VeryUnhappy, colonist.ShortName);

                if (colonist.Cards.Contains(CardType.Cold3)) Add(WarningType.Hypothermia, colonist.ShortName);
                else Remove(WarningType.Hypothermia, colonist.ShortName);
            }

            foreach (var colonist in colonists)
            {
                switch (colonist.StressLevel)
                {
                    case StressLevel.Extreme:
                        Remove(WarningType.WorkloadHigh, colonist.ShortName);
                        Add(WarningType.WorkloadExtreme, colonist.ShortName);
                        break;
                    case StressLevel.High:
                        Add(WarningType.WorkloadHigh, colonist.ShortName);
                        Remove(WarningType.WorkloadExtreme, colonist.ShortName);
                        break;
                    default:
                        Remove(WarningType.WorkloadHigh, colonist.ShortName);
                        Remove(WarningType.WorkloadExtreme, colonist.ShortName);
                        break;
                }

                if (colonist.IsDead || colonist.Body.IsSleeping)
                {
                    Remove(WarningType.NoWater, colonist.ShortName);
                    Remove(WarningType.NoFood, colonist.ShortName);
                }
                else
                {
                    if (colonist.LookingForFoodCounter > 1) Add(WarningType.NoFood, colonist.ShortName);
                    else Remove(WarningType.NoFood, colonist.ShortName);

                    if (colonist.LookingForWaterCounter > 1) Add(WarningType.NoWater, colonist.ShortName);
                    else Remove(WarningType.NoWater, colonist.ShortName);
                }

                if (colonist.IsDead || colonist.Body.IsSleeping || colonist.StressLevel >= StressLevel.High || colonist.Cards.GetEffectsAny(CardEffectType.OnStrike))
                {
                    Remove(WarningType.Idle, colonist.ShortName);
                    Remove(WarningType.WaitingForCooker, colonist.ShortName);
                }
            }

            if (showPowerWarning != (network.EnergyTotal < minEnergy))
            {
                showPowerWarning = !showPowerWarning;
                IsDisplayInvalidated = true;
            }

            
            if (showSiloWarning != (network.ResourcesCapacity <= network.CountResources))
            {
                showSiloWarning = !showSiloWarning;
                IsDisplayInvalidated = true;
            }

            if (showFoodStorageFullWarning != (network.FoodCapacity <= network.CountFood))
            {
                showFoodStorageFullWarning = !showFoodStorageFullWarning;
                IsDisplayInvalidated = true;
            }

            if (showItemStorageFullWarning != (network.ItemsCapacity <= network.CountItems))
            {
                showItemStorageFullWarning = !showItemStorageFullWarning;
                IsDisplayInvalidated = true;
            }

            var food = network.GetItemTotal(ItemType.Food) + network.GetItemTotal(ItemType.Mush);
            if (showLowFoodWarning != (food > 0 && food < colonists.Count))
            {
                showLowFoodWarning = !showLowFoodWarning;
                IsDisplayInvalidated = true;
            }
            if (showNoFoodWarning != (food == 0))
            {
                showNoFoodWarning = !showNoFoodWarning;
                IsDisplayInvalidated = true;
            }

            var missingCooker = !World.GetThings(ThingType.Cooker).Any() && World.GetThings<IPlanter>(ThingType.PlanterHydroponics, ThingType.PlanterStone)
                .Any(p => p.PlanterStatus == PlanterStatus.WaitingToHarvest && CropDefinitionManager.GetDefinition(p.CurrentCropTypeId)?.CookerType == ThingType.Cooker);
            if (showNoCookerWarning != missingCooker)
            {
                showNoCookerWarning = !showNoCookerWarning;
                IsDisplayInvalidated = true;
            }

            var missingKekFactory = !World.GetThings(ThingType.KekFactory).Any() && World.GetThings<IPlanter>(ThingType.PlanterStone)
                .Any(p => p.PlanterStatus == PlanterStatus.WaitingToHarvest && CropDefinitionManager.GetDefinition(p.CurrentCropTypeId)?.CookerType == ThingType.KekFactory);
            if (showNoKekFactoryWarning != missingKekFactory)
            {
                showNoKekFactoryWarning = !showNoKekFactoryWarning;
                IsDisplayInvalidated = true;
            }

            if (showNoKekDispenserWarning != (network.GetItemTotal(ItemType.Kek) > 0 && !World.GetThings(ThingType.KekDispenser).Any()))
            {
                showNoKekDispenserWarning = !showNoKekDispenserWarning;
                IsDisplayInvalidated = true;
            }

        }

        /// <summary>
        /// Add a warning.  Does nothing if already exists.
        /// </summary>
        /// <param name="warningType">The type of warning to add.</param>
        /// <param name="name">Name for format string</param>
        public static void Add(WarningType warningType, string name)
        {
            if (!warningsByType.ContainsKey(warningType))
            {
                warningsByType.Add(warningType, new HashSet<string> { name });
                IsDisplayInvalidated = true;
            }
            else if (warningsByType[warningType] == null)
            {
                warningsByType[warningType] = new HashSet<string> { name };
                IsDisplayInvalidated = true;
            }
            else if (!warningsByType[warningType].Contains(name))
            {
                warningsByType[warningType].Add(name);
                IsDisplayInvalidated = true;
            }
        }

        public static bool Contains(WarningType warningType)
        {
            return warningsByType.ContainsKey(warningType) && warningsByType[warningType]?.Any() == true;
        }

        public static bool Contains(WarningType warningType, string name)
        {
            return warningsByType.ContainsKey(warningType) && warningsByType[warningType].Contains(name) == true;
        }

        /// <summary>
        /// Gets the list of strings to display on screen
        /// </summary>
        /// <returns></returns>
        public static List<WarningMessage> GetDisplayMessages()
        {
            if (IsDisplayInvalidated) UpdateDisplayMessages();
            return displayMessages;
        }

        /// <summary>
        /// Remove a warning.  Does nothing if doesn't exist.
        /// </summary>
        /// <param name="warningType">The type of warning to remove.</param>
        /// <param name="name">Name for format string</param>
        public static void Remove(WarningType warningType, string name)
        {
            if (warningsByType.ContainsKey(warningType) && warningsByType[warningType]?.Contains(name) == true)
            {
                warningsByType[warningType].Remove(name);
                IsDisplayInvalidated = true;
            }
        }

        public static void Clear()
        {
            warningsByType.Clear();
            IsDisplayInvalidated = true;
        }

        public static Dictionary<WarningType, List<string>> Serialize()
        {
            var result = new Dictionary<WarningType, List<string>>();
            foreach (var kv in warningsByType) result.Add(kv.Key, kv.Value?.ToList());
            return result;
        }

        public static void Deserialize(Dictionary<WarningType, List<string>> content)
        {
            warningsByType.Clear();
            foreach (var kv in content) warningsByType.Add(kv.Key, kv.Value?.ToHashSet());
            IsDisplayInvalidated = true;
        }

        private static void UpdateDisplayMessages()
        {
            displayMessages.Clear();

            // TODO: The ordering in this method is important, but it would be better to have some warning severity system

            if (IsShownWaterPumpNeededWarning)
            {
                var name = LanguageManager.GetName(ThingType.WaterPump);
                displayMessages.Add(new WarningMessage(LanguageManager.Get<StringsForWarnings>(StringsForWarnings.BuildingNeeded, name), WarningType.BuildingNeeded));
            }
            if (showFoodStorageFullWarning)
            {
                var name = LanguageManager.GetName(ThingType.FoodStorage);
                displayMessages.Add(new WarningMessage(LanguageManager.Get<StringsForWarnings>(StringsForWarnings.BuildingNeeded, name), WarningType.BuildingNeeded));
            }
            if (showItemStorageFullWarning)
            {
                var name = LanguageManager.GetName(ThingType.ItemsStorage);
                displayMessages.Add(new WarningMessage(LanguageManager.Get<StringsForWarnings>(StringsForWarnings.BuildingNeeded, name), WarningType.BuildingNeeded));
            }
            if (showSiloWarning)
            {
                var name = LanguageManager.GetName(ThingType.Silo);
                displayMessages.Add(new WarningMessage(LanguageManager.Get<StringsForWarnings>(StringsForWarnings.BuildingNeeded, name), WarningType.BuildingNeeded));
            }
            if (showNoCookerWarning)
            {
                var name = LanguageManager.GetName(ThingType.Cooker);
                displayMessages.Add(new WarningMessage(LanguageManager.Get<StringsForWarnings>(StringsForWarnings.BuildingNeeded, name), WarningType.BuildingNeeded));
            }
            if (showNoKekFactoryWarning)
            {
                var name = LanguageManager.GetName(ThingType.KekFactory);
                displayMessages.Add(new WarningMessage(LanguageManager.Get<StringsForWarnings>(StringsForWarnings.BuildingNeeded, name), WarningType.BuildingNeeded));
            }
            if (showNoKekDispenserWarning)
            {
                var name = LanguageManager.GetName(ThingType.KekDispenser);
                displayMessages.Add(new WarningMessage(LanguageManager.Get<StringsForWarnings>(StringsForWarnings.BuildingNeeded, name), WarningType.BuildingNeeded));
            }
            if (showLowFoodWarning) displayMessages.Add(new WarningMessage(LanguageManager.Get<StringsForWarnings>(StringsForWarnings.LowFood), WarningType.LowFoodStorage));
            if (showNoFoodWarning) displayMessages.Add(new WarningMessage(LanguageManager.Get<StringsForWarnings>(StringsForWarnings.NoFood), WarningType.NoFoodStorage));

            if (warningsByType.ContainsKey(WarningType.NeedsRepair) && warningsByType[WarningType.NeedsRepair] != null)
            {
                foreach (var thingType in warningsByType[WarningType.NeedsRepair])
                {
                    displayMessages.Add(new WarningMessage(LanguageManager.Get<StringsForWarnings>(StringsForWarnings.NeedsRepair, thingType), WarningType.NeedsRepair));
                }
            }

            if (warningsByType.ContainsKey(WarningType.TooCold) && warningsByType[WarningType.TooCold] != null)
            {
                foreach (var thingType in warningsByType[WarningType.TooCold])
                {
                    displayMessages.Add(new WarningMessage(LanguageManager.Get<StringsForWarnings>(StringsForWarnings.TooCold, thingType), WarningType.TooCold));
                }
            }

            if (showPowerWarning) displayMessages.Add(new WarningMessage(LanguageManager.Get<StringsForWarnings>(StringsForWarnings.LowPower), WarningType.LowPower));

            if (warningsByType.ContainsKey(WarningType.Hypothermia))
            {
                var strs = warningsByType[WarningType.Hypothermia];
                if (strs?.Count > 1) displayMessages.Add(new WarningMessage(LanguageManager.GetWarningPlural(WarningType.Hypothermia, strs.Count), WarningType.Hypothermia));
                else if (strs?.Count == 1)
                {
                    displayMessages.Add(new WarningMessage(LanguageManager.GetWarningSingular(WarningType.Hypothermia, strs.First()), WarningType.Hypothermia));
                }
            }

            foreach (var kv in warningsByType.Where(k => k.Key != WarningType.Hypothermia))
            {
                if (kv.Value?.Count > 1) displayMessages.Add(new WarningMessage(LanguageManager.GetWarningPlural(kv.Key, kv.Value.Count), kv.Key));
                else if (kv.Value?.Count == 1)
                {
                    displayMessages.Add(new WarningMessage(LanguageManager.GetWarningSingular(kv.Key, kv.Value.First()), kv.Key));
                }
            }
        }
    }
}
