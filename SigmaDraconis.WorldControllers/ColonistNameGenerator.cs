namespace SigmaDraconis.WorldControllers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Shared;
    using World;
    using WorldInterfaces;

    public static class ColonistNameGenerator
    {
        private static bool isLoaded = false;
        private static List<string> names = new List<string>();

        public static string GetNextName(string exclude = null)
        {
            if (!isLoaded) Load();

            var index = Rand.Next(names.Count);
            while ((World.GetThings<IColonist>(ThingType.Colonist).Any(c => c.ShortName == names[index]) && World.GetThings<IColonist>(ThingType.Colonist).Count() < names.Count)
                || (MothershipController.GetColonistPlaceholders().Any(c => c.Name == names[index]) && MothershipController.GetColonistPlaceholders().Count() < names.Count)
                || (names[index] == exclude && World.GetThings<IColonist>(ThingType.Colonist).Count() < names.Count - 1))
            {
                index = Rand.Next(names.Count);
            }

            return names[index];
        }

        public static string GetNextName(List<string> exclude)
        {
            if (!isLoaded) Load();

            var index = Rand.Next(names.Count);
            while (exclude.Contains(names[index]))
            {
                index = Rand.Next(names.Count);
            }

            return names[index];
        }

        private static void Load()
        {
            var path = Path.Combine("Config", "Language", "English", "ColonistNames.txt");
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

                        names.Add(line);
                    }
                    catch
                    {
                        throw new Exception($"Error on line {lineNumber} of {path}");
                    }
                }
            }
        }
    }
}
