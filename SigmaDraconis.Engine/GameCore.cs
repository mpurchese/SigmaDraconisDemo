namespace SigmaDraconis.Engine
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Input;
    using Draconis.UI;

    using IO;
    using Language;
    using Settings;
    using Sound;
    using UI;
    using Shared;

    public class GameCore : Game
    {
        private readonly Logger logger = new Logger();

        protected readonly GraphicsDeviceManager graphicsManager;
        protected GameEngine gameEngine;
        protected MenuEngine menuEngine;
        protected bool isPlaying;
        protected bool loggedException;
        protected bool isFirstUpdate = true;
        protected int prevWindowWidth;
        protected int prevWindowHeight;
        protected MouseCursor mouseCursor;
        protected bool wasFocused = true;
        protected bool pausedDueToLostFocus = false;
        protected Task backgroundUpdateTask;
        protected readonly Stopwatch gameTimer;
        protected long nextTick = 0;
        protected long runningSlowlyTick = 0;
        protected bool isRunningSlowly;
        protected int runningSlowlyCounter;
        protected int appliedUiScale;
        protected readonly long ticksPerMs;
        protected long gameLoadedTick;

        public static GameCore Instance { get; private set; }

        public GameCore()
        {
            if (Instance == null)
            {
                var isFullScreen = SettingsManager.GetSettingBool(SettingGroup.Graphics, SettingNames.IsFullScreen).GetValueOrDefault();
                var width = SettingsManager.GetSettingInt(SettingGroup.Graphics, isFullScreen ? SettingNames.FullScreenSizeX : SettingNames.WindowScreenSizeX).GetValueOrDefault(1600);
                var height = SettingsManager.GetSettingInt(SettingGroup.Graphics, isFullScreen ? SettingNames.FullScreenSizeY : SettingNames.WindowScreenSizeY).GetValueOrDefault(900);

                this.graphicsManager = new GraphicsDeviceManager(this)
                {
                    IsFullScreen = false,
                    PreferredBackBufferWidth = width,
                    PreferredBackBufferHeight = height,
                    GraphicsProfile = GraphicsProfile.HiDef
                };

                this.IsFixedTimeStep = false;    // We do fps limiting ourselves, as the Monogame way causes stutter
                this.gameTimer = Stopwatch.StartNew();
                this.ticksPerMs = TimeSpan.FromMilliseconds(1.0).Ticks;

                this.graphicsManager.PreparingDeviceSettings += this.GraphicsManager_PreparingDeviceSettings;

                Instance = this;
                Content.RootDirectory = "Content";
            }
            else
            {
                throw new ApplicationException("GameCore already created");
            }
        }

        private void GraphicsManager_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            // This stops the screen being cleared when we do SetRenderTarget(null)
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }

        protected override void Initialize()
        {
            try
            {
                // Switch to 1280x720 on small screens
                if (!this.graphicsManager.IsFullScreen
                    && (this.GraphicsDevice.Adapter.CurrentDisplayMode.Width < this.GraphicsDevice.Viewport.Width
                    || this.GraphicsDevice.Adapter.CurrentDisplayMode.Height < this.GraphicsDevice.Viewport.Height))
                {
                    this.graphicsManager.PreferredBackBufferWidth = 1280;
                    this.graphicsManager.PreferredBackBufferHeight = 720;
                    this.graphicsManager.ApplyChanges();

                    SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.WindowScreenSizeX, 1280);
                    SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.WindowScreenSizeY, 720);
                    SettingsManager.Save();
                }

                this.Window.AllowUserResizing = true;
                this.prevWindowWidth = this.Window.ClientBounds.Width;
                this.prevWindowHeight = this.Window.ClientBounds.Height;
                this.Window.ClientSizeChanged += this.OnWindowClientSizeChanged;

                MouseManager.GraphicsDevice = this.GraphicsDevice;
                MouseManager.Window = this.Window;

                // UI config
                UIStatics.Content = this.Content;
                UIStatics.Graphics = this.GraphicsDevice;
                UIStatics.TextRenderer = new TextRenderer(GetFontTexturePath());
                UIStatics.TextRenderer.LoadContent();

                this.appliedUiScale = UIStatics.Scale;

                this.mouseCursor = new MouseCursor();
                this.mouseCursor.LoadContent();

                var language = SettingsManager.GetSetting(SettingGroup.Misc, SettingNames.Language) ?? "English";
                LanguageManager.Load(language);

                var menuScreen = new MenuScreen(this.Window, this.graphicsManager);
                menuScreen.DisplaySettingsChangeRequest += this.OnDisplaySettingsChangeRequest;
                menuScreen.DisplaySettingsChanged += this.OnDisplaySettingsChanged;
                menuScreen.ExitRequest += this.OnExitRequest;
                this.menuEngine = new MenuEngine(menuScreen);

                var gameScreen = new GameScreen(Window, graphicsManager);
                gameScreen.DisplaySettingsChangeRequest += this.OnDisplaySettingsChangeRequest;
                gameScreen.DisplaySettingsChanged += this.OnDisplaySettingsChanged;
                gameScreen.ExitToMainMenuRequest += this.OnExitToMainMenuRequest;
                gameScreen.ExitToDesktopRequest += this.OnExitRequest;

                this.gameEngine = new GameEngine(gameScreen);
                this.gameEngine.Load();
                this.gameEngine.Initialized += this.OnGameInitialized;
                this.gameEngine.Initializing += this.OnGameInitializing;
                this.gameEngine.GameLoaded += this.OnGameLoaded;

                MouseManager.CurrentScreen = menuScreen;
                menuScreen.IsVisible = true;
                gameScreen.IsVisible = false;
                if (MusicManager.IsPlaying) MusicManager.Stop();

                base.Initialize();
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("GameCore:Initialize", ex.ToString());
                Logger.Instance.Flush();
            }
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            if (GameScreen.Instance.ShouldConfirmExit)
            {
                // We can't easily stop the game from exiting here, but we can autosave.
                this.gameEngine.DoAutosave();
            }

            base.OnExiting(sender, args);
        }

        /// <summary>
        /// check when the application loses and then re-gains focus, then double-toggle fullscreen if necessary
        /// See http://community.monogame.net/t/how-to-detect-alt-tab-lost-window-focus/10568
        /// </summary>
        protected virtual void CheckAppRegainedFocus()
        {
            if (!this.IsActive)
            {
                this.wasFocused = false;
                if (GameScreen.Instance.IsVisible && !gameEngine.IsPaused)
                {
                    this.gameEngine.IsPaused = true;
                    this.pausedDueToLostFocus = true;
                }
            }
            else if (!wasFocused && this.graphicsManager.IsFullScreen)
            {
                this.wasFocused = true;
            }

            if (this.IsActive && this.pausedDueToLostFocus)
            {
                this.pausedDueToLostFocus = false;
                if (GameScreen.Instance.IsVisible)
                {
                    this.gameEngine.IsPaused = false;
                }
            }
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            if (!this.IsActive) return;

            SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.IsFullScreen, this.graphicsManager.IsFullScreen);
            if (this.graphicsManager.IsFullScreen)
            {
                SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.FullScreenSizeX, this.GraphicsDevice.Viewport.Width);
                SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.FullScreenSizeY, this.GraphicsDevice.Viewport.Height);
                SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.FullScreenPositionX, this.Window.Position.X);
                SettingsManager.SetSetting(SettingGroup.Graphics, SettingNames.FullScreenPositionY, this.Window.Position.Y);
            }

            SettingsManager.Save();
        }

        private void OnWindowClientSizeChanged(object sender, EventArgs e)
        {
            if (!this.IsActive || this.Window.ClientBounds.Width == 0) return;

            if (this.Window.ClientBounds.Width < 1280 || this.Window.ClientBounds.Height < 720)
            {
                if (this.Window.ClientBounds.Width < 1280) this.graphicsManager.PreferredBackBufferWidth = 1280;
                if (this.Window.ClientBounds.Height < 720) this.graphicsManager.PreferredBackBufferHeight = 720;
                this.graphicsManager.ApplyChanges();
            }
        }

        protected virtual void OnExitRequest(object sender, EventArgs e)
        {
            // Must call Exit() from platform specific code or Monogame gives an error
        }

        private void OnExitToMainMenuRequest(object sender, EventArgs e)
        {
            this.isPlaying = false;
            MouseManager.CurrentScreen = this.menuEngine.Screen;
            GameScreen.Instance.IsVisible = false;
            MouseManager.CurrentMouseOverElement = this.menuEngine.Screen;
            this.menuEngine.Screen.Reset();
            this.menuEngine.Screen.IsVisible = true;
            if (MusicManager.IsPlaying) MusicManager.Stop();
        }

        private void OnGameInitializing(object sender, EventArgs e)
        {
            this.isPlaying = false;
            GameScreen.Instance.IsVisible = false;
            this.menuEngine.Screen.IsVisible = true;
            if (MusicManager.IsPlaying) MusicManager.Stop();
        }

        private void OnGameLoaded(object sender, EventArgs e)
        {
            this.gameLoadedTick = this.gameTimer.Elapsed.Ticks;
        }

        private void OnGameInitialized(object sender, EventArgs e)
        {
            this.isPlaying = true;
            this.gameEngine.LoadContent();
            if (!string.IsNullOrEmpty(this.gameEngine.FileNameToLoad))
            {
                SaveGameManager.Load(this.gameEngine.FileNameToLoad, this.gameEngine.IsFileToLoadAutosave, this.menuEngine.Screen.W, this.menuEngine.Screen.H);
            }

            MouseManager.CurrentScreen = GameScreen.Instance;
            GameScreen.Instance.IsVisible = true;
            MouseManager.CurrentMouseOverElement = GameScreen.Instance;
            this.menuEngine.Screen.IsVisible = false;
            if (!MusicManager.IsPlaying) MusicManager.Start();
            this.gameLoadedTick = this.gameTimer.Elapsed.Ticks;
        }

        protected override void LoadContent()
        {
            this.menuEngine.LoadContent();
            this.menuEngine.NewGameClick += this.OnNewGameClick;
            this.menuEngine.LoadGameClick += this.OnLoadGameClick;
            MusicManager.LoadContent();
        }

        private void OnNewGameClick(object sender, NewGameEventArgs e)
        {
            if (!this.isPlaying)
            {
                this.gameEngine.FileNameToLoad = "";
                this.gameEngine.BeginInitialize(e.MapSize, SettingsManager.GetSettingInt(SettingGroup.Misc, SettingNames.MaxGameSpeed).GetValueOrDefault(8), true, e.ClimateType);
            }
        }

        private void OnLoadGameClick(object sender, LoadGameEventArgs e)
        {
            this.isPlaying = true;
            this.gameEngine.FileNameToLoad = e.FileName;
            this.gameEngine.IsFileToLoadAutosave = e.IsAutosave;
            this.gameEngine.BeginInitialize(128, SettingsManager.GetSettingInt(SettingGroup.Misc, SettingNames.MaxGameSpeed).GetValueOrDefault(8), false);
            if (sender is MenuEngine menu)
            {
                // This stops the scroll position from being wrong because the screen dimensions are wrong
                GameScreen.Instance.W = menu.Screen.W;
                GameScreen.Instance.H = menu.Screen.H;
            }
        }

        protected virtual void OnDisplaySettingsChangeRequest(object sender, DisplaySettingsChangeRequestEventArgs e)
        {
            this.graphicsManager.PreferredBackBufferWidth = e.Width;
            this.graphicsManager.PreferredBackBufferHeight = e.Height;
            if (e.ToggleFullScreen) this.graphicsManager.IsFullScreen = !this.graphicsManager.IsFullScreen;
            this.graphicsManager.ApplyChanges();
        }

        protected override void UnloadContent()
        {
            this.menuEngine.Dispose();
            this.gameEngine.Dispose();
        }

        protected override void Update(GameTime gameTime)
        {
            try
            {
                if (this.isFirstUpdate)
                {
                    this.isFirstUpdate = false;
                    if (SettingsManager.GetSettingBool(SettingGroup.Graphics, SettingNames.IsFullScreen) == true)
                    {
                        // Restore to correct screen in multi-screen setups
                        var x = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.FullScreenPositionX) ?? 0;
                        var y = SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.FullScreenPositionY) ?? 0;
                        this.Window.Position = new Point(x, y);
                        this.graphicsManager.ToggleFullScreen();
                    }
                }

                // Limit to 60fps.  This method should compensate for the inaccuracy of Thread.Sleep()
                var thisTick = this.gameTimer.Elapsed.Ticks;
                if (this.isRunningSlowly && thisTick > this.runningSlowlyTick + (ticksPerMs * 500)) this.isRunningSlowly = false;
                if (thisTick < this.nextTick - ticksPerMs)
                {
                    System.Threading.Thread.Sleep((int)((this.nextTick - thisTick) / ticksPerMs));
                    if (this.runningSlowlyCounter > 0) this.runningSlowlyCounter-=2;
                }
                else if (thisTick > this.nextTick)
                {
                    if (thisTick < this.gameLoadedTick + (this.ticksPerMs * 10000))
                    {
                        // Stops the game speeding up after loading
                        this.isRunningSlowly = false;
                        this.runningSlowlyCounter = 0;
                        this.nextTick = thisTick;
                    }
                    else
                    {

                        if (this.runningSlowlyCounter < 60) this.runningSlowlyCounter++;
                        //  && thisTick > this.gameLoadedTick + (this.ticksPerMs * 20000)
                        if (this.runningSlowlyCounter > 5)
                        {
                            // Slow down draw for a short while to let update catch up
                            this.isRunningSlowly = true;
                            this.runningSlowlyTick = thisTick;
                        }
                    }
                }

                thisTick = this.gameTimer.Elapsed.Ticks;
                this.nextTick += (long)(this.ticksPerMs * 16.667);
                if (this.nextTick < thisTick + (ticksPerMs * 10)) this.nextTick = thisTick + (this.ticksPerMs * 10);
                else if (this.nextTick > thisTick + (ticksPerMs * 20)) this.nextTick = thisTick + (this.ticksPerMs * 20);

                if (this.backgroundUpdateTask != null) this.backgroundUpdateTask.Wait();

                this.CheckAppRegainedFocus();
                MouseManager.IsWindowActive = this.IsActive;
                GameScreen.Instance.IsWindowActive = this.IsActive;

                // Automatic UI scale
                var displayRect = UIStatics.Graphics.Viewport.TitleSafeArea;
                var scaleSetting = (UIScaleSettings)SettingsManager.GetSettingInt(SettingGroup.Graphics, SettingNames.UIScaling);
                if (displayRect.Width > 2400 && displayRect.Height > 1200 && scaleSetting == UIScaleSettings.Maximum) UIStatics.Scale = 200;
                else if (displayRect.Width > 1800 && displayRect.Height > 800 && scaleSetting != UIScaleSettings.None) UIStatics.Scale = 150;
                else UIStatics.Scale = 100;

                if (this.isPlaying || this.gameEngine.IsInitializing)
                {
                    this.gameEngine.Update(this.isRunningSlowly);
                    if (!this.gameEngine.IsPaused && !this.gameEngine.IsInitializing)
                    {
                        this.backgroundUpdateTask = new Task(this.DoBackgroundUpdate);
                        this.backgroundUpdateTask.Start();
                    }
                }
                else
                {
                    this.menuEngine.Update();
                }

                if (this.appliedUiScale != UIStatics.Scale)
                {
                    UIStatics.TextRenderer.ReloadContent(GetFontTexturePath());
                    this.appliedUiScale = UIStatics.Scale;
                }

                this.mouseCursor.Update();
                base.Update(gameTime);

                //this.updateTimeoutTimer.Stop();
            }
            catch (Exception ex)
            {
                if (!loggedException)
                {
                    this.logger.Log("GameCore:Update", ex.ToString());
                    loggedException = true;
                }

                if (this.gameEngine.IsInitializing)
                {
                    this.logger.Flush();
                    this.OnExitRequest(this, null);
                }
            }
        }

        protected void DoBackgroundUpdate()
        {
            this.gameEngine.DoBackgroundUpdate();
        }

        protected override void Draw(GameTime gameTime)
        {
            try
            {
                if (this.isPlaying && !this.gameEngine.IsInitializing)
                {
                    if (this.gameEngine.Draw()) this.mouseCursor.Draw();
                }
                else
                {
                    this.menuEngine.Draw(gameTime);
                    if (!this.gameEngine.IsInitializing) this.mouseCursor.Draw();
                }
            }
            catch (Exception ex)
            {
                if (!loggedException)
                {
                    this.logger.Log("GameCore:Draw", ex.ToString());
                    loggedException = true;
                }
            }
        }

        private static string GetFontTexturePath()
        {
            if (UIStatics.Scale == 200) return "Textures\\Fonts\\SpaceMono17";
            if (UIStatics.Scale == 150) return "Textures\\Fonts\\SpaceMono13";
            return "Textures\\Fonts\\SpaceMono8";
        }
    }
}
