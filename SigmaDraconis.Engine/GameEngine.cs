namespace SigmaDraconis.Engine
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using Draconis.Input;
    using Draconis.Shared;
    using Draconis.UI;

    using AI;
    using AnimalAI;
    using CheckList;
    using Commentary;
    using Config;
    using IO;
    using Language;
    using Renderers;
    using Settings;
    using Shared;
    using Sound;
    using UI;
    using UI.Managers;
    using World;
    using World.Projects;
    using World.Zones;
    using WorldControllers;
    using WorldGenerator;
    using WorldInterfaces;

    public class GameEngine : IDisposable
    {
        private readonly GameScreenRenderer gameRenderer;
        private readonly GameScreen gameScreen;

        private bool isPaused;
        private int gameSpeed = 1;
        private int mapSize = 256;        
        private bool isInitialised;
        private bool isNewGame;
        private bool requireStartOfGameAutosave;
        private bool isContentLoaded;
        private int maxGameSpeed;
        private bool prevIsPaused = false;
        private bool resourceOverlayShownBecauseOfContext = false;
        private int currentGameId = 0;
        private readonly Dictionary<ThingType, int> lastSelectedIds = new Dictionary<ThingType, int>();

        private WorldGeneratorBase worldGenerator;

        public bool IsInitializing { get; set; }
        public string FileNameToLoad { get; set; }
        public bool IsFileToLoadAutosave { get; set; }

        public static bool IsLogEnabled { get; set; } = true;

        public event EventHandler<EventArgs> Initialized;
        public event EventHandler<EventArgs> Initializing;
        public event EventHandler<EventArgs> GameLoaded;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.gameRenderer != null) this.gameRenderer.Dispose();
            }
        }

        public static GameEngine Instance { get; private set; }

        public int GameSpeed
        {
            get
            {
                return this.gameSpeed;
            }
            set
            {
                this.gameSpeed = value;
                this.gameScreen.StatusBar.GameSpeed = value;
            }
        }

        public bool IsPaused
        {
            get
            {
                return this.isPaused;
            }
            set
            {
                this.isPaused = value;
                this.gameScreen.StatusBar.IsPaused = value;
            }
        }

        public GameEngine(GameScreen screen)
        {
            if (Instance == null)
            {
                Instance = this;

                this.gameScreen = screen;

                EventManager.Init();

                this.gameRenderer = new GameScreenRenderer(this.gameScreen);
                SaveGameManager.Loading += this.OnGameLoading;
                SaveGameManager.Loaded += this.OnGameLoaded;
                SaveGameManager.LoadFailed += this.OnGameLoadFailed;
                this.gameScreen.AutosaveRequested += this.OnGameScreenAutosaveRequested;
                this.gameScreen.SaveRequested += this.OnGameScreenSaveRequested;
                this.gameScreen.ExitToMainMenuRequest += this.OnGameScreenExitRequested;
                PlayerWorldInteractionManager.CurrentActivityChanged += this.PlayerActivityChanged;

                EventManager.Subscribe(EventType.RocketLaunchClick, delegate (object obj) { this.OnRocketLaunchClick(); });
            }
            else
            {
                throw new ApplicationException("GameEngine already created");
            }
        }

        public void Load()
        {
            ResourceNetworkController.Init();
            BuildingNameController.Init();
            ZoneManager.Init();
            CommentaryController.Load();
            CheckListController.Load();
            GameDataManager.Load();
        }

        public void BeginInitialize(int mapSize, int maxGameSpeed, bool isNewGame = true, ClimateType climateType = ClimateType.Normal)
        {
            this.IsInitializing = true;
            this.mapSize = mapSize;
            this.maxGameSpeed = maxGameSpeed;
            this.isNewGame = isNewGame;
            World.ClimateType = climateType;

            if (isNewGame)
            {
                this.worldGenerator = new WorldGeneratorNormal();
                this.worldGenerator.BeginGenerate();
            }

            this.Initializing?.Invoke(this, new EventArgs());
        }

        private void Initialize()
        {
            Log("Beginning initialisation");

            World.WorldTime.Reset();
            SoundManager.Reset();
            SoundManager.GlobalVolume = (SettingsManager.GetSettingInt(SettingGroup.Sound, SettingNames.SoundVolume) ?? 80) / 100f;

            if (!this.isInitialised)
            {
                Log("Loading ThingTypeManager");
                ThingTypeManager.Load();
                Log("Loading CropDefinitionManager");
                CropDefinitionManager.Load();
                Log("Loading ProjectManager");
                ProjectManager.Init();
                Log("Initialising World");
                World.Init(mapSize, mapSize);
                Log("Initialising WeatherController");
                WeatherController.Init();
                WorldRenderer.Instance.InitShadowMeshRenderers();

                UIStatics.Graphics.RasterizerState = new RasterizerState { CullMode = CullMode.None };

                this.gameScreen.Initialise(mapSize, maxGameSpeed);
                this.gameScreen.PauseGameRequest += this.OnPauseGameRequest;
                this.gameScreen.ResumeGameRequest += this.OnResumeGameRequest;
                this.gameScreen.PauseClick += this.OnPauseButtonClick;
                this.gameScreen.PlayClick += this.OnPlayButtonClick;
                this.gameScreen.TogglePauseClick += this.OnTogglePauseButtonClick;
                this.gameScreen.IncreaseSpeedClick += this.OnIncreaseSpeedClick;
                this.gameScreen.DecreaseSpeedClick += this.OnDecreaseSpeedClick;
                this.gameScreen.ToggleRoofClick += this.OnToggleRoofButtonClick;
                this.gameScreen.ResourceOverlayClick += this.OnResourceOverlayClick;
                this.gameScreen.CropOverlayClick += this.OnCropOverlayClick;
                this.gameScreen.TemperatureOverlayClick += this.OnTemperatureOverlayClick;
                this.gameScreen.ZoneOverlayClick += this.OnZoneOverlayClick;
                this.gameScreen.LocateRequest += this.OnLocateRequest;
                this.gameScreen.ScreenshotRequest += this.OnScreenshotRequest;
                this.gameScreen.ScreenshotNoUIRequest += this.OnScreenshotNoUIRequest;
                this.gameScreen.FarmPanelClose += OnGameScreenFarmPanelRemoved;
            }
            else
            {
                ResourceStackingController.Clear();
                WorldController.Clear(mapSize);
                CommentaryController.Reset();
                CheckListController.Reset();
                TileHighlightManager.Reset();
                SeeThroughTreeManager.Reset();
                ProjectManager.Init();
            }

            if (this.isNewGame)
            {
                ZoneManager.HomeZone.Clear();
                this.worldGenerator.Create();
                ResourceMapController.Init();
                ZoneManager.BuildGlobalZone();
                WeatherController.Update(true);
                CommentaryController.Reset();
                CheckListController.Reset();
                WorldStats.Reset();

                this.gameRenderer.IsLoading = false;

                this.gameScreen.OnNewGame(this.worldGenerator.StartTileIndex);
                this.worldGenerator = null;

                World.WorldLight.Update(World.WorldTime);

                this.GameSpeed = 1;

                this.gameRenderer.IsLoading = false;
                this.gameRenderer.InvalidateBuffers();

                this.currentGameId = (SettingsManager.GetSettingInt(SettingGroup.Misc, SettingNames.LatestGameID) ?? 0) + 1;
                SettingsManager.SetSetting(SettingGroup.Misc, SettingNames.LatestGameID, this.currentGameId);
                SettingsManager.Save();

                if (SettingsManager.GetSettingBool(SettingGroup.Misc, SettingNames.AutosaveAtStart) != false) this.requireStartOfGameAutosave = true;

                EventManager.RaiseEvent(EventType.Game, EventSubType.Loaded, null);
            }
            else
            {
                foreach (var tile in World.BigTiles)
                {
                    tile.UpdateCoords();
                }
            }

            World.WorldLight.Update(World.WorldTime);
            this.isInitialised = true;
            this.IsInitializing = false;
            this.isNewGame = false;
            this.IsPaused = true;
            this.Initialized?.Invoke(this, new EventArgs());
        }

        private void OnLocateRequest(object sender, EventArgs e)
        {
            this.Locate(this.gameScreen.ThingTypeToLocate);
        }

        private void OnScreenshotRequest(object sender, EventArgs e)
        {
            this.gameRenderer.Draw(this.gameScreen.ScrollPosition, this.gameScreen.Zoom, true);
        }

        private void OnScreenshotNoUIRequest(object sender, EventArgs e)
        {
            this.gameRenderer.Draw(this.gameScreen.ScrollPosition, this.gameScreen.Zoom, true, true);
        }

        public void LoadContent()
        {
            if (!this.isContentLoaded)
            {
                this.gameRenderer.LoadContent(UIStatics.Graphics, UIStatics.Content);
                this.gameScreen.LoadContent();
            }

            this.gameRenderer.Update(this.gameScreen.ScrollPosition, this.gameScreen.Zoom, this.IsPaused);
            this.isContentLoaded = true;
        }

        public void Update(bool isRunningSlowly)
        {
            if (this.IsInitializing)
            {
                if (this.isNewGame && this.worldGenerator?.IsReady != true) return;
                this.Initialize();
            }

            var stopWatch = Stopwatch.StartNew();

            MouseManager.Update();
            KeyboardManager.Update();

            if (MouseManager.HasMoved)
            {
                PlayerWorldInteractionManager.OnMouseMoved();
            }

            // Scroll using arrow keys or WASD
            if (KeyboardManager.FocusedElement == this.gameScreen && ModalBackgroundBox.Instance?.IsInteractive != true)
            {
                KeyboardState currentKeyboardState = Keyboard.GetState();
                var scrollSpeed = UIStatics.Graphics.Viewport.Width / (isRunningSlowly ? 50 : 100);
                foreach (var key in currentKeyboardState.GetPressedKeys())
                {
                    if (key.In(Keys.LeftAlt, Keys.LeftControl, Keys.LeftShift, Keys.LeftWindows, Keys.RightAlt, Keys.RightControl, Keys.RightShift, Keys.RightWindows)) continue;

                    var keyName = key.ToString();
                    if (key == Keys.OemOpenBrackets) keyName = "[";
                    else if (key == Keys.OemCloseBrackets) keyName = "]";
                    else if (key == Keys.OemPlus || key == Keys.Add) keyName = "+";
                    else if (key == Keys.OemMinus || key == Keys.Subtract) keyName = "-";

                    if (this.gameScreen.IsOptionsPanelVisible && (key == Keys.Down || key == Keys.Up)) continue;

                    var keySetting = SettingsManager.GetKeySetting(keyName, KeyboardManager.IsAlt, KeyboardManager.IsCtrl, KeyboardManager.IsShift);
                    if (keySetting?.StartsWith("Scroll") != true) continue;

                    switch (keySetting.ToLowerInvariant())
                    {
                        case "scroll:left":
                            this.gameScreen.Scroll(-scrollSpeed, 0);
                            break;
                        case "scroll:right":
                            this.gameScreen.Scroll(scrollSpeed, 0);
                            break;
                        case "scroll:down":
                            this.gameScreen.Scroll(0, scrollSpeed);
                            break;
                        case "scroll:up":
                            this.gameScreen.Scroll(0, -scrollSpeed);
                            break;
                    }
                }
            }

            var worldHour = World.WorldTime.TotalHoursPassed;

            try
            {
                if (!this.IsPaused)
                {
                    // If we are running at 30fps we need to update twice every frame
                    var speed = this.gameSpeed;
                    if (isRunningSlowly && PerfMonitor.UpdateFramesPerSecond > 0 && PerfMonitor.UpdateFramesPerSecond < 33) speed *= 2;

                    for (int i = 0; i < speed; ++i)
                    {
                        WorldController.Update(i == speed - 1, gameScreen.ScrollPosition, this.gameScreen.Zoom, this.gameScreen.W, this.gameScreen.H);
                        ResourceStackingController.Update();
                        ColonistController.Update();
                        AnimalController.Update();
                        ScreenShaker.Update();
                    }

                    CommentaryController.Update();
                    CheckListController.Update();
                }
                else
                {
                    ResourceNetworkController.UpdateStartOfFrame(true);
                }

                SoundManager.Update(this.IsPaused, this.gameScreen.ScrollPosition, this.gameScreen.Zoom, this.gameScreen.W, this.gameScreen.H);
                MusicManager.Update();
                TileHighlightManager.Update();
                SeeThroughTreeManager.Update();

                if (this.gameScreen.Zoom >= 4)
                {
                    this.gameRenderer.Update(this.gameScreen.ScrollPosition + ScreenShaker.CurrentOffset, this.gameScreen.Zoom, this.IsPaused);
                }
                else
                {
                    // Snapping to nearest pixel helps reduce flickering effects whilst in tracking mode, but at high zoom levels it will make the mmotion rough.
                    this.gameRenderer.Update(new Vector2f((int)(this.gameScreen.ScrollPosition.X + ScreenShaker.CurrentOffset.X), (int)(this.gameScreen.ScrollPosition.Y + ScreenShaker.CurrentOffset.Y)), this.gameScreen.Zoom, this.IsPaused);
                }
            }
            catch (Exception ex)
            {
                ExceptionManager.CurrentExceptions.Add(ex);
            }

            if (ExceptionManager.CurrentExceptions.Any())
            {
                Logger.Instance.Log("GameEngine", ExceptionManager.CurrentExceptions[0].ToString());
                ExceptionManager.CurrentExceptions.Clear();
            }

            this.gameScreen.IsPaused = this.IsPaused;
            this.gameScreen.IsRunningSlowly = isRunningSlowly;
            this.gameScreen.Update();
            ResourceNetworkController.UpdateEndOfFrame();

            if ((World.WorldTime.TotalHoursPassed > worldHour && (World.WorldTime.TotalHoursPassed + 1) % 10 == 0) || (this.requireStartOfGameAutosave && World.WorldTime.FrameNumber > 600))
            {
                this.DoAutosave();
                this.requireStartOfGameAutosave = false;
            }

            PerfMonitor.Update(stopWatch.ElapsedTicks);
            Logger.Instance.Flush();
        }

        public void DoBackgroundUpdate()
        {
            AnimalController.DoBackgroundUpdate();
        }

        public bool Draw()
        {
            // Uncomment to slow down draw rate in order to optimise update rate
            //if (isRunningSlowly && !skippedPrefFrame && World.WorldTime.FrameNumber % 2 == 1)
            //{
            //    UIStatics.Graphics.Present();   // A flickering effect on OpenGL if we don't do this
            //    this.skippedPrefFrame = true;
            //    return false;
            //}

            var stopWatch = Stopwatch.StartNew();
            this.gameRenderer.Draw(this.gameScreen.ScrollPosition, this.gameScreen.Zoom);
            PerfMonitor.Draw(stopWatch.ElapsedTicks);
            return true;
        }

        public void ToggleRoofVisibility()
        {
            this.gameRenderer.IsRoofVisible = this.gameScreen.IsRoofVisible;
            this.gameScreen.UpdateHighlights();
        }

        private void OnGameLoading(object sender, EventArgs e)
        {
            this.gameRenderer.InvalidateBuffers();
            this.gameRenderer.ClearShadows();
            this.gameRenderer.IsLoading = true;
            SoundManager.Reset();
        }

        private void OnGameLoaded(object sender, GameLoadedEventArgs e)
        {
            this.gameScreen.OnGameLoaded(e);

            PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            PlayerWorldInteractionManager.SelectedThing = null;

            this.mapSize = World.Width;
            this.GameSpeed = 1;
            ResourceMapController.Init();
            ZoneManager.BuildGlobalZone();
            WorldController.Update(true, gameScreen.ScrollPosition, this.gameScreen.Zoom, this.gameScreen.W, this.gameScreen.H);
            ResourceStackingController.Update();
            ColonistController.Update();
            AnimalController.Update();
            TileHighlightManager.Update();

            var latestGameId = SettingsManager.GetSettingInt(SettingGroup.Misc, SettingNames.LatestGameID) ?? 0;
            this.currentGameId = e.AdditionalProperties.ContainsKey("GameID")
                ? this.currentGameId = int.Parse(e.AdditionalProperties["GameID"])
                : latestGameId + 1;

            SettingsManager.SetSetting(SettingGroup.Misc, SettingNames.LatestGameID, Math.Max(this.currentGameId, latestGameId));
            SettingsManager.Save();

            this.gameRenderer.IsLoading = false;
            this.gameScreen.ModalBackgroundBox.IsInteractive = false;

            this.GameLoaded?.Invoke(this, new EventArgs());
        }

        private void OnGameLoadFailed(object sender, GameLoadFailedEventArgs e)
        {
            this.gameRenderer.IsLoading = false;

            Logger.Instance.Log("GameEngine:OnGameLoadFailed", SaveGameManager.LastException);

            if (e.Reason == "")
            {
                this.gameScreen.ShowErrorDialog();
            }
            else
            {
                this.gameScreen.ShowErrorDialog("GAME LOAD FAILED", e.Reason);
            }
        }

        public void DoAutosave()
        {
            var additionalProperties = new Dictionary<string, string> {{ "GameID", this.currentGameId.ToString() }};
            try
            {
                var hours = World.WorldTime.TotalHoursPassed;
                var day = (hours / WorldTime.HoursInDay) + 1;
                var hour = (hours % WorldTime.HoursInDay) + 1;
                var fileName = LanguageManager.Get<StringsForSystem>(StringsForSystem.AutosaveFileFormat, this.currentGameId, day, hour);
                SaveGameManager.Save(fileName, true, this.gameScreen.ScrollPosition, this.gameScreen.ZoomLevel, this.gameScreen.W, this.gameScreen.H, additionalProperties);
                SaveGameManager.CleanupAutosaves();
            }
            catch
            {
                // Autosave failure shoudn't break anything else
            }
        }

        private void OnGameScreenAutosaveRequested(object sender, EventArgs e)
        {
            this.DoAutosave();
        }

        private void OnGameScreenSaveRequested(object sender, SaveRequestEventArgs e)
        {
            var additionalProperties = new Dictionary<string, string> { { "GameID", this.currentGameId.ToString() } };

            try
            {
                // Cancel any UI stuff before saving
                UI.MouseCursor.Instance.MouseCursorType = MouseCursorType.Normal;
                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);

                SaveGameManager.Save(e.FileName, false, this.gameScreen.ScrollPosition, this.gameScreen.ZoomLevel, this.gameScreen.W, this.gameScreen.H, additionalProperties);
                this.gameScreen.Saved(true);
            }
            catch
            {
                this.gameScreen.Saved(false);
            }
        }

        private void OnGameScreenLoadGameRequested(object sender, LoadGameEventArgs e)
        {
            if (SaveGameManager.CheckSaveGameVersion(e.FileName))
            {
                this.gameRenderer.ClearShadows();
                this.gameRenderer.IsLoading = true;
                SoundManager.Reset();
                SaveGameManager.Load(e.FileName, e.IsAutosave, this.gameScreen.W, this.gameScreen.H);
                this.gameRenderer.IsLoading = false;
                this.gameRenderer.InvalidateBuffers();
            }
            else
            {
                this.OnGameLoadFailed(null, new GameLoadFailedEventArgs(SaveGameManager.LastException));
            }
        }

        private void OnGameScreenExitRequested(object sender, EventArgs e)
        {
            this.gameRenderer.InvalidateBuffers();
            this.gameRenderer.ClearShadows();
            SoundManager.Reset();
            ResourceStackingController.Clear();
            WorldController.Clear();
            CommentaryController.Reset();
            CheckListController.Reset();
            TileHighlightManager.Reset();
            SeeThroughTreeManager.Reset();
        }

        private void OnGameScreenNewGameRequested(object sender, NewGameEventArgs e)
        {
            this.mapSize = e.MapSize;

            Log("New game requested");

            this.gameRenderer.InvalidateBuffers();
            this.gameRenderer.ClearShadows();
            this.gameRenderer.IsLoading = true;
            SoundManager.Reset();
            ResourceStackingController.Clear();
            CommentaryController.Reset();
            CheckListController.Reset();
            TileHighlightManager.Reset();
            SeeThroughTreeManager.Reset();
            PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            WorldController.Clear(this.mapSize);
            PathFinderBlockManager.Reset();
            World.ClimateType = e.ClimateType;

            this.isNewGame = true;
            this.IsInitializing = true;
            this.FileNameToLoad = "";
            this.Initializing?.Invoke(this, new EventArgs());
        }

        private void OnGameScreenFarmPanelRemoved(object sender, EventArgs e)
        {
            if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Farm) PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            if (this.gameRenderer.OverlayType == OverlayType.Crops) this.gameRenderer.OverlayType = OverlayType.None;
        }

        private void OnPauseButtonClick(object sender, EventArgs e)
        {
            this.IsPaused = !this.IsPaused;
        }

        private void OnPlayButtonClick(object sender, EventArgs e)
        {
            this.GameSpeed = 1;
            this.IsPaused = false;
        }

        private void OnTogglePauseButtonClick(object sender, EventArgs e)
        {
            this.IsPaused = !this.IsPaused;
        }

        private void OnIncreaseSpeedClick(object sender, EventArgs e)
        {
            this.IncreaseGameSpeed();
            this.IsPaused = false;
        }

        private void OnDecreaseSpeedClick(object sender, EventArgs e)
        {
            this.DecreaseGameSpeed();
            this.IsPaused = false;
        }

        private void OnToggleRoofButtonClick(object sender, EventArgs e)
        {
            this.ToggleRoofVisibility();
        }

        private void OnResourceOverlayClick(object sender, EventArgs e)
        {
            var newOverlayType = this.gameRenderer.OverlayType == OverlayType.Resources ? OverlayType.None : OverlayType.Resources;
            this.gameRenderer.OverlayType = newOverlayType;
            this.gameScreen.OverlayType = newOverlayType;
            this.resourceOverlayShownBecauseOfContext = false;
        }

        private void OnCropOverlayClick(object sender, EventArgs e)
        {
            var newOverlayType = this.gameRenderer.OverlayType == OverlayType.Crops ? OverlayType.None : OverlayType.Crops;
            this.gameRenderer.OverlayType = newOverlayType;
            this.gameScreen.OverlayType = newOverlayType;
            if (this.gameScreen.IsFarmPanelShown != (newOverlayType == OverlayType.Crops)) this.gameScreen.ToggleFarmPanel();
        }

        private void OnTemperatureOverlayClick(object sender, EventArgs e)
        {
            var newOverlayType = this.gameRenderer.OverlayType == OverlayType.Temperature ? OverlayType.None : OverlayType.Temperature;
            this.gameRenderer.OverlayType = newOverlayType;
            this.gameScreen.OverlayType = newOverlayType;
        }

        private void OnZoneOverlayClick(object sender, EventArgs e)
        {
            this.gameScreen.IsDiagnosticVisible = this.gameRenderer.ToggleZoneOverlay();
        }

        private void OnPauseGameRequest(object sender, EventArgs e)
        {
            this.prevIsPaused = this.IsPaused;
            if (!this.IsPaused)
            {
                this.IsPaused = true;
            }
        }

        private void OnResumeGameRequest(object sender, EventArgs e)
        {
            this.IsPaused = this.prevIsPaused;
        }

        private void DecreaseGameSpeed()
        {
            if (this.GameSpeed > 1)
            {
                this.GameSpeed /= 2;
            }
        }

        private void IncreaseGameSpeed()
        {
            if (this.GameSpeed < this.maxGameSpeed)
            {
                this.GameSpeed *= 2;
            }
        }

        private void Locate(ThingType thingType)
        {
            // Cycle between things of this type
            if (!this.lastSelectedIds.ContainsKey(thingType)) this.lastSelectedIds.Add(thingType, 0);
            var lastId = this.lastSelectedIds[thingType];

            IThing next = null;
            var candidates = World.GetThings(thingType);
            foreach (var candidate in candidates)
            {
                if (candidate is IColonist c && c.IsDead) continue;   // Don't include dead colonists
                if (next == null 
                    || (candidate.Id > lastId && (next.Id <= lastId || candidate.Id < next.Id))   // Next colonist ordered by ID
                    || (next.Id < lastId && candidate.Id < next.Id)) next = candidate;                         // First colonist ordered by ID
            }

            if (next != null)
            {
                PlayerWorldInteractionManager.SelectedThing = next;
                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
                
                this.gameScreen.ShowRelevantLeftPanels(next);
                this.lastSelectedIds[thingType] = next.Id;
                if (this.gameScreen.CameraTrackingThing == null)
                {
                    this.gameScreen.ScrollToPosition(next.GetWorldPosition());
                }
                else if (this.gameScreen.CameraTrackingThing != next)
                {
                    this.gameScreen.CameraTrackSelectedThing();
                }
            }
        }

        private void OnRocketLaunchClick()
        {
            this.GameSpeed = 1;
        }

        private void PlayerActivityChanged(object sender, EventArgs e)
        {
            // If building a mine or ore scanner then show the resource map, then hide it again when finished
            var showResourceOverlay = this.ShouldContextShowResourceOverlay();
            var showCropOverlay = PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Farm;
            if (showResourceOverlay && this.gameScreen.OverlayType != OverlayType.Resources && !showCropOverlay)
            {
                this.resourceOverlayShownBecauseOfContext = true;
                this.gameRenderer.OverlayType = OverlayType.Resources;
                this.gameScreen.OverlayType = OverlayType.Resources;
            }
            else if (showCropOverlay && this.gameScreen.OverlayType != OverlayType.Crops)
            {
                this.gameRenderer.OverlayType = OverlayType.Crops;
                this.gameScreen.OverlayType = OverlayType.Crops;
            }
            else if (this.resourceOverlayShownBecauseOfContext && !showResourceOverlay)
            {
                this.resourceOverlayShownBecauseOfContext = false;
                this.gameRenderer.OverlayType = OverlayType.None;
                this.gameScreen.OverlayType = OverlayType.None;
            }
        }

        private bool ShouldContextShowResourceOverlay()
        {
            if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Geology) return true;
            if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Build
                && (PlayerWorldInteractionManager.CurrentThingTypeToBuild == ThingType.Mine || PlayerWorldInteractionManager.CurrentThingTypeToBuild == ThingType.OreScanner)) return true;
            return PlayerWorldInteractionManager.SelectedThing?.ThingType == ThingType.Mine || PlayerWorldInteractionManager.SelectedThing?.ThingType == ThingType.OreScanner;
        }

        private static void Log(string message)
        {
            if (IsLogEnabled) Logger.Instance.Log("GameEngine", message);
        }
    }
}
