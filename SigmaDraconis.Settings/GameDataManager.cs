namespace SigmaDraconis.Settings
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Shared;

    public static class GameDataManager
    {
        private static readonly HashSet<int> fruitTypesHarvested = new HashSet<int>();

        public static bool IsSnowRegionUnlocked { get; private set; }

        public static void Load()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis", "Data");
                if (!Directory.Exists(dir)) return;

                string path = Path.Combine(dir, "GameData");
                if (!File.Exists(path)) return;

                using (var sr = File.OpenText(path))
                {
                    var line = sr.ReadLine();
                    if (line.Contains("|"))
                    {
                        var data = line.Split('|')[1];
                        if (data.StartsWith("1")) IsSnowRegionUnlocked = true;
                        if (data.Length >= 6)
                        {
                            if (data[1] == '1') fruitTypesHarvested.Add(100);
                            if (data[2] == '1') fruitTypesHarvested.Add(101);
                            if (data[3] == '1') fruitTypesHarvested.Add(102);
                            if (data[4] == '1') fruitTypesHarvested.Add(110);
                            if (data[5] == '1') fruitTypesHarvested.Add(111);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("GameDataManager", "Failed to load game data: " + ex.ToString());
            }
        }

        public static bool AllFruitTypesHarvested()
        {
            return fruitTypesHarvested.Count >= 5;
        }

        public static void FruitHarvested(int cropId)
        {
            if (fruitTypesHarvested.Contains(cropId)) return;

            fruitTypesHarvested.Add(cropId);
            Save();
        }

        public static void UnlockSnowRegion()
        {
            if (IsSnowRegionUnlocked) return;

            IsSnowRegionUnlocked = true;
            Save();
        }

        private static void Save()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                dir = Path.Combine(dir, "Data");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string path = Path.Combine(dir, "GameData");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                using (var sw = File.CreateText(path))
                {
                    var snowRegionUnlocked = IsSnowRegionUnlocked ? "1" : "0";
                    var fruit1Harvested = fruitTypesHarvested.Contains(100) ? "1" : "0";
                    var fruit2Harvested = fruitTypesHarvested.Contains(101) ? "1" : "0";
                    var fruit3Harvested = fruitTypesHarvested.Contains(102) ? "1" : "0";
                    var fruit4Harvested = fruitTypesHarvested.Contains(110) ? "1" : "0";
                    var fruit5Harvested = fruitTypesHarvested.Contains(111) ? "1" : "0";
                    sw.WriteLine($"{GameVersion.CurrentGameVersion}|{snowRegionUnlocked}{fruit1Harvested}{fruit2Harvested}{fruit3Harvested}{fruit4Harvested}{fruit5Harvested}");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("GameDataManager", "Failed to save game data: " + ex.ToString());
            }
        }
    }
}
