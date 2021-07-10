namespace SigmaDraconis.IO
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;

    using Draconis.Shared;

    using GameSerializer;
    using Settings;
    using Shared;

    public static class SaveGameManager
    {
        public static event EventHandler<EventArgs> Loading;
        public static event EventHandler<GameLoadedEventArgs> Loaded;
        public static event EventHandler<GameLoadFailedEventArgs> LoadFailed;

        public static string LastException;

        private static string GetFolder(bool isAutosave)
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = Path.Combine(dir, "Saves");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (isAutosave)
            {
                dir = Path.Combine(dir, "Autosaves");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }

            return dir;
        }

        public static List<SaveGameDetail> GetSaveFileDetails(bool isAutosaves, out bool linuxFormat)
        {
            MoveAutoavesToSubfolder();  // From 0.0.22, autosaves are located in a subfolder.  Old saves are automatically moved.

            linuxFormat = false;

            var dir = GetFolder(isAutosaves);
            
            var result = new List<SaveGameDetail>();
            foreach (var filePath in Directory.GetFiles(dir).OrderByDescending(d => File.GetLastWriteTime(d)))
            {
                if (filePath.ToLowerInvariant().EndsWith(".sav"))
                {
                    var detail = new SaveGameDetail() { FileDate = File.GetLastWriteTime(filePath) };
                    if (filePath.StartsWith("/"))
                    {
                        // Linux
                        detail.FileName = filePath.Substring(filePath.LastIndexOf("/") + 1, filePath.Length - filePath.LastIndexOf("/") - 5);
                        linuxFormat = true;
                    }
                    else
                    {
                        // Windows
                        detail.FileName = filePath.Substring(filePath.LastIndexOf("\\") + 1, filePath.Length - filePath.LastIndexOf("\\") - 5);
                    }

                    using (var loader = new GameLoader(filePath))
                    {
                        try
                        {
                            loader.LoadHeader();
                            detail.GameVersion = loader.InitialGameVersion ?? loader.GameVersion;
                            detail.WorldTime = loader.Time;
                            result.Add(detail);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.Log("GameLoader", $"Failed to read header of file {filePath}.  Error: {ex.Message}");
                        }
                    }
                }
            }

            return result;
        }

        public static void Save(string fileNameRoot, bool isAutosave, Vector2f scrollPosition, int zoomLevel, int gameScreenWidth, int gameScreenHeight, Dictionary<string, string> additionalProperties)
        {
            string dir = GetFolder(isAutosave);

            var path = Path.Combine(dir, $"{fileNameRoot}.sav");
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (var saver = new GameSaver(path))
            {
                // Scroll position ajdusted for screen size
                var scrollPos = new Vector2i((int)scrollPosition.X + gameScreenWidth, (int)scrollPosition.Y + gameScreenHeight);
                saver.Save(additionalProperties, scrollPos, zoomLevel);
            }
        }

        public static void CleanupAutosaves()
        {
            var dir = GetFolder(true);

            var result = new Dictionary<string, DateTime>();
            foreach (var filePath in Directory.GetFiles(dir))
            {
                result.Add(filePath, File.GetLastWriteTime(filePath));
            }

            var maxAutosaves = SettingsManager.GetSettingInt(SettingGroup.Misc, SettingNames.AutosaveMaxCount) ?? 8;

            while (result.Count > maxAutosaves)
            {
                var oldest = result.OrderBy(r => r.Value).First().Key;
                File.Delete(oldest);
                result.Remove(oldest);
            }
        }

        public static bool CheckSaveGameVersion(string fileNameRoot)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis", "Saves", $"{fileNameRoot}.sav");
            if (File.Exists(path))
            {
                try
                {
                    var fs = new FileStream(path, FileMode.Open);
                    using (var br = new BinaryReader(fs))
                    {
                        var bf = new BinaryFormatter();
                        var version = (GameVersion)bf.Deserialize(fs);
                        if (version.Major == 0 && version.Minor == 0 && version.Build < 23)
                        {
                            throw new NotSupportedException("Sorry, games saved before version 0.0.23|are not supported in this version.");
                        }
                    }
                }
                catch (NotSupportedException ex)
                {
                    LastException = ex.Message;
                    return false;
                }
                catch (Exception)
                {
                    LastException = "Cannot open save game file.";
                    return false;
                }
            }

            return true;
        }

        public static void Load(string fileNameRoot, bool isAutosave, int gameScreenWidth, int gameScreenHeight)
        {
            Loading?.Invoke(null, new EventArgs());

            var additionalProperties = new Dictionary<string, string>();

            string path = isAutosave
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis", "Saves", "Autosaves", $"{fileNameRoot}.sav")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis", "Saves", $"{fileNameRoot}.sav");
            if (File.Exists(path))
            {
                try
                { 
                    using (var loader = new GameLoader(path))
                    {
                        loader.Load();
                        if (Loaded != null)
                        {
                            var scrollPos = new Vector2i(loader.ScrollPosition.X - gameScreenWidth, loader.ScrollPosition.Y - gameScreenHeight);
                            Loaded(null, new GameLoadedEventArgs(loader.GameVersion, scrollPos, loader.ZoomLevel, loader.AdditionalProperties));
                            EventManager.RaiseEvent(EventType.Game, EventSubType.Loaded, loader.GameVersion);
                        }
                    }
                }
                catch (NotSupportedException ex)
                {
                    LoadFailed?.Invoke(null, new GameLoadFailedEventArgs(ex.Message));
                }
                catch (Exception ex)
                {
                    LastException = ex.ToString();
                    LoadFailed?.Invoke(null, new GameLoadFailedEventArgs(""));
                }
            }
        }

        public static void Delete(string fileNameRoot, bool isAutosave)
        {
            string path = isAutosave 
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis", "Saves", "Autosaves", $"{fileNameRoot}.sav")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis", "Saves", $"{fileNameRoot}.sav");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void MoveAutoavesToSubfolder()
        {
            // In v0.0.22, autosave games moved to a subfolder
            var dir1 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis", "Saves");
            if (!Directory.Exists(dir1))
            {
                return;
            }

            var dir2 = Path.Combine(dir1, "Autosaves");
            if (!Directory.Exists(dir2))
            {
                Directory.CreateDirectory(dir2);
            }

            var filesToMove = Directory.GetFiles(dir1);
            foreach (var file in Directory.GetFiles(dir1).Where(f => f.ToLowerInvariant().EndsWith("autosave.sav")))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(dir2, fileName);
                File.Move(file, destFile);
            }
        }
    }
}
