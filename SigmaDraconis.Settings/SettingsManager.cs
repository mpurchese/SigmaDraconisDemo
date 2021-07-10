namespace SigmaDraconis.Settings
{
    using Draconis.Shared;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Shared;

    public static class SettingsManager
    {
        private static readonly Dictionary<string, string> defaultKeySettings = new Dictionary<string, string>
        {
            { "+", "GameSpeed:Increase" },
            { "Escape", "Options" },
            { "Space", "TogglePause" },
            { "-", "GameSpeed:Decrease" },
            { "Left", "Scroll:Left" },
            { "Right", "Scroll:Right" },
            { "Down", "Scroll:Down" },
            { "Up", "Scroll:Up" },
            { "A", "Scroll:Left" },
            { "B", "Construct" },
            { "C", "Locate:Colonist" },
            { "D", "Scroll:Right" },
            { "E", "RotateBlueprint:Left" },
            { "F", "Farm" },
            { "H", "Harvest" },
            { "G", "Geology" },
            { "L", "Locate:Lander" },
            { "M", "ResourceMap" },
            { "N", "Mothership" },
            { "O", "ToggleRoof" },
            { "R", "RotateBlueprint:Right" },
            { "S", "Scroll:Down" },
            { "T", "CameraTrack" },
            { "W", "Scroll:Up" },
            { "X", "Deconstruct" },
            { "[", "Zoom:In" },
            { "]", "Zoom:Out" },
            { "F1", "Help" },
            { "F11", "ToggleFullScreen" },
            { "shift-C", "Build:Conduit" },
            { "shift-D", "Build:Door" },
            { "shift-F", "Build:FoundationStone" },
            { "shift-G", "Build:Generator" },
            { "shift-M", "Build:Mine" },
            { "shift-O", "Build:ResourceProcessor" },
            { "shift-R", "Build:Roof" },
            { "shift-W", "Build:Wall" },
            { "ctrl-C", "CommentArchive" },
            { "ctrl-D", "Debug" },
            { "ctrl-F", "ToggleFrameRate" },
            { "ctrl-S", "Screenshot" },
            { "ctrl-T", "Temperature" },
            { "ctrl-Z", "ToggleHomeZone" },
            { "ctrl-F12", "ToggleUI" },
            { "ctrl-shift-S", "ScreenshotNoUI" }
        };

        private static bool isLoaded;
        private static Dictionary<SettingGroup, Dictionary<string, string>> settings = new Dictionary<SettingGroup, Dictionary<string, string>>();
        private static readonly List<SettingGroup> settingGroups = new List<SettingGroup> { SettingGroup.Graphics, SettingGroup.Sound, SettingGroup.Keys, SettingGroup.Misc };
        private static readonly Dictionary<string, List<string>> keysForActions = new Dictionary<string, List<string>>();

        // Shortcuts to commonly used settings
        public static int FullScreenSizeX => GetSettingInt(SettingGroup.Graphics, SettingNames.FullScreenSizeX).GetValueOrDefault(1920);
        public static int FullScreenSizeY => GetSettingInt(SettingGroup.Graphics, SettingNames.FullScreenSizeY).GetValueOrDefault(1080);

        public static TemperatureUnit TemperatureUnit { get; private set; }
        public static SpeedUnit SpeedUnit { get; private set; }

        public static event EventHandler<EventArgs> SettingsSaved;

        public static void Reload()
        {
            isLoaded = false;
            settings.Clear();
            keysForActions.Clear();
            Load();
        }

        public static string GetSetting(SettingGroup group, string name)
        {
            if (!isLoaded) Load();
            if (!settings.ContainsKey(group)) return null;
            if (!settings[group].ContainsKey(name)) return null;
            return settings[group][name];
        }

        public static int? GetSettingInt(SettingGroup group, string name)
        {
            if (!isLoaded) Load();
            if (!settings.ContainsKey(group)) return null;
            if (!settings[group].ContainsKey(name)) return null;
            if (!int.TryParse(settings[group][name], out int result)) return null;
            return result;
        }

        public static bool? GetSettingBool(SettingGroup group, string name)
        {
            if (!isLoaded) Load();
            if (!settings.ContainsKey(group)) return null;
            if (!settings[group].ContainsKey(name)) return null;
            if (!bool.TryParse(settings[group][name], out bool result)) return null;
            return result;
        }

        public static void SetSetting(SettingGroup group, string name, string value)
        {
            if (!isLoaded) Load();
            if (!settings.ContainsKey(group)) settings.Add(group, new Dictionary<string, string>());
            if (!settings[group].ContainsKey(name)) settings[group].Add(name, value);
            else settings[group][name] = value;

            if (group == SettingGroup.Keys)
            {
                keysForActions.Clear();
                foreach (var kv in settings[SettingGroup.Keys])
                {
                    if (!keysForActions.ContainsKey(kv.Value)) keysForActions.Add(kv.Value, new List<string>());
                    keysForActions[kv.Value].Add(kv.Key);
                }
            }
        }

        public static void SetSetting(SettingGroup group, string name, int value)
        {
            if (!isLoaded) Load();
            if (!settings.ContainsKey(group)) settings.Add(group, new Dictionary<string, string>());
            if (!settings[group].ContainsKey(name)) settings[group].Add(name, value.ToString());
            else settings[group][name] = value.ToString();
        }

        public static void SetSetting(SettingGroup group, string name, bool value)
        {
            if (!isLoaded) Load();
            if (!settings.ContainsKey(group)) settings.Add(group, new Dictionary<string, string>());
            if (!settings[group].ContainsKey(name)) settings[group].Add(name, value.ToString());
            else settings[group][name] = value.ToString();
        }

        public static string GetKeySetting(string keyName, bool isAlt, bool isCtrl, bool isShift)
        {
            var sb = new StringBuilder();
            if (isAlt) sb.Append("alt-");
            if (isCtrl) sb.Append("ctrl-");
            if (isShift) sb.Append("shift-");
            sb.Append(keyName);

            return GetSetting(SettingGroup.Keys, sb.ToString());
        }

        public static void RemoveKeySetting(string key)
        {
            if (settings[SettingGroup.Keys].ContainsKey(key))
            {
                var action = settings[SettingGroup.Keys][key];
                if (keysForActions.ContainsKey(action) && keysForActions[action].Contains(key)) keysForActions[action].Remove(key);
                settings[SettingGroup.Keys].Remove(key);
            }
        }

        private static void Load()
        {
            LoadDefaultSettings();

            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var path = Path.Combine(dir, "Settings.txt");
                if (File.Exists(path))
                {
                    using (var sr = File.OpenText(path))
                    {
                        var isGroupHeader = false;
                        var isItemName = false;
                        var isItemValue = false;
                        var groupHeader = "";
                        var itemName = "";
                        var itemValue = "";

                        var line1 = sr.ReadLine();
                        if (line1?.StartsWith("Version") == true)
                        {
                            bool isValid = false;
                            var version = new string[] { "0", "0", "0"};
                            try
                            {
                                // Key bindings reset in v0.0.18
                                version = line1.Split(' ')[1].Replace(':', '.').Split('.');   // Replace because string format changed in 0.0.22
                                if (int.Parse(version[0]) > 0 || int.Parse(version[1]) > 0 || int.Parse(version[2]) >= 23) isValid = true;
                            }
                            catch { }

                            if (isValid)
                            {
                                while (!sr.EndOfStream)
                                {
                                    char c = (char)sr.Read();
                                    if (c == '\r' || c == '\n')
                                    {
                                        if (groupHeader != "" && itemName != "") AddSetting(groupHeader, itemName, itemValue);
                                        isGroupHeader = false;
                                        isItemName = true;
                                        isItemValue = false;
                                        itemName = "";
                                        itemValue = "";
                                    }
                                    else if (c == '[' && !isItemValue)
                                    {
                                        isGroupHeader = true;
                                        isItemName = false;
                                        groupHeader = "";
                                    }
                                    else if (c == ']' && isGroupHeader)
                                    {
                                        isGroupHeader = false;
                                    }
                                    else if (c == '=')
                                    {
                                        isItemName = false;
                                        isItemValue = true;
                                        if (isGroupHeader && groupHeader == "")
                                        {
                                            // Special case: '[' followed by '=' is an item name, not a group header
                                            isGroupHeader = false;
                                            groupHeader = "Keys";
                                            itemName = "[";
                                        }
                                    }
                                    else if (isGroupHeader)
                                    {
                                        groupHeader += c;
                                    }
                                    else if (isItemName)
                                    {
                                        itemName += c;
                                    }
                                    else if (isItemValue && (c != ' ' || itemValue != ""))
                                    {
                                        itemValue += c;
                                    }
                                }

                                // In case there is no new-line at end of file
                                if (groupHeader != "" && itemName != "") AddSetting(groupHeader, itemName, itemValue);
                            }

                            if (int.Parse(version[0]) == 0 && int.Parse(version[1]) <= 2 && int.Parse(version[2]) == 0)
                            {
                                // Default window size changed in v0.2.1
                                try
                                {
                                    settings[SettingGroup.Graphics][SettingNames.WindowScreenSizeX] = "1600";
                                    settings[SettingGroup.Graphics][SettingNames.WindowScreenSizeY] = "900";
                                }
                                catch { }
                            }
                        }
                    }
                }

                Save();  // Save here to merge default settings with saved settings (in case of first run or game upgrade)
            }
            catch
            {
                // TODO: Maybe display message that the settings file is invalid and that default settings will be used?
            }

            // Action key dictionary
            foreach (var kv in settings[SettingGroup.Keys])
            {
                if (!keysForActions.ContainsKey(kv.Value)) keysForActions.Add(kv.Value, new List<string>());
                keysForActions[kv.Value].Add(kv.Key);
            }

            isLoaded = true;

            TemperatureUnit = GetSetting(SettingGroup.Misc, SettingNames.TemperatureUnit) == "F" ? TemperatureUnit.F : TemperatureUnit.C;

            var speedSetting = GetSetting(SettingGroup.Misc, SettingNames.WindSpeedUnit);
            switch (speedSetting)
            {
                case "mps": SpeedUnit = SpeedUnit.Mps; break;
                case "mph": SpeedUnit = SpeedUnit.Mph; break;
                case "kph": SpeedUnit = SpeedUnit.Kph; break;
            }
        }

        private static void AddSetting(string groupName, string name, string value)
        {
            if (groupName == "Graphics" && name == "FullScreen") return;   // Don't load fullscreen, this can cause problems.
            if (groupName == "CtrlKeys" || groupName == "ShiftKeys" || groupName == "ShiftCtrlKeys") return;   // Obsolete
            if (groupName == "Keys" && name == "Escape") return;   // Obsolete

            var group = (SettingGroup)Enum.Parse(typeof(SettingGroup), groupName);
            if (!settings.ContainsKey(group)) settings.Add(group, new Dictionary<string, string>());

            // Keys defined in setting file overwrite defaults
            if (group == SettingGroup.Keys)
            {
                foreach (var key in settings[SettingGroup.Keys].Where(s => s.Value.ToLowerInvariant() == value.ToLowerInvariant()).Select(s => s.Key).Where(k => !k.In("Left", "Right", "Up", "Down")).ToList()) settings[SettingGroup.Keys].Remove(key);
            }

            if (settings[group].ContainsKey(name)) settings[group][name] = value;
            else settings[group].Add(name, value);
        }

        public static void Save()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var path = Path.Combine(dir, "Settings.txt");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (var sr = File.CreateText(path))
            {
                sr.WriteLine("Version " + GameVersion.CurrentGameVersion.ToString());
                sr.WriteLine("");
                foreach (var group in settingGroups)
                {
                    sr.WriteLine($"[{group.ToString()}]");
                    foreach (var setting in settings[group])
                    {
                        sr.WriteLine($"{setting.Key}={setting.Value}");
                    }

                    sr.WriteLine("");
                }
            }

            if (isLoaded)
            {
                TemperatureUnit = GetSetting(SettingGroup.Misc, SettingNames.TemperatureUnit) == "F" ? TemperatureUnit.F : TemperatureUnit.C;

                var speedSetting = GetSetting(SettingGroup.Misc, SettingNames.WindSpeedUnit);
                switch (speedSetting)
                {
                    case "mps": SpeedUnit = SpeedUnit.Mps; break;
                    case "mph": SpeedUnit = SpeedUnit.Mph; break;
                    case "kph": SpeedUnit = SpeedUnit.Kph; break;
                }
            }

            SettingsSaved?.Invoke(null, new EventArgs());
        }

        public static List<string> GetKeysForAction(string action)
        {
            return keysForActions.ContainsKey(action) ? keysForActions[action] : new List<string>();
        }

        public static IEnumerable<string> GetDefaultKeysForAction(string action)
        {
            foreach (var kv in defaultKeySettings)
            {
                if (kv.Value == action) yield return kv.Key;
            }
        }

        public static string GetFirstKeyForAction(string action)
        {
            return GetKeysForAction(action).FirstOrDefault();
        }

        private static void LoadDefaultSettings()
        {
            settings = new Dictionary<SettingGroup, Dictionary<string, string>>();
            foreach (var group in settingGroups)
            {
                settings.Add(group, new Dictionary<string, string>());
            }

            var graphicsSettings = new Dictionary<string, string>
            {
                { SettingNames.FullScreenSizeX, "1920" },
                { SettingNames.FullScreenSizeY, "1080" },
                { SettingNames.WindowScreenSizeX, "1600" },
                { SettingNames.WindowScreenSizeY, "900" },
                { SettingNames.FullScreenPositionX, "0" },
                { SettingNames.FullScreenPositionY, "0" },
                { SettingNames.IsFullScreen, "false" },
                { SettingNames.UIScaling, ((int)UIScaleSettings.Maximum).ToString() },
                { SettingNames.TextureRes, "1" },
                { SettingNames.ShadowDetail, "2" },
                { SettingNames.EnableTerrainGrid, false.ToString() }
            };

            var soundSettings = new Dictionary<string, string>
            {
                { SettingNames.MusicVolume, "20" },
                { SettingNames.SoundVolume, "80" }
            };

            var miscSettings = new Dictionary<string, string>
            {
                { SettingNames.MaxGameSpeed, "8" },
                { SettingNames.LatestGameID, "0" },
                { SettingNames.AutosaveMaxCount, "8" },
                { SettingNames.AutosaveAtStart, "true" },
                { SettingNames.Language, "English" },
                { SettingNames.TemperatureUnit, "C" },
                { SettingNames.WindSpeedUnit, "mps" }
            };

            settings = new Dictionary<SettingGroup, Dictionary<string, string>>
            {
                {SettingGroup.Graphics, graphicsSettings },
                {SettingGroup.Sound, soundSettings },
                {SettingGroup.Keys, defaultKeySettings.ToDictionary(kv => kv.Key, kv => kv.Value) },
                {SettingGroup.Misc, miscSettings }
            };
        }
    }
}
