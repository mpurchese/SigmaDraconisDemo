namespace SigmaDraconis.Language
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Draconis.Shared;
    using Draconis.UI;
    using Cards.Interface;
    using Shared;

    public static class LanguageManager
    {
        public static string CurrentLanguage { get; private set; } = "";
        public static int CurrentLanguageId { get; private set; } = 0;

        public static bool IsCapitalNouns = false;

        private const string defaultLanguage = "English";
        
        private static readonly Dictionary<string, int> allLanguages = new Dictionary<string, int>();

        private static readonly Dictionary<string, string> numberAndDateStrings = new Dictionary<string, string>();
        private static readonly TwoKeyDictionary<Type, int, string> enumStrings = new TwoKeyDictionary<Type, int, string>();
        private static readonly Dictionary<CardType, string> cardNames = new Dictionary<CardType, string>();
        private static readonly Dictionary<int, string> foodNames = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> foodNamesHuman = new Dictionary<int, string>();
        private static readonly Dictionary<CardType, string> cardDescriptions = new Dictionary<CardType, string>();
        private static readonly Dictionary<int, string> projectNames = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> projectDescriptions = new Dictionary<int, string>();
        private static readonly Dictionary<ThingType, string> thingTypeNames = new Dictionary<ThingType, string>();
        private static readonly Dictionary<ThingType, string> thingTypeNamesPlural = new Dictionary<ThingType, string>();
        private static readonly Dictionary<ThingType, string> thingTypeShortNames = new Dictionary<ThingType, string>();
        private static readonly Dictionary<ThingType, string> thingTypeDescriptions = new Dictionary<ThingType, string>();
        private static readonly Dictionary<WarningType, string> warningsSingular = new Dictionary<WarningType, string>();
        private static readonly Dictionary<WarningType, string> warningsPlural = new Dictionary<WarningType, string>();

        public static string GetNumberOrDate(string key)
        {
            return numberAndDateStrings.ContainsKey(key) ? numberAndDateStrings[key] : "";
        }

        public static string GetCardName(CardType cardType)
        {
            return cardNames.ContainsKey(cardType) ? cardNames[cardType] : "";
        }

        public static string GetName(ThingType thingType, bool plural = false)
        {
            if (plural && thingTypeNamesPlural.ContainsKey(thingType)) return thingTypeNamesPlural[thingType];
            return thingTypeNames.ContainsKey(thingType) ? thingTypeNames[thingType] : thingType.ToString();
        }

        public static string GetNameLower(ThingType thingType, bool plural = false)
        {
            if (IsCapitalNouns) return GetName(thingType, plural);

            if (plural && thingTypeNamesPlural.ContainsKey(thingType)) return thingTypeNamesPlural[thingType].ToLowerInvariant();
            return thingTypeNames.ContainsKey(thingType) ? thingTypeNames[thingType].ToLowerInvariant() : thingType.ToString().ToLowerInvariant();
        }

        public static string GetShortName(ThingType thingType)
        {
            return thingTypeShortNames.ContainsKey(thingType) ? thingTypeShortNames[thingType] : GetName(thingType);
        }

        public static string GetShortNameLower(ThingType thingType)
        {
            if (IsCapitalNouns) return GetShortName(thingType);
            return thingTypeShortNames.ContainsKey(thingType) ? thingTypeShortNames[thingType] : GetName(thingType);
        }

        public static string GetDescription(ThingType thingType)
        {
            return thingTypeDescriptions.ContainsKey(thingType) ? thingTypeDescriptions[thingType] : "";
        }

        public static string GetCardName(CardType cardType, object arg0)
        {
            return cardNames.ContainsKey(cardType) ? string.Format(cardNames[cardType], arg0) : "";
        }

        public static string GetFoodName(int foodType)
        {
            return foodNames.ContainsKey(foodType) ? foodNames[foodType] : "Food";
        }

        public static string GetFoodNameHuman(int foodType)
        {
            return foodNamesHuman.ContainsKey(foodType) ? foodNamesHuman[foodType] : "";
        }

        public static string GetProjectDescription(int projectId)
        {
            return projectDescriptions[projectId];
        }

        public static string GetProjectName(int projectId)
        {
            return projectNames[projectId];
        }

        public static string GetCardDescription(CardType cardType, object arg0)
        {
            return cardDescriptions.ContainsKey(cardType) ? string.Format(cardDescriptions[cardType], arg0) : "";
        }

        public static string GetCardDescription(CardType cardType, object arg0, object arg1)
        {
            return cardDescriptions.ContainsKey(cardType) ? string.Format(cardDescriptions[cardType], arg0, arg1) : "";
        }

        public static string GetCardDescription(CardType cardType, params object[] args)
        {
            return cardDescriptions.ContainsKey(cardType) ? string.Format(cardDescriptions[cardType], args) : "";
        }

        public static string GetWarningSingular(WarningType warningType, string name)
        {
            return warningsSingular.ContainsKey(warningType) ? string.Format(warningsSingular[warningType], name) : "";
        }

        public static string GetWarningPlural(WarningType warningType, int name)
        {
            return warningsPlural.ContainsKey(warningType) ? string.Format(warningsPlural[warningType], name) : "";
        }

        public static string Get(Type enumType, object value)
        {
            try
            {
                return enumStrings[enumType, (int)value];
            }
            catch { return ""; };
        }

        public static string Get<T>(object value)
        {
            try
            {
                return enumStrings[typeof(T), (int)value];
            }
            catch { return ""; };
        }

        public static string Get<T>(object value, object arg0)
        {
            try
            {
                return string.Format(enumStrings[typeof(T), (int)value], arg0);
            }
            catch { return ""; };
        }

        public static string Get<T>(object value, object arg0, object arg1)
        {
            try
            {
                return string.Format(enumStrings[typeof(T), (int)value], arg0, arg1);
            }
            catch { return ""; };
        }

        public static string Get<T>(object value, object arg0, object arg1, object arg2)
        {
            try
            {
                return string.Format(enumStrings[typeof(T), (int)value], arg0, arg1, arg2);
            }
            catch { return ""; };
        }

        public static string Get<T>(object value, object arg0, object arg1, object arg2, object arg3)
        {
            try
            {
                return string.Format(enumStrings[typeof(T), (int)value], arg0, arg1, arg2, arg3);
            }
            catch { return ""; };
        }

        public static string Get<T>(object value, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            try
            {
                return string.Format(enumStrings[typeof(T), (int)value], arg0, arg1, arg2, arg3, arg4);
            }
            catch { return ""; };
        }

        public static string Get<T>(object value, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            try
            {
                return string.Format(enumStrings[typeof(T), (int)value], arg0, arg1, arg2, arg3, arg4, arg5);
            }
            catch { return ""; };
        }

        public static string Get(Type enumType, object value, object arg0)
        {
            try
            {
                return string.Format(enumStrings[enumType, (int)value], arg0);
            }
            catch { return ""; };
        }

        public static IEnumerable<string> GetAvailableLanguages()
        {
            if (!allLanguages.Any())
            {
                var root = Path.Combine("Config", "Language");
                var id = 1;
                foreach (var name in Directory.EnumerateDirectories(root).Where(d => Directory.GetFiles(d).Any()).Select(d => Path.GetFileName(d))
                    .OrderBy(d => string.Compare(d, defaultLanguage, StringComparison.InvariantCultureIgnoreCase) == 0 ? 0 : 1)
                    .ThenBy(d => d))
                {
                    allLanguages.Add(name, id);
                    id++;
                }
            }

            return allLanguages.Keys;
        }

        public static void Load(string language)
        {
            if (CurrentLanguage == language) return;

            numberAndDateStrings.Clear();
            enumStrings.Clear();
            cardNames.Clear();
            foodNames.Clear();
            foodNamesHuman.Clear();
            cardDescriptions.Clear();
            projectNames.Clear();
            projectDescriptions.Clear();
            thingTypeDescriptions.Clear();
            thingTypeNames.Clear();
            thingTypeNamesPlural.Clear();
            thingTypeShortNames.Clear();
            warningsSingular.Clear();
            warningsPlural.Clear();

            // Set current language and check folder exists
            CurrentLanguage = language;
            var path = Path.Combine("Config", "Language", CurrentLanguage);
            if (!Directory.Exists(path))
            {
                LogError($"Language folder for {CurrentLanguage} not found.  Switching to {defaultLanguage}.");
                CurrentLanguage = defaultLanguage;
            }

            GetAvailableLanguages();
            UIStatics.CurrentLanguageId = allLanguages[CurrentLanguage];
            CurrentLanguageId = UIStatics.CurrentLanguageId;
            IsCapitalNouns = CurrentLanguage == "Deutsch";

            Safe(LoadGeneral);
            Safe(LoadNumbersAndDates);
            Safe(LoadCards);
            Safe(LoadFoodNames);
            Safe(LoadProjects);
            Safe(LoadThings);
            Safe(LoadWarnings);
        }

        private static void Safe(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                LogError($"Language file load failed: {ex.Message}");
            }
        }

        private static void LoadGeneral()
        {
            var path = Path.Combine("Config", "Language", CurrentLanguage, "UI.txt");
            using (var sr = File.OpenText(path))
            {
                var lineNumber = 0;
                while (!sr.EndOfStream)
                {
                    lineNumber++;
                    try
                    {
                        var line = sr.ReadLine();
                        if (line.Contains('#')) line = line.Substring(0, line.IndexOf('#'));
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var fields = line.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length >= 3)
                        {
                            var typeName = "SigmaDraconis.Language.StringsFor" + fields[0];
                            var type = Type.GetType(typeName);
                            if (type == null)
                            {
                                typeName = "SigmaDraconis.Shared." + fields[0] + ", SigmaDraconis.Shared";
                                type = Type.GetType(typeName);
                            }

                            enumStrings.Add(type, (int)Enum.Parse(type, fields[1], true), fields[2].Replace("_comma_", ",").Replace("_hash_", "#"));
                        }
                    }
                    catch
                    {
                        LogError($"Error on line {lineNumber} of {path}");
                    }
                }
            }
        }

        private static void LoadNumbersAndDates()
        {
            var path = Path.Combine("Config", "Language", CurrentLanguage, "NumbersAndDates.txt");
            using (var sr = File.OpenText(path))
            {
                var lineNumber = 0;
                while (!sr.EndOfStream)
                {
                    lineNumber++;
                    try
                    {
                        var line = sr.ReadLine();
                        if (line.Contains('#')) line = line.Substring(0, line.IndexOf('#'));
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        var fields = line.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length >= 2)
                        {
                            numberAndDateStrings.Add(fields[0], fields[1]);
                        }
                    }
                    catch
                    {
                        LogError($"Error on line {lineNumber} of {path}");
                    }
                }
            }
        }

        private static void LoadCards()
        {
            var path = Path.Combine("Config", "Language", CurrentLanguage, "Cards.txt");
            using (var sr = File.OpenText(path))
            {
                var lineNumber = 0;
                while (!sr.EndOfStream)
                {
                    lineNumber++;
                    try
                    {
                        var line = sr.ReadLine();
                        if (line.Contains('#')) line = line.Substring(0, line.IndexOf('#'));
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = line.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length >= 2)
                        {
                            var parts = fields[0].Split('.');
                            if (parts.Length == 2)
                            {
                                Enum.TryParse(parts[0], out CardType cardType);
                                if (parts[1] == "Name") cardNames.Add(cardType, fields[1].Replace("_comma_", ","));
                                else if (parts[1] == "Desc") cardDescriptions.Add(cardType, fields[1].Replace("_comma_", ","));
                            }
                        }
                    }
                    catch
                    {
                        LogError($"Error on line {lineNumber} of {path}");
                    }
                }
            }
        }

        private static void LoadFoodNames()
        {
            var path = Path.Combine("Config", "Language", CurrentLanguage, "FoodNames.txt");
            using (var sr = File.OpenText(path))
            {
                var lineNumber = 0;
                while (!sr.EndOfStream)
                {
                    lineNumber++;
                    try
                    {
                        var line = sr.ReadLine();
                        if (line.Contains('#')) line = line.Substring(0, line.IndexOf('#'));
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = line.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length >= 2) foodNames.Add(int.Parse(fields[0]), fields[1]);
                        if (fields.Length >= 3) foodNamesHuman.Add(int.Parse(fields[0]), fields[2]);
                    }
                    catch
                    {
                        LogError($"Error on line {lineNumber} of {path}");
                    }
                }
            }
        }

        private static void LoadProjects()
        {
            var path = Path.Combine("Config", "Language", CurrentLanguage, "Projects.txt");
            using (var sr = File.OpenText(path))
            {
                var lineNumber = 0;
                while (!sr.EndOfStream)
                {
                    lineNumber++;
                    try
                    {
                        var line = sr.ReadLine();
                        if (line.Contains('#')) line = line.Substring(0, line.IndexOf('#'));
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = line.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length >= 3)
                        {
                            var id = int.Parse(fields[0]);
                            projectNames.Add(id, fields[1]);
                            projectDescriptions.Add(id, fields[2]);
                        }
                    }
                    catch
                    {
                        LogError($"Error on line {lineNumber} of {path}");
                    }
                }
            }
        }

        private static void LoadThings()
        {
            var path = Path.Combine("Config", "Language", CurrentLanguage, "Things.txt");
            using (var sr = File.OpenText(path))
            {
                var lineNumber = 0;
                ThingType thingType = ThingType.None;
                while (!sr.EndOfStream)
                {
                    lineNumber++;
                    try
                    {
                        var line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                        var spaceIndex = line.IndexOf(' ');
                        var key = line.Substring(0, spaceIndex).ToUpperInvariant();
                        var value = line.Substring(spaceIndex + 1);

                        switch (key)
                        {
                            case "TYPE":
                                thingType = (ThingType)Enum.Parse(typeof(ThingType), value, true);
                                thingTypeShortNames.Add(thingType, thingType.ToString());
                                break;
                            case "DISPLAYNAME":
                                thingTypeNames.Add(thingType, value);
                                break;
                            case "DISPLAYNAMEPLURAL":
                                thingTypeNamesPlural.Add(thingType, value);
                                break;
                            case "SHORTNAME":
                                thingTypeShortNames[thingType] = value;
                                break;
                            case "DESCRIPTION":
                                thingTypeDescriptions.Add(thingType, value);
                                break;
                        }
                    }
                    catch
                    {
                        throw new Exception($"Error on line {lineNumber} of {path}");
                    }
                }
            }
        }

        private static void LoadWarnings()
        {
            var path = Path.Combine("Config", "Language", CurrentLanguage, "Warnings.txt");
            using (var sr = File.OpenText(path))
            {
                var lineNumber = 0;
                while (!sr.EndOfStream)
                {
                    lineNumber++;
                    try
                    {
                        var line = sr.ReadLine();
                        if (line.Contains('#')) line = line.Substring(0, line.IndexOf('#'));
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var fields = line.Split(new[] { "\t" }, StringSplitOptions.RemoveEmptyEntries);
                        if (fields.Length >= 2)
                        {
                            var parts = fields[0].Split('.');
                            if (parts.Length == 2)
                            {
                                Enum.TryParse(parts[0], out WarningType warningType);
                                if (parts[1] == "Single") warningsSingular.Add(warningType, fields[1].Replace("_comma_", ","));
                                else if (parts[1] == "Plural") warningsPlural.Add(warningType, fields[1].Replace("_comma_", ","));
                            }
                        }
                    }
                    catch
                    {
                        LogError($"Error on line {lineNumber} of {path}");
                    }
                }
            }
        }

        private static void LogError(string message)
        {
            Logger.Instance.Log("LanguageManager", $"Error: {message}");
            Logger.Instance.Flush();
        }
    }
}
