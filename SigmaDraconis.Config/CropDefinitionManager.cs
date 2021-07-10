namespace SigmaDraconis.Config
{
    using Language;
    using Shared;
    using System.Collections.Generic;
    using System.Linq;

    public static class CropDefinitionManager
    {
        private static readonly Dictionary<int, CropDefinition> definitions = new Dictionary<int, CropDefinition>();
        private static int currentLanguageId;

        public static void Load()
        {
            AddDefinition(0, 36, 0, false, 128, 128, 128);
            AddDefinition(1, 136, 2, true, 224, 139, 222, 26, -10, 10, 30, 50, 1, true, true);
            AddDefinition(2, 161, 3, true, 41, 160, 219, 20, 6, 14, 26, 34, 1, true, true);
            AddDefinition(3, 186, 4, true, 204, 160, 40, 48, 0, 10, 30, 40, 2, false, true);
            AddDefinition(4, 211, 5, true, 161, 89, 255, 24, 0, 12, 24, 32, 1, true, false);
            AddDefinition(5, 236, 6, true, 104, 168, 113, 42, 8, 32, 38, 50, 2, false, true);
            AddDefinition(6, 236, 7, true, 220, 120, 0, 18, 10, 15, 25, 30, 1, false, true, ThingType.KekFactory, false);
            AddDefinition(100, 86, 7, false, 234, 181, 21, isWildFruit: true);
            AddDefinition(101, 61, 8, false, 40, 60, 255, isWildFruit: true);
            AddDefinition(102, 111, 9, false, 219, 36, 32, isWildFruit: true);
            AddDefinition(110, 261, 10, false, 220, 140, 220, isWildFruit: true);
            AddDefinition(111, 286, 11, false, 255, 100, 0, isWildFruit: true);
            AddDefinition(999, 11, 1, false, 63, 173, 48);

            UpdateLanguage();
        }

        private static void AddDefinition(int id, int animationStartFrame, int iconIndex, bool isCrop,
            int textColourR, int textColourG, int textColourB,
            int hoursToGrow = 0, int minTemp = 0, int minGoodTemp = 0, int maxGoodTemp = 0, int maxTemp = 0,
            int harvestYield = 1, bool canGrowHydroponics = true, bool canGrowSoil = true, ThingType cookerType = ThingType.Cooker, bool canEat = true, bool isWildFruit = false)
        {
            var definition = new CropDefinition()
            {
                Id = id,
                AnimationStartFrame = animationStartFrame,
                IconIndex = iconIndex,
                IsCrop = isCrop,
                TextR = textColourR,
                TextG = textColourG,
                TextB = textColourB,
                HoursToGrow = hoursToGrow,
                MinTemp = minTemp,
                MinGoodTemp = minGoodTemp,
                MaxGoodTemp = maxGoodTemp,
                MaxTemp = maxTemp,
                HarvestYield = harvestYield,
                CanGrowHydroponics = canGrowHydroponics,
                CanGrowSoil = canGrowSoil,
                CookerType = cookerType,
                CanEat = canEat,
                IsWildFruit = isWildFruit
            };

            definitions.Add(id, definition);
        }

        public static IEnumerable<string> GetNames(bool native = false)
        {
            if (currentLanguageId != LanguageManager.CurrentLanguageId) UpdateLanguage();
            return definitions.Values.Where(v => v.IsCrop != native).Select(d => d.DisplayName);
        }

        public static IEnumerable<CropDefinition> GetAll()
        {
            if (currentLanguageId != LanguageManager.CurrentLanguageId) UpdateLanguage();
            return definitions.Values;
        }

        public static CropDefinition GetDefinition(int id)
        {
            if (currentLanguageId != LanguageManager.CurrentLanguageId) UpdateLanguage();
            return definitions.ContainsKey(id) ? definitions[id] : null;
        }

        private static void UpdateLanguage()
        {
            currentLanguageId = LanguageManager.CurrentLanguageId;

            foreach (var definition in definitions.Values)
            {
                definition.DisplayName = LanguageManager.GetFoodName(definition.Id);
                definition.DisplayNameLower = LanguageManager.IsCapitalNouns ? definition.DisplayName : definition.DisplayName.ToLowerInvariant();
                var humanName = LanguageManager.GetFoodNameHuman(definition.Id);
                definition.DisplayNameLong = string.IsNullOrEmpty(humanName) ? definition.DisplayName : $"{definition.DisplayName} ({humanName})";
            }
        }
    }
}
