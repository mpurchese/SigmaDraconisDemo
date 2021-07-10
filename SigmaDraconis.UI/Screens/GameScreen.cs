namespace SigmaDraconis.UI
{
    using System;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Input;

    using Draconis.Input;
    using Draconis.Shared;
    using Draconis.UI;

    using CheckList;
    using Config;
    using IO;
    using Language;
    using Managers;
    using Settings;
    using Shared;
    using World;
    using World.Buildings;
    using World.Rooms;
    using WorldInterfaces;

    public sealed class GameScreen : ScreenBase
    {
        private Vector2i minScrollPosition = new Vector2i(-700, -4400);
        private Vector2i maxScrollPosition = new Vector2i(15600, 3700);

        private bool hideUI;
        private int zoomSteps = 0;
        private float zoomStepSize = 0f;
        public Vector2f ScrollPosition = new Vector2f(1400, -350);
        private bool isRequestingExitToMainMenu;
        private bool isZoomingUsingScollWheel;
        private bool shownRocketLaunchedDialog;
        private bool isPausedForDialog;
        private bool shownCheckListPanel;
        private long frameLastSaved;

        public static GameScreen Instance { get; private set; }

        public Toolbar Toolbar { get; private set; }
        public StatusBar StatusBar { get; private set; }

        private readonly PanelManager panelManager;

        public ModalBackgroundBox ModalBackgroundBox { get; private set; }
        public ColonistPortraitButtonContainer ColonistPortraitButtonContainer { get; private set; }
        public TooltipParent TooltipParent { get; private set; }

        private DebugDialog debugDialog;
        private CommentArchiveDialog commentArchiveDialog;
        private SaveGameDialog saveGameDialog;
        private SavedGameDialog savedGameDialog;
        private DeleteGameDialog deleteGameDialog;
        private ConfirmExitDialog confirmExitDialog;
        private GameOverDialog gameOverDialog;
        private RocketLaunchedDialog rocketLaunchedDialog;
        private MothershipDialog mothershipDialog;
        private ErrorDialog errorDialog;
        private IUIElement prevDialog;
        private TextLabel fpsLabel;
        private TextLabel diagnosticLabel;
        private TextLabel tileInfoLabel;
        private TextLabel screenhotLabel;
        private OverlayType currentOverlayType;
        private CommentaryHistoryPopup commentaryHistoryPopup;
        private WarningsDisplay warningsDisplay;

        public event EventHandler<EventArgs> ExitToMainMenuRequest;
        public event EventHandler<EventArgs> ExitToDesktopRequest;
        public event EventHandler<EventArgs> FarmPanelClose;
        public event EventHandler<EventArgs> PauseGameRequest;
        public event EventHandler<EventArgs> ResumeGameRequest;
        public event EventHandler<EventArgs> PauseClick;
        public event EventHandler<EventArgs> PlayClick;
        public event EventHandler<EventArgs> TogglePauseClick;
        public event EventHandler<EventArgs> IncreaseSpeedClick;
        public event EventHandler<EventArgs> DecreaseSpeedClick;
        public event EventHandler<EventArgs> ToggleRoofClick;
        public event EventHandler<EventArgs> CropOverlayClick;
        public event EventHandler<EventArgs> ResourceOverlayClick;
        public event EventHandler<EventArgs> TemperatureOverlayClick;
        public event EventHandler<EventArgs> ZoneOverlayClick;
        public event EventHandler<EventArgs> LocateRequest;
        public event EventHandler<EventArgs> ScreenshotRequest;
        public event EventHandler<EventArgs> ScreenshotNoUIRequest;
        public event EventHandler<EventArgs> AutosaveRequested;
        public event EventHandler<SaveRequestEventArgs> SaveRequested;

        public float Zoom { get; private set; } = 0.5f;
        public float TargetZoom { get; private set; } = 0.5f;
        public int ZoomLevel { get; private set; } = 31;
        public IThing CameraTrackingThing { get; private set; }
        public ThingType ThingTypeToLocate { get; private set; }
        public bool IsPaused { get; set; }
        public bool IsRunningSlowly { get; set; }

        public bool IsCheckListPanelShown => this.panelManager.ChecklistPanel.IsShown;
        public bool IsConstructPanelShown => this.panelManager.ConstructPanel.IsShown;
        public bool IsFarmPanelShown => this.panelManager.FarmPanel.IsShown;

        public bool IsRoofVisible
        {
            get { return this.StatusBar.IsRoofVisible; }
            set
            {
                this.StatusBar.IsRoofVisible = value;
                if (!value && PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Build && PlayerWorldInteractionManager.CurrentThingTypeToBuild == ThingType.Roof)
                {
                    // Cancel roof building if we hide roofs
                    PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
                }
            }
        }

        // For auto-scrolling
        public Vector2f StartScrollPosition { get; set; } = null;
        public Vector2f EndScrollPosition { get; set; } = null;
        public int ScrollMoveFrame { get; set; } = 0;

        public bool IsOptionsPanelVisible => this.panelManager.OptionsPanel.IsShown;

        public void SetZoom(int level, bool immediate = false)
        {
            this.ZoomLevel = level;
            this.TargetZoom = (1 + this.ZoomLevel) * 0.125f;
            if (immediate)
            {
                this.Zoom = this.TargetZoom;
            }
            else
            {
                this.zoomSteps = (int)(24f * (Math.Max(30f, PerfMonitor.UpdateFramesPerSecond) / 60f));
                this.zoomStepSize = (this.TargetZoom - this.Zoom) / this.zoomSteps;
            }
        }

        public bool IsDiagnosticVisible
        {
            get
            {
                return this.diagnosticLabel.IsVisible;
            }
            set
            {
                this.diagnosticLabel.IsVisible = value;
            }
        }

        public OverlayType OverlayType
        {
            get
            {
                return this.currentOverlayType;
            }
            set
            {
                this.tileInfoLabel.IsVisible = value != OverlayType.None;
                this.currentOverlayType = value;
            }
        }

        public bool IsFpsVisible
        {
            get
            {
                return this.fpsLabel.IsVisible;
            }
            set
            {
                this.fpsLabel.IsVisible = value;
            }
        }

        public IThing HighlightedThing { get; set; }

        public bool ShouldConfirmExit => World.WorldTime.FrameNumber > this.frameLastSaved + 300 && World.Colonists.Any(c => !c.IsDead);


        public GameScreen(GameWindow gameWindow, GraphicsDeviceManager graphicsManager)
            : base(gameWindow, graphicsManager)
        {
            if (Instance == null)
            {
                Instance = this;
                this.IsDraggable = true;
                this.IsRightDraggable = true;
                this.IsInteractive = true;
                this.panelManager = new PanelManager(this);
            }
            else
            {
                throw new ApplicationException("GameScreen instance already created");
            }
        }

        public void Initialise(int mapSize, int maxGameSpeed)
        {
            this.ScrollPosition = new Vector2f((16 * mapSize) - 640, -350);
            this.minScrollPosition = new Vector2i(0, 0 - (32 * mapSize));
            this.maxScrollPosition = new Vector2i(128 * mapSize, 32 * mapSize);
            this.appliedScale = UIStatics.Scale;

            this.TooltipParent = new TooltipParent(this);

            this.Toolbar = new Toolbar(this, 0, 0, UIStatics.Graphics.Viewport.Width, Scale(40));
            this.Toolbar.OptionsButtonClick += this.ToolbarOptionsButtonClick;
            this.Toolbar.HelpButtonClick += this.ToolbarCheckListButtonClick;
            this.AddChild(this.Toolbar);

            this.commentaryHistoryPopup = new CommentaryHistoryPopup(this) { IsVisible = false };
            this.AddChild(this.commentaryHistoryPopup);
            this.commentaryHistoryPopup.ArchiveButtonClick += this.OnCommentaryHistoryPopupArchiveButtonClick;

            this.StatusBar = new StatusBar(this, 0, UIStatics.Graphics.Viewport.Height - Scale(24) - 2, UIStatics.Graphics.Viewport.Width, Scale(24) + 2) { MaxGameSpeed = maxGameSpeed };
            this.AddChild(this.StatusBar);
            this.StatusBar.PauseClick += this.OnPauseButtonClick;
            this.StatusBar.PlayClick += this.OnPlayButtonClick;
            this.StatusBar.FastForwardClick += this.OnFastForwardButtonClick;
            this.StatusBar.ZoomInClick += this.OnZoomInButtonClick;
            this.StatusBar.ZoomOutClick += this.OnZoomOutButtonClick;
            this.StatusBar.ToggleRoofClick += this.OnToggleRoofButtonClick;
            this.StatusBar.ConstructClick += this.OnConstructButtonClick;
            this.StatusBar.FarmClick += this.OnFarmButtonClick;
            this.StatusBar.GeologyClick += this.OnGeologyButtonClick;
            this.StatusBar.RecycleClick += this.OnRecycleButtonClick;
            this.StatusBar.HarvestClick += this.OnHarvestButtonClick;
            this.StatusBar.TemperatureClick += this.OnTemperatureOverlayButtonClick;
            this.StatusBar.ResourceMapClick += this.OnResourceOverlayButtonClick;
            this.StatusBar.MothershipClick += this.OnMothershipButtonClick;
            this.StatusBar.CommentaryTickerClick += this.OnCommentaryTickerClick;

            this.fpsLabel = new TextLabel(this, Scale(10), UIStatics.Graphics.Viewport.Height - Scale(44), "", Color.LightGray, true) { IsVisible = false };
            this.AddChild(this.fpsLabel);

            this.diagnosticLabel = new TextLabel(this, Scale(10), UIStatics.Graphics.Viewport.Height - Scale(54), "", Color.LightGray, true) { IsVisible = false };
            this.AddChild(this.diagnosticLabel);

            this.tileInfoLabel = new TextLabel(this, Scale(10), UIStatics.Graphics.Viewport.Height - Scale(54), "", Color.LightGray, true) { IsVisible = false };
            this.AddChild(this.tileInfoLabel);

            this.warningsDisplay = new WarningsDisplay(this, UIStatics.Graphics.Viewport.Width - Scale(358), UIStatics.Graphics.Viewport.Height - Scale(230), Scale(350), Scale(200));
            this.AddChild(this.warningsDisplay);

            this.screenhotLabel = new TextLabel(this, 0, UIStatics.Graphics.Viewport.Height - Scale(44), this.W, Scale(16), "", Color.LightGray, TextAlignment.TopCentre, true) { IsVisible = false };
            this.AddChild(this.screenhotLabel);

            this.panelManager.Init();

            this.panelManager.OptionsPanel.AchievementsClick += this.OnOptionsAchievementsClick;
            this.panelManager.OptionsPanel.SaveGameClick += this.OnOptionsPanelSaveGameClick;
            this.panelManager.OptionsPanel.SettingsClick += this.OnOptionsPanelSettingsClick;
            this.panelManager.OptionsPanel.KeyboardControlsClick += this.OnOptionsPanelKeyboardControlsClick;
            this.panelManager.OptionsPanel.ExitToMainMenuClick += this.OnOptionsPanelExitToMainMenuClick;
            this.panelManager.OptionsPanel.ExitToDesktopClick += this.OnOptionsPanelExitToDesktopClick;

            this.panelManager.AnimalPanel.FollowButtonClick += this.OnPanelCameraTrackingButtonClick;
            this.panelManager.ColonistPanel.FollowButtonClick += this.OnPanelCameraTrackingButtonClick;
            this.panelManager.LandingPodPanel.FollowButtonClick += this.OnPanelCameraTrackingButtonClick;

            this.panelManager.DeconstructConduitNodeClick += this.OnPanelDeconstructConduitNodeClick;
            this.panelManager.DeconstructFoundationClick += this.OnPanelDeconstructFoundationClick;

            this.panelManager.FarmPanel.Close += this.OnFarmPanelClose;

            this.ColonistPortraitButtonContainer = new ColonistPortraitButtonContainer(this, Scale(42));
            this.AddChild(this.ColonistPortraitButtonContainer);

            // This must come after the rest of the UI, apart from modal dialogs
            this.AddChild(this.TooltipParent);

            this.ModalBackgroundBox = new ModalBackgroundBox(this);
            this.AddChild(this.ModalBackgroundBox);

            // For dialog tooltips
            this.AddChild(new TooltipParentForDialogs(this));

            this.debugDialog = new DebugDialog(this.ModalBackgroundBox);
            this.debugDialog.Close += this.OnDebugDialogCloseClick;

            this.commentArchiveDialog = new CommentArchiveDialog(this.ModalBackgroundBox);
            this.commentArchiveDialog.CloseClick += this.OnCommentArchiveDialogCloseClick;

            this.saveGameDialog = new SaveGameDialog(this.ModalBackgroundBox);
            this.saveGameDialog.SaveClick += this.OnSaveGameDialogSave;
            this.saveGameDialog.CancelClick += this.OnSaveGameDialogCancel;
            this.saveGameDialog.DeleteClick += this.OnSaveGameDialogDelete;

            this.deleteGameDialog = new DeleteGameDialog(this.ModalBackgroundBox);
            this.deleteGameDialog.CloseClick += this.OnDeleteGameDialogClose;

            this.savedGameDialog = new SavedGameDialog(this.ModalBackgroundBox);
            this.savedGameDialog.ContinueClick += this.OnSavedGameDialogClose;

            this.errorDialog = new ErrorDialog(this.ModalBackgroundBox);
            this.errorDialog.ContinueClick += this.OnErrorDialogClose;

            this.confirmExitDialog = new ConfirmExitDialog(this.ModalBackgroundBox);
            this.confirmExitDialog.ExitClick += this.OnConfirmExitDialogOk;
            this.confirmExitDialog.SaveClick += this.OnConfirmExitDialogSave;
            this.confirmExitDialog.CancelClick += this.OnConfirmExitDialogCancel;

            this.rocketLaunchedDialog = new RocketLaunchedDialog(this.ModalBackgroundBox);
            this.rocketLaunchedDialog.ContinueClick += this.OnRocketLaunchedDialogContinue;
            this.rocketLaunchedDialog.ExitClick += this.OnRocketLaunchedExitClick;

            this.mothershipDialog = new MothershipDialog(this.ModalBackgroundBox);
            this.mothershipDialog.StartClick += this.OnMothershipDialogCloseClick;
            this.mothershipDialog.CancelClick += this.OnMothershipDialogCloseClick;

            this.gameOverDialog = new GameOverDialog(this.ModalBackgroundBox);
            this.gameOverDialog.ExitToMenuClick += this.OnExitToMenuClick;
            this.gameOverDialog.ExitToWindowsClick += this.OnRocketLaunchedExitClick;

            this.ModalBackgroundBox.AddChild(this.debugDialog);
            this.ModalBackgroundBox.AddChild(this.commentArchiveDialog);
            this.ModalBackgroundBox.AddChild(this.saveGameDialog);
            this.ModalBackgroundBox.AddChild(this.savedGameDialog);
            this.ModalBackgroundBox.AddChild(this.errorDialog);
            this.ModalBackgroundBox.AddChild(this.deleteGameDialog);
            this.ModalBackgroundBox.AddChild(this.confirmExitDialog);
            this.ModalBackgroundBox.AddChild(this.gameOverDialog);
            this.ModalBackgroundBox.AddChild(this.rocketLaunchedDialog);
            this.ModalBackgroundBox.AddChild(this.mothershipDialog);

            this.InitBaseDialogs(this.ModalBackgroundBox);

            previousIsFullScreen = this.graphicsManager.IsFullScreen;
            previousDisplayWidth = UIStatics.Graphics.Viewport.Width;
            previousDisplayHeight = UIStatics.Graphics.Viewport.Height;

            EventManager.Subscribe(EventType.Game, EventSubType.Loaded, delegate (object obj) { this.OnGameLoaded(); });
            EventManager.Subscribe(EventType.Thing, EventSubType.Removed, delegate (object obj) { this.OnThingRemoved(obj); });
            EventManager.Subscribe(EventType.RocketLaunchClick, delegate (object obj) { this.OnRocketLaunchClick(obj); });
            EventManager.Subscribe(EventType.RocketLaunchStart, delegate (object obj) { this.OnRocketLaunchStart(); });
            EventManager.Subscribe(EventType.RocketLaunched, delegate (object obj) { this.OnRocketLaunched(); });

            PlayerWorldInteractionManager.Init();
            PlayerWorldInteractionManager.CurrentActivityChanged += this.OnCurrentActivityChanged;

            KeyboardManager.FocusedElement = this;
        }

        public override void Draw()
        {
            if (this.hideUI) return;
            base.Draw();
        }

        public void ClearScreenshotText()
        {
            this.screenhotLabel.IsVisible = false;
        }

        public void SetScreenshotText(string filePath, bool noUI)
        {
            this.screenhotLabel.IsVisible = true;
            this.screenhotLabel.Text = LanguageManager.Get<StringsForGameScreen>(noUI ? StringsForGameScreen.SavingScreenshotNoUI : StringsForGameScreen.SavingScreenshot, filePath);
        }

        public override void ApplyLayout()
        {
            this.StatusBar.Y = UIStatics.Graphics.Viewport.Height - Scale(24) - 2;
            this.ColonistPortraitButtonContainer.Y = Scale(42);
            this.warningsDisplay.X = UIStatics.Graphics.Viewport.Width - Scale(358);
            this.warningsDisplay.Y = UIStatics.Graphics.Viewport.Height - Scale(230);
            this.screenhotLabel.Y = UIStatics.Graphics.Viewport.Height - Scale(44);

            foreach (var child in this.Children)
            {
                if (child is PanelLeft)
                {
                    child.X = this.Rescale(child.X);
                    child.Y = this.Rescale(child.Y);
                }
                else if (child is PanelRight)
                {
                    child.X = this.W - this.Rescale(this.W - child.X);
                    child.Y = this.Rescale(child.Y);
                }
                else if (child is PanelBottom)
                {
                    child.Y = this.H - this.Rescale(this.H - child.Y);
                }

                if (child == this.screenhotLabel)
                {
                    child.W = this.W;
                    child.H = Scale(16);
                }
                else
                {
                    child.ApplyScale();
                    child.ApplyLayout();
                }
            }

            this.appliedScale = UIStatics.Scale;
            this.suppressOnParentResize = true;
            this.IsContentChangedSinceDraw = true;
        }

        public override T AddChild<T>(T child)
        {
            return child is PanelBase ? base.AddBefore(child, this.TooltipParent) : base.AddChild(child);
        }

        private void OnDebugDialogCloseClick(object sender, EventArgs e)
        {
            this.CloseDialog(this.debugDialog);
            if (this.isPausedForDialog) this.PlayClick?.Invoke(this, null);
            this.isPausedForDialog = false;
        }

        private void OnCommentArchiveDialogCloseClick(object sender, EventArgs e)
        {
            this.CloseDialog(this.commentArchiveDialog);
            if (this.isPausedForDialog) this.PlayClick?.Invoke(this, null);
            this.isPausedForDialog = false;
        }

        private void OnMothershipDialogCloseClick(object sender, MouseEventArgs e)
        {
            this.CloseDialog(this.mothershipDialog);
            if (this.isPausedForDialog || World.WorldTime.FrameNumber == 0) this.PlayClick?.Invoke(this, null);
            this.isPausedForDialog = false;
        }

        public void CameraTrackSelectedThing()
        {
            if (this.CameraTrackingThing != null && this.CameraTrackingThing == PlayerWorldInteractionManager.SelectedThing)
            {
                this.StartScrollPosition = null;
                this.EndScrollPosition = null;
                this.CameraTrackingThing = null;
            }
            else if (PlayerWorldInteractionManager.SelectedThing != null)
            {
                this.CameraTrackingThing = PlayerWorldInteractionManager.SelectedThing;
                this.ScrollToPosition(PlayerWorldInteractionManager.SelectedThing.GetWorldPosition());
            }
        }

        private void OnPanelCameraTrackingButtonClick(object sender, EventArgs e)
        {
            if (sender is IThingPanel ts && ts.Thing != null)
            {
                this.CameraTrackingThing = this.CameraTrackingThing == ts.Thing ? null : ts.Thing;
                if (this.CameraTrackingThing != null) this.ScrollToPosition(this.CameraTrackingThing.GetWorldPosition());
            }
        }

        public void ShowDebugDialog()
        {
            if (!this.IsPaused)
            {
                this.isPausedForDialog = true;
                this.PauseGameRequest?.Invoke(this, null);
            }

            OpenDialog(this.debugDialog);
        }

        private void ShowCommentArchiveDialog()
        {
            if (!this.IsPaused)
            {
                this.isPausedForDialog = true;
                this.PauseGameRequest?.Invoke(this, null);
            }

            OpenDialog(this.commentArchiveDialog);
        }

        public void ShowErrorDialog(string title = "", string message = "")
        {
            this.PauseGameRequest?.Invoke(this, null);
            OpenDialog(this.errorDialog);
            if (message == "")
            {
                this.errorDialog.ResetMessage();
            }
            else
            {
                this.errorDialog.SetMessage(title, message);
            }
        }

        public void ShowMothershipDialog()
        {
            if (!this.IsPaused)
            {
                this.isPausedForDialog = true;
                this.PauseGameRequest?.Invoke(this, null);
            }

            OpenDialog(this.mothershipDialog);
        }

        public void ToggleCommentaryHistoryPanel()
        {
            if (this.commentaryHistoryPopup.IsVisible) this.commentaryHistoryPopup.Hide();
            else
            {
                this.commentaryHistoryPopup.ResetScrollPosition();
                this.commentaryHistoryPopup.Show();
            }
        }

        // Call after load or new game to make sure unwanted UI elements are hidden
        public void HidePanels()
        {
            this.panelManager.HideAll();
            this.commentaryHistoryPopup.Hide();
            this.Toolbar.SelectRightButton(null);
        }

        public void Scroll(float deltaX, float deltaY, bool updateHiglights = true, bool isAuto = false)
        {
            var multiplier = 2f / this.Zoom;
            if (!isAuto)
            {
                this.StartScrollPosition = null;
                this.EndScrollPosition = null;
                this.CameraTrackingThing = null;
            }
            else multiplier = 1;

            this.ScrollPosition.X += deltaX * multiplier;
            this.ScrollPosition.Y += deltaY * multiplier;

            if (this.ScrollPosition.X < this.minScrollPosition.X - UIStatics.Graphics.Viewport.Width) this.ScrollPosition.X = this.minScrollPosition.X - UIStatics.Graphics.Viewport.Width;
            if (this.ScrollPosition.Y < this.minScrollPosition.Y - UIStatics.Graphics.Viewport.Height) this.ScrollPosition.Y = this.minScrollPosition.Y - UIStatics.Graphics.Viewport.Height;
            if (this.ScrollPosition.X > this.maxScrollPosition.X - UIStatics.Graphics.Viewport.Width) this.ScrollPosition.X = this.maxScrollPosition.X - UIStatics.Graphics.Viewport.Width;
            if (this.ScrollPosition.Y > this.maxScrollPosition.Y - UIStatics.Graphics.Viewport.Height) this.ScrollPosition.Y = this.maxScrollPosition.Y - UIStatics.Graphics.Viewport.Height;

            if (updateHiglights)
            {
                MouseWorldPosition.Update(this.ScrollPosition, this.Zoom);
                this.UpdateHighlights();
            }
        }

        public override void Update()
        {
            this.DoDisplaySettings();

            UIStatics.BackgroundAlpha = World.ClimateType == ClimateType.Snow ? 128 : 64;
            this.panelManager.UpdateAll();

            this.UpdateHighlights();
            PlayerWorldInteractionManager.Update(this.ScrollPosition, this.Zoom);

            var mx = UIStatics.CurrentMouseState.X;
            var my = UIStatics.CurrentMouseState.Y;

            bool isMouseDown = UIStatics.CurrentMouseState.LeftButton == ButtonState.Pressed;

            if (this.zoomSteps > 0)
            {
                if (this.isZoomingUsingScollWheel)
                {
                    var currentMouseWorldPosition = CoordinateHelper.GetWorldPosition(UIStatics.Graphics, this.ScrollPosition, this.Zoom, mx, my);
                    var newMouseWorldPosition = CoordinateHelper.GetWorldPosition(UIStatics.Graphics, this.ScrollPosition, this.Zoom + this.zoomStepSize, mx, my);
                    this.ScrollPosition.X += currentMouseWorldPosition.X - newMouseWorldPosition.X;
                    this.ScrollPosition.Y += currentMouseWorldPosition.Y - newMouseWorldPosition.Y;
                }

                this.Zoom += this.zoomStepSize;

                --this.zoomSteps;
            }
            else if (this.Zoom != this.TargetZoom)
            {
                this.Zoom = this.TargetZoom;
            }

            if (this.EndScrollPosition != null)
            {
                if (this.CameraTrackingThing is IMoveableThing mt)
                {
                    var pos = (mt as Thing).GetWorldPosition();
                    this.EndScrollPosition = new Vector2f((pos.X * 2) - UIStatics.Graphics.Viewport.Width, (pos.Y * 2) - UIStatics.Graphics.Viewport.Height);
                }

                var delta = this.EndScrollPosition - this.StartScrollPosition;
                this.ScrollMoveFrame++;

                if (this.ScrollMoveFrame == 30)
                {
                    this.ScrollPosition.X = this.EndScrollPosition.X;
                    this.ScrollPosition.Y = this.EndScrollPosition.Y;
                    this.StartScrollPosition = null;
                    this.EndScrollPosition = null;
                    this.ScrollMoveFrame = 0;
                }
                else
                {
                    var frac = (float)Math.Sin((float)this.ScrollMoveFrame * Math.PI / 60f);
                    var nextPosX = (delta.X * frac) + this.StartScrollPosition.X;
                    var nextPosY = (delta.Y * frac) + this.StartScrollPosition.Y;
                    this.Scroll(nextPosX - this.ScrollPosition.X, nextPosY - this.ScrollPosition.Y, true, true);
                }
            }
            else if (this.CameraTrackingThing != null)
            {
                var target = (this.CameraTrackingThing as Thing).GetWorldPosition();
                var pos = new Vector2f((target.X * 2f) - UIStatics.Graphics.Viewport.Width, (target.Y * 2f) - UIStatics.Graphics.Viewport.Height);

                // Scroll integer pixels
                var multiplier = (int)(this.Zoom / 2f);
                if (multiplier < 1)
                {
                    this.ScrollPosition.X = (int)(pos.X / 2) * 2;
                    this.ScrollPosition.Y = (int)(pos.Y / 2) * 2;
                }
                else
                {
                    this.ScrollPosition.X = (int)(pos.X * multiplier) / (float)multiplier;
                    this.ScrollPosition.Y = (int)(pos.Y * multiplier) / (float)multiplier;
                }
            }

            if (MouseManager.CurrentMouseOverElement != this)
            {
                MouseWorldPosition.Tile = null;
                for (int i = 0; i < 5; i++) MouseCursor.Instance.TextLine[i] = "";
            }

            this.fpsLabel.Text = GetString(StringsForGameScreen.FPS, PerfMonitor.UpdateFramesPerSecond, PerfMonitor.DrawFramesPerSecond, PerfMonitor.AverageUpdateTicks / 10000.0, PerfMonitor.AverageDrawTicks / 10000.0, PerfMonitor.ShadowTriCount, PerfMonitor.DrawsPerFrame);

            if (!this.IsPaused) this.isPausedForDialog = false;

            if (MouseWorldPosition.Tile != null && this.tileInfoLabel.IsVisible)
            {
                var tile = MouseWorldPosition.Tile;
                if (this.OverlayType == OverlayType.Resources)
                {
                    if (tile.IsMineResourceVisible)
                    {
                        var resource = tile.GetResources();
                        if (resource != null && resource.Type != ItemType.None && resource.Count > 0)
                        {
                            var type1 = LanguageManager.Get<ItemType>(resource.Type);
                            var densityStr = LanguageManager.Get<MineResourceDensity>(resource.Density);
                            this.tileInfoLabel.Text = GetString(StringsForGameScreen.MineResourceFormat, resource.Count, type1, densityStr);
                        }
                        else this.tileInfoLabel.Text = GetString(StringsForGameScreen.MineNoResource);
                    }
                    else this.tileInfoLabel.Text = "";
                }
                else if (this.OverlayType == OverlayType.Temperature)
                {
                    this.tileInfoLabel.Text = LanguageHelper.FormatTemperature(RoomManager.GetTileTemperature(tile.Index));
                }

                this.diagnosticLabel.Text = "";
            }
            else if (MouseWorldPosition.Tile != null && this.diagnosticLabel.IsVisible)
            {
                var tile = MouseWorldPosition.Tile;
                var isCorridorStr = tile.IsCorridor ? "true" : "false";
                var t = (int)RoomManager.GetTileTemperature(tile.Index);
                var l = (int)(RoomManager.GetTileLightLevel(tile.Index) * 100);
                this.diagnosticLabel.Text = $"Tile {tile.Index} ({tile.X}, {tile.Y}), T = {t}C, L = {l}%, terrain = {tile.TerrainType}, corridor = {isCorridorStr}";
                this.tileInfoLabel.Text = "";
            }
            else
            {
                this.diagnosticLabel.Text = "";
                this.tileInfoLabel.Text = "";
            }

            if (this.tileInfoLabel.Text != "") this.tileInfoLabel.Colour = World.ClimateType == ClimateType.Snow ? UIColour.WhiteText : UIColour.LightGrayText;

            var colonists = World.GetThings<IColonist>(ThingType.Colonist);
            if (colonists.Any() && colonists.All(c => c.IsDead) && !this.gameOverDialog.IsVisible)
            {
                this.PauseGameRequest?.Invoke(this, null);
                this.gameOverDialog.UpdateText();
                OpenDialog(this.gameOverDialog);
            }

            var y = UIStatics.Graphics.Viewport.Height - Scale(44);
            this.fpsLabel.Y = y;
            if (this.fpsLabel.IsVisible) y -= Scale(12);
            this.tileInfoLabel.Y = y;
            if (this.tileInfoLabel.IsVisible) y -= Scale(12);
            this.diagnosticLabel.Y = y;

            if (this.prevWidth != UIStatics.Graphics.Viewport.Width || this.prevHeight != UIStatics.Graphics.Viewport.Height)
            {
                var deltaW = prevWidth - UIStatics.Graphics.Viewport.Width;
                var deltaH = prevHeight - UIStatics.Graphics.Viewport.Height;
                this.ScrollPosition = new Vector2f(this.ScrollPosition.X + deltaW, this.ScrollPosition.Y + deltaH);
            }

            if (!this.IsPaused && !this.shownCheckListPanel && World.WorldTime.FrameNumber < 1800 && !this.panelManager.IsChecklistPanelShown && CheckListController.HaveItemsForDisplay)
            {
                // Display checklist panel at start of game
                this.panelManager.ShowChecklistPanel();
                this.Toolbar.SelectRightButton(this.Toolbar.ChecklistButton);
                this.shownCheckListPanel = true;
            }

            base.Update();
        }

        public void OnGameLoaded(GameLoadedEventArgs e)
        {
            this.SetZoom(e.ZoomLevel, true);
            this.ScrollPosition = e.ScrollPosition;
            this.HidePanels();
            this.IsRoofVisible = true;
            this.WidthHeightUpdate();   // Doing this here prevents scroll position from being changed
            this.commentaryHistoryPopup.ResetScrollPosition();
            this.shownRocketLaunchedDialog = false;
        }

        public void OnNewGame(int startTileIndex)
        {
            this.SetZoom(31, true);
            this.CenterOnTerrainPosition(World.GetSmallTile(startTileIndex).CentrePosition);
            this.HidePanels();
            this.ShowMothershipDialog();
            this.IsRoofVisible = true;
            this.commentaryHistoryPopup.ResetScrollPosition();
            this.shownRocketLaunchedDialog = false;
        }

        public void CenterOnTerrainPosition(Vector2f target)
        {
            this.ScrollPosition = new Vector2f((int)(target.X * 2) - this.prevWidth, (int)(target.Y * 2) - this.prevHeight);
        }

        public void ScrollToPosition(Vector2f target, int offsetX = 0, int offsetY = 0)
        {
            this.StartScrollPosition = this.ScrollPosition.Clone();
            this.EndScrollPosition = new Vector2f((int)(target.X * 2) - UIStatics.Graphics.Viewport.Width + offsetX, (int)(target.Y * 2) - UIStatics.Graphics.Viewport.Height + offsetY);
            this.ScrollMoveFrame = 0;
        }

        public void Saved(bool success)
        {
            if (success)
            {
                this.savedGameDialog.SetText(LanguageManager.Get<StringsForSavedGameDialog>(StringsForSavedGameDialog.Success), UIColour.DefaultText);
                this.frameLastSaved = World.WorldTime.FrameNumber;
            }
            else this.savedGameDialog.SetText(LanguageManager.Get<StringsForSavedGameDialog>(StringsForSavedGameDialog.Fail), UIColour.RedText);

            if (success && this.saveGameDialog.IsSaveForExit)
            {
                this.ResumeGameRequest?.Invoke(this, null);
                EventManager.RaiseEvent(EventType.GameExit, this);

                if (this.isRequestingExitToMainMenu) this.ExitToMainMenuRequest?.Invoke(this, new EventArgs());
                else this.ExitToDesktopRequest?.Invoke(this, new EventArgs());

                return;
            }

            OpenDialog(this.savedGameDialog);
        }

        public void ToggleFarmPanel()
        {
            this.panelManager.ToggleFarmPanel();
        }

        private void ToggleChecklistPanel()
        {
            this.panelManager.ShowChecklistPanel();
            this.Toolbar.SelectRightButton(this.panelManager.IsChecklistPanelShown ? this.Toolbar.ChecklistButton : null);
        }

        private void ToggleOptionsPanel()
        {
            this.panelManager.ShowOptionsPanel();
            this.Toolbar.SelectRightButton(this.panelManager.IsOptionsPanelShown ? this.Toolbar.OptionsButton : null);
        }

        public void ShowRelevantLeftPanels(IThing thing)
        {
            if (thing == null)
            {
                this.panelManager.HideLeft();
                return;
            }

            var lander = World.GetThings(ThingType.Lander).FirstOrDefault() as Lander;

            this.panelManager.LanderPanel.Thing = lander;
            this.panelManager.ConstructPanel.Lander = lander;
            this.Toolbar.Lander = lander;

            if (thing is IColonist c && c.MainTile.ThingsPrimary.OfType<ISleepPod>().FirstOrDefault() is ISleepPod pod)
            {
                // If we clicked on a colonist sleeping in a pod, then show both the colonist and the pod
                this.panelManager.ShowColonistAndSleepPodPanels(c, pod);
            }
            else this.panelManager.ShowRelevantPanel(thing);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.CurrentMouseState.LeftButton == ButtonState.Released && e.CurrentMouseState.RightButton == ButtonState.Released) this.UpdateMouseCursorForCurrentActivity();
            base.OnMouseMove(e);
        }

        private void UpdateMouseCursorForCurrentActivity()
        {
            if ((PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Build || PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.PlaceStackingArea) && this.IsMouseOverNotChildren)
            {
                MouseCursor.Instance.MouseCursorType = MouseCursorType.Hammer;
            }
            else if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Deconstruct && this.IsMouseOverNotChildren)
            {
                MouseCursor.Instance.MouseCursorType = MouseCursorType.Recycle;
            }
            else if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Harvest && this.IsMouseOverNotChildren)
            {
                MouseCursor.Instance.MouseCursorType = MouseCursorType.Harvest;
            }
            else if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Geology && this.IsMouseOverNotChildren)
            {
                MouseCursor.Instance.MouseCursorType = MouseCursorType.Geology;
            }
            else if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Farm && this.IsMouseOverNotChildren)
            {
                MouseCursor.Instance.MouseCursorType = MouseCursorType.Farm;
            }
            else
            {
                MouseCursor.Instance.MouseCursorType = MouseCursorType.Normal;
            }
        }

        protected override void OnMouseLeftClick(MouseEventArgs e)
        {
            this.HandleLeftClick();
            base.OnMouseLeftClick(e);
        }

        protected override void OnMouseLeftDragRelease(MouseEventArgs e)
        {
            this.HandleLeftClick();
            base.OnMouseLeftDragRelease(e);
        }

        protected override void OnMouseRightClick(MouseEventArgs e)
        {
            if (PlayerWorldInteractionManager.CurrentActivity != PlayerActivityType.None)
            {
                MouseCursor.Instance.MouseCursorType = MouseCursorType.Normal;
                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            }

            base.OnMouseRightClick(e);
        }

        private void HandleLeftClick()
        {
            this.hideUI = false;
            this.UpdateHighlights();

            if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Build)
            {
                PlayerWorldInteractionManager.Build();
            }
            else if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.PlaceStackingArea)
            {
                PlayerActivityPlaceStackingArea.HandleLeftClick();
            }
            else if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Deconstruct)
            {
                PlayerActivityDeconstruct.HandleLeftClick();
            }
            else if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Harvest)
            {
                PlayerActivityHarvest.HandleLeftClick();
            }
            else if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Geology)
            {
                PlayerActivityGeology.HandleLeftClick();
            }
            else if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Farm)
            {
                PlayerActivityFarm.HandleLeftClick();
            }
            else
            {
                PlayerWorldInteractionManager.SelectedThing = this.HighlightedThing;
                this.ShowRelevantLeftPanels(this.HighlightedThing);
            }
        }

        protected override void OnMouseRightDrag(MouseEventArgs e)
        {
            var deltaX = e.CurrentMouseState.X - e.LastMouseState.X;
            var deltaY = e.CurrentMouseState.Y - e.LastMouseState.Y;

            this.Scroll(-deltaX, -deltaY, false);

            MouseCursor.Instance.MouseCursorType = MouseCursorType.Drag;

            base.OnMouseRightDrag(e);
        }

        protected override void OnMouseRightDragRelease(MouseEventArgs e)
        {
            if (MouseCursor.Instance.MouseCursorType == MouseCursorType.Drag) this.UpdateMouseCursorForCurrentActivity();
            base.OnMouseRightDragRelease(e);
        }

        protected override void OnMouseScrollUp(MouseEventArgs e)
        {
            this.isZoomingUsingScollWheel = true;
            this.ZoomIn();
        }

        protected override void OnMouseScrollDown(MouseEventArgs e)
        {
            this.isZoomingUsingScollWheel = true;
            this.ZoomOut();
        }

        public void ZoomOut()
        {
            if (this.ZoomLevel == 127)
            {
                this.SetZoom(63);
            }
            else if (this.ZoomLevel == 63)
            {
                this.SetZoom(31);
            }
            else if (this.ZoomLevel == 31)
            {
                this.SetZoom(15);
            }
            else if (this.ZoomLevel == 15)
            {
                this.SetZoom(7);
            }
        }

        public void ZoomIn()
        {
            if (this.ZoomLevel < 15)
            {
                this.SetZoom(15);
            }
            else if (this.ZoomLevel < 31)
            {
                this.SetZoom(31);
            }
            else if (this.ZoomLevel < 63)
            {
                this.SetZoom(63);
            }
            else if (this.ZoomLevel < 127)
            {
                this.SetZoom(127);
            }
        }

        public void UpdateHighlights()
        {
            var activity = PlayerWorldInteractionManager.CurrentActivity;
            this.HighlightedThing = null;

            if (activity == PlayerActivityType.Deconstruct || activity == PlayerActivityType.Harvest)
            {
                this.HighlightedThing = PlayerWorldInteractionManager.GetThingUnderCursor(ThingTypeManager.DeconstructableThingTypes, true);
            }
            else if (activity != PlayerActivityType.Build && activity != PlayerActivityType.PlaceStackingArea)
            {
                this.HighlightedThing = PlayerWorldInteractionManager.GetThingUnderCursor(ThingTypeManager.SelectableThingTypes, false);
                if (this.HighlightedThing?.ThingType == ThingType.LanderPanel) this.HighlightedThing = World.GetThings(ThingType.Lander).FirstOrDefault();
            }
        }

        private void ToolbarCheckListButtonClick(object sender, EventArgs e)
        {
            this.ToggleChecklistPanel();
        }

        private void ToolbarOptionsButtonClick(object sender, EventArgs e)
        {
            this.ToggleOptionsPanel();
        }

        private void OnOptionsAchievementsClick(object sender, EventArgs e)
        {
            this.ToggleOptionsPanel();
            this.panelManager.ShowAchievementsPanel();
        }

        private void OnOptionsPanelSaveGameClick(object sender, EventArgs e)
        {
            this.PauseGameRequest?.Invoke(this, null);
            this.OpenDialog(this.saveGameDialog);
            this.saveGameDialog.IsSaveForExit = false;
            this.saveGameDialog.UpdateButtons();
        }

        private void OnOptionsPanelSettingsClick(object sender, EventArgs e)
        {
            this.PauseGameRequest?.Invoke(this, null);
            this.optionsDialog.ResetSettings(this.graphicsManager.IsFullScreen);
            this.OpenDialog(this.optionsDialog);
        }

        private void OnOptionsPanelKeyboardControlsClick(object sender, EventArgs e)
        {
            this.PauseGameRequest?.Invoke(this, null);
            this.keyboardControlsDialog.Reset();
            this.OpenDialog(this.keyboardControlsDialog);
        }

        private void OnOptionsPanelExitToMainMenuClick(object sender, EventArgs e)
        {
            this.isRequestingExitToMainMenu = true;
            this.PauseGameRequest?.Invoke(this, null);

            if (this.ShouldConfirmExit)
            {
                this.OpenDialog(this.confirmExitDialog);
            }
            else
            {
                EventManager.RaiseEvent(EventType.GameExit, this);
                this.ExitToMainMenuRequest?.Invoke(this, new EventArgs());
            }
        }

        private void OnOptionsPanelExitToDesktopClick(object sender, EventArgs e)
        {
            this.isRequestingExitToMainMenu = false;
            this.PauseGameRequest?.Invoke(this, null);

            if (this.ShouldConfirmExit)
            {
                this.OpenDialog(this.confirmExitDialog);
            }
            else
            {
                EventManager.RaiseEvent(EventType.GameExit, this);
                this.ExitToDesktopRequest?.Invoke(this, new EventArgs());
            }
        }

        private void OnRocketLaunched()
        {
            this.rocketLaunchedDialog.WasSnowRegionUnlocked = World.ClimateType != ClimateType.Snow && !GameDataManager.IsSnowRegionUnlocked;
            GameDataManager.UnlockSnowRegion();

            if (this.shownRocketLaunchedDialog) return;

            this.shownRocketLaunchedDialog = true;
            this.panelManager.HideLeft();
            this.PauseGameRequest?.Invoke(this, null);
            this.rocketLaunchedDialog.UpdateText();
            MouseCursor.Instance.MouseCursorType = MouseCursorType.Normal;
            WorldStats.Increment(WorldStatKeys.RocketsLaunched);
            this.OpenDialog(this.rocketLaunchedDialog);
        }

        protected override void OpenDialog(Dialog dialog)
        {
            this.hideUI = false;
            this.HidePanels();
            base.OpenDialog(dialog);
        }

        private void OnGameLoaded()
        {
            if (KeyboardManager.FocusedElement != this.mothershipDialog) KeyboardManager.FocusedElement = this;

            // Deselect all
            this.Toolbar.SelectRightButton(null);

            if (World.GetThings(ThingType.Lander).FirstOrDefault() is Lander lander)
            {
                this.panelManager.LanderPanel.Thing = lander;
                this.panelManager.ConstructPanel.Lander = lander;
                this.Toolbar.Lander = lander;
            }

            this.frameLastSaved = World.WorldTime.FrameNumber;
        }

        private void OnThingRemoved(object obj)
        {
            if (PlayerWorldInteractionManager.SelectedThing == obj)
            {
                PlayerWorldInteractionManager.SelectedThing = null;
                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            }

            // If thing has a panel open, then hide it
            if (obj is IThing thing) this.panelManager.CloseIfOpen(thing);
        }

        private void OnRocketLaunchClick(object obj)
        {
            var building = obj as Building;
            PlayerWorldInteractionManager.SelectedThing = null;
            PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            this.SetZoom(63);
            this.ScrollToPosition(building.MainTile.CentrePosition, 0, -50);
            this.panelManager.HideAll();
        }

        private void OnRocketLaunchStart()
        {
            ScreenShaker.Shake();
        }

        private void OnDeleteGameDialogClose(object sender, EventArgs e)
        {
            this.deleteGameDialog.IsVisible = false;
            this.prevDialog.IsVisible = true;
            if (this.prevDialog is LoadGameDialog loadGameDialog) loadGameDialog.UpdateButtons();
            else if (this.prevDialog is SaveGameDialog saveGameDialog) saveGameDialog.UpdateButtons();
        }

        private void OnSaveGameDialogSave(object sender, EventArgs e)
        {
            this.CloseDialog(this.saveGameDialog);
            this.SaveRequested?.Invoke(this, new SaveRequestEventArgs(this.saveGameDialog.FileName));
        }

        private void OnSaveGameDialogDelete(object sender, EventArgs e)
        {
            this.saveGameDialog.IsVisible = false;
            this.prevDialog = this.saveGameDialog;
            this.deleteGameDialog.IsVisible = true;
            this.deleteGameDialog.FileName = this.saveGameDialog.FileName;
            this.deleteGameDialog.IsAutosave = false;
        }

        private void OnSaveGameDialogCancel(object sender, EventArgs e)
        {
            CloseDialog(this.saveGameDialog);
            this.ResumeGameRequest?.Invoke(this, null);
        }

        private void OnSavedGameDialogClose(object sender, EventArgs e)
        {
            CloseDialog(this.savedGameDialog);
            this.ResumeGameRequest?.Invoke(this, null);
        }

        private void OnErrorDialogClose(object sender, EventArgs e)
        {
            CloseDialog(this.errorDialog);
            this.ResumeGameRequest?.Invoke(this, null);
        }

        protected override void ConfirmSettingsDialogOk(object sender, EventArgs e)
        {
            base.ConfirmSettingsDialogOk(sender, e);
            this.ResumeGameRequest?.Invoke(this, null);
            this.panelManager.HideRight();
            this.Toolbar.OptionsButton.IsSelected = false;
        }

        protected override void ConfirmSettingsDialogCancel(object sender, EventArgs e)
        {
            base.ConfirmSettingsDialogCancel(sender, e);
            this.ResumeGameRequest?.Invoke(this, null);
        }

        private void OnConfirmExitDialogOk(object sender, EventArgs e)
        {
            CloseDialog(this.confirmExitDialog);

            this.AutosaveRequested?.Invoke(this, new EventArgs());
            EventManager.RaiseEvent(EventType.GameExit, this);

            if (this.isRequestingExitToMainMenu)
            {
                this.ExitToMainMenuRequest?.Invoke(this, new EventArgs());
            }
            else
            {
                this.ExitToDesktopRequest?.Invoke(this, new EventArgs());
            }
        }

        private void OnConfirmExitDialogSave(object sender, EventArgs e)
        {
            CloseDialog(this.confirmExitDialog);
            OpenDialog(this.saveGameDialog);
            this.saveGameDialog.IsSaveForExit = true;
            this.saveGameDialog.UpdateButtons();
        }

        private void OnConfirmExitDialogCancel(object sender, EventArgs e)
        {
            CloseDialog(this.confirmExitDialog);
            this.ResumeGameRequest?.Invoke(this, null);
        }

        private void OnCommentaryHistoryPopupArchiveButtonClick(object sender, EventArgs e)
        {
            this.ShowCommentArchiveDialog();
        }

        protected override void OnOptionsDialogOk(object sender, EventArgs e)
        {
            base.OnOptionsDialogOk(sender, e);
            if ((!isChangeScreenResRequested && !isToggleFullScreenRequested) || autoConfirmSettingsChange) this.ResumeGameRequest?.Invoke(this, null);
        }

        protected override void OnOptionsDialogClose(object sender, EventArgs e)
        {
            base.OnOptionsDialogClose(sender, e);
            this.ResumeGameRequest?.Invoke(this, null);
        }

        private void OnRocketLaunchedDialogContinue(object sender, EventArgs e)
        {
            CloseDialog(this.rocketLaunchedDialog);
            this.ResumeGameRequest?.Invoke(this, null);
        }

        private void OnExitToMenuClick(object sender, EventArgs e)
        {
            CloseDialog(this.gameOverDialog);
            EventManager.RaiseEvent(EventType.GameExit, this);
            this.ExitToMainMenuRequest?.Invoke(this, new EventArgs());
        }

        private void OnRocketLaunchedExitClick(object sender, EventArgs e)
        {
            CloseDialog(this.rocketLaunchedDialog);
            if (this.ShouldConfirmExit) this.AutosaveRequested?.Invoke(this, new EventArgs());
            EventManager.RaiseEvent(EventType.GameExit, this);
            this.ExitToMainMenuRequest?.Invoke(this, new EventArgs());
        }

        private void OnZoomInButtonClick(object sender, EventArgs e)
        {
            this.isZoomingUsingScollWheel = false;
            this.ZoomIn();
        }

        private void OnZoomOutButtonClick(object sender, EventArgs e)
        {
            this.isZoomingUsingScollWheel = false;
            this.ZoomOut();
        }

        private void OnPauseButtonClick(object sender, EventArgs e)
        {
            this.PauseClick?.Invoke(this, null);
        }

        private void OnPlayButtonClick(object sender, EventArgs e)
        {
            this.PlayClick?.Invoke(this, null);
        }

        private void OnFastForwardButtonClick(object sender, EventArgs e)
        {
            this.IncreaseSpeedClick?.Invoke(this, null);
        }

        private void OnToggleRoofButtonClick(object sender, EventArgs e)
        {
            this.IsRoofVisible = this.StatusBar.IsRoofVisible;
            if (!this.StatusBar.IsRoofVisible && PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Build && PlayerWorldInteractionManager.CurrentThingTypeToBuild == ThingType.Roof)
            {
                // Cancel roof building if we hide roofs
                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            }

            this.ToggleRoofClick?.Invoke(this, null);
        }

        private void OnRecycleButtonClick(object sender, EventArgs e)
        {
            PlayerWorldInteractionManager.SetCurrentActivity(PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Deconstruct ? PlayerActivityType.None : PlayerActivityType.Deconstruct, PlayerActivitySubType.None);
        }

        private void OnHarvestButtonClick(object sender, EventArgs e)
        {
            PlayerWorldInteractionManager.SetCurrentActivity(PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Harvest ? PlayerActivityType.None : PlayerActivityType.Harvest, PlayerActivitySubType.None);
        }

        private void OnGeologyButtonClick(object sender, EventArgs e)
        {
            PlayerWorldInteractionManager.SetCurrentActivity(PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Geology ? PlayerActivityType.None : PlayerActivityType.Geology, PlayerActivitySubType.None);
        }

        private void OnMothershipButtonClick(object sender, EventArgs e)
        {
            this.ShowMothershipDialog();
        }

        private void OnCommentaryTickerClick(object sender, EventArgs e)
        {
            this.ToggleCommentaryHistoryPanel();
        }

        private void OnResourceOverlayButtonClick(object sender, EventArgs e)
        {
            this.ResourceOverlayClick?.Invoke(this, null);
            if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Geology && this.OverlayType != OverlayType.Resources)
            {
                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
            }
        }

        private void OnTemperatureOverlayButtonClick(object sender, EventArgs e)
        {
            this.TemperatureOverlayClick?.Invoke(this, null);
        }

        private void OnConstructButtonClick(object sender, EventArgs e)
        {
            this.panelManager.ToggleConstructPanel();
        }

        private void OnFarmButtonClick(object sender, EventArgs e)
        {
            this.CropOverlayClick?.Invoke(this, null);
        }

        private void OnCurrentActivityChanged(object sender, EventArgs e)
        {
            if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Build && PlayerWorldInteractionManager.CurrentThingTypeToBuild == ThingType.Roof)
            {
                this.IsRoofVisible = true;
            }
        }

        private void OnFarmPanelClose(object sender, EventArgs e)
        {
            this.FarmPanelClose?.Invoke(sender, new EventArgs());
        }

        private void OnPanelDeconstructFoundationClick(object sender, EventArgs e)
        {
            if (!(sender is IThingPanel panel) || panel.Thing == null) return;

            var foundation = panel.Thing.AllTiles.SelectMany(t => t.ThingsAll).OfType<IFoundation>().FirstOrDefault();
            if (foundation != null) PlayerActivityDeconstruct.Deconstruct(foundation);
        }

        private void OnPanelDeconstructConduitNodeClick(object sender, EventArgs e)
        {
            if (!(sender is BuildingPanel panel) || panel.Thing == null) return;

            var conduitNode = panel.Thing.AllTiles.SelectMany(t => t.ThingsAll).OfType<IConduitNode>().FirstOrDefault();
            if (conduitNode != null) PlayerActivityDeconstruct.Deconstruct(conduitNode);
        }

        #region IKeyboardHandler implementation
        public override void HandleKeyHold(Keys key)
        {
            this.HandleKeyRelease(key);
        }

        public override void HandleKeyRelease(Keys key)
        {
            this.ProcessKeyPress(key);
        }

        private void ProcessKeyPress(Keys key)
        {
            if (this.panelManager.OptionsPanel.IsShown && this.panelManager.OptionsPanel.HandleKeyPress(key)) return;

            var keyName = key.ToString();
            if (key == Keys.OemOpenBrackets) keyName = "[";
            else if (key == Keys.OemCloseBrackets) keyName = "]";
            else if (key == Keys.OemPlus || key == Keys.Add) keyName = "+";
            else if (key == Keys.OemMinus || key == Keys.Subtract) keyName = "-";

            var setting = SettingsManager.GetKeySetting(keyName, KeyboardManager.IsAlt, KeyboardManager.IsCtrl, KeyboardManager.IsShift);
            if (!string.IsNullOrWhiteSpace(setting))
            {
                setting = setting.ToLowerInvariant();
                if (setting.Contains(":"))
                {
                    var parts = setting.Split(':');
                    var group = parts[0].ToLowerInvariant();
                    var value = parts[1].ToLowerInvariant();
                    switch (group)
                    {
                        case "build" when Enum.TryParse(parts[1], out ThingType t):
                            var definition = ThingTypeManager.GetDefinition(t, false);
                            if (t == ThingType.StackingArea)
                            {
                                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.PlaceStackingArea, PlayerActivitySubType.PlaceStackingArea, t);
                            }
                            else if (definition?.CanBuild == true)
                            {
                                PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.Build, PlayerActivitySubType.BuildFactory, t);
                            }
                            break;
                        case "gamespeed" when value == "increase":
                            this.IncreaseSpeedClick?.Invoke(this, new EventArgs());
                            break;
                        case "gamespeed" when value == "decrease":
                            this.DecreaseSpeedClick?.Invoke(this, new EventArgs());
                            break;
                        case "locate" when Enum.TryParse(parts[1], out ThingType t2):
                            this.ThingTypeToLocate = t2;
                            this.LocateRequest?.Invoke(this, new EventArgs());
                            break;
                        case "zoom" when value == "out":
                            this.ZoomOut();
                            break;
                        case "zoom" when value == "in":
                            this.ZoomIn();
                            break;
                        case "rotateblueprint":
                            if (group == "rotateblueprint"
                                                    && PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Build
                                                    && PlayerWorldInteractionManager.CurrentThingTypeToBuild.HasValue
                                                    && ThingTypeManager.GetDefinition(PlayerWorldInteractionManager.CurrentThingTypeToBuild.Value)?.CanRotate == true
                                                    && ThingTypeManager.GetDefinition(PlayerWorldInteractionManager.CurrentThingTypeToBuild.Value)?.AutoRotate == false)
                            {
                                if (value == "left") PlayerWorldInteractionManager.RotateBlueprintLeft();
                                else if (value == "right") PlayerWorldInteractionManager.RotateBlueprintRight();
                            }
                            break;
                    }
                }
                else if (setting == "construct")
                {
                    this.panelManager.ToggleConstructPanel();
                }
                else if (setting == "commentarchive")
                {
                    this.ShowCommentArchiveDialog();
                }
                else if (setting == "farm")
                {
                    this.CropOverlayClick?.Invoke(this, new EventArgs());
                }
                else if (setting == "resourcemap")
                {
                    this.ResourceOverlayClick?.Invoke(this, new EventArgs());
                    if (PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Geology && this.OverlayType != OverlayType.Resources)
                    {
                        PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
                    }
                }
                else if (setting == "temperature")
                {
                    this.TemperatureOverlayClick?.Invoke(this, new EventArgs());
                }
                else if (setting == "mothership")
                {
                    this.ShowMothershipDialog();
                }
                else if (setting == "harvest")
                {
                    PlayerWorldInteractionManager.SetCurrentActivity(PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Harvest ? PlayerActivityType.None : PlayerActivityType.Harvest, PlayerActivitySubType.None);
                }
                else if (setting == "geology")
                {
                    PlayerWorldInteractionManager.SetCurrentActivity(PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Geology ? PlayerActivityType.None : PlayerActivityType.Geology, PlayerActivitySubType.None);
                }
                else if (setting == "deconstruct")
                {
                    PlayerWorldInteractionManager.SetCurrentActivity(PlayerWorldInteractionManager.CurrentActivity == PlayerActivityType.Deconstruct ? PlayerActivityType.None : PlayerActivityType.Deconstruct, PlayerActivitySubType.None);
                }
                else if (setting == "options")
                {
                    if (this.hideUI) this.hideUI = false;
                    if (PlayerWorldInteractionManager.CurrentActivity != PlayerActivityType.None)
                    {
                        MouseCursor.Instance.MouseCursorType = MouseCursorType.Normal;
                        PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
                    }
                    else if (PlayerWorldInteractionManager.SelectedThing != null || this.panelManager.CurrentPanels.Any() || this.commentaryHistoryPopup.IsShown)
                    {
                        PlayerWorldInteractionManager.SelectedThing = null;
                        this.HidePanels();
                    }
                    else
                    {
                        this.ToggleOptionsPanel();
                    }
                }
                else if (setting == "help")
                {
                    this.ToggleChecklistPanel();
                }
                else if (setting == "debug")
                {
                    this.ShowDebugDialog();
                }
                else if (setting == "toggleframerate")
                {
                    this.IsFpsVisible = !this.IsFpsVisible;
                }
                else if (setting == "togglefullscreen")
                {
                    this.ToggleFullScreen();
                }
                else if (setting == "toggleui")
                {
                    this.hideUI = !this.hideUI;
                }
                else if (setting == "togglehomezone")
                {
                    this.ZoneOverlayClick?.Invoke(this, new EventArgs());
                }
                else if (setting == "togglepause")
                {
                    this.TogglePauseClick?.Invoke(this, new EventArgs());
                }
                else if (setting == "toggleroof")
                {
                    this.IsRoofVisible = !this.IsRoofVisible;
                }
                else if (setting == "cameratrack")
                {
                    this.CameraTrackSelectedThing();
                }
                else if (setting == "screenshot")
                {
                    this.ScreenshotRequest?.Invoke(this, new EventArgs());
                }
                else if (setting == "screenshotnoui")
                {
                    this.ScreenshotNoUIRequest?.Invoke(this, new EventArgs());
                }
            }
            else if (key == Keys.Escape)
            {
                if (this.hideUI) this.hideUI = false;
                if (PlayerWorldInteractionManager.CurrentActivity != PlayerActivityType.None)
                {
                    MouseCursor.Instance.MouseCursorType = MouseCursorType.Normal;
                    PlayerWorldInteractionManager.SetCurrentActivity(PlayerActivityType.None, PlayerActivitySubType.None);
                }
                else if (PlayerWorldInteractionManager.SelectedThing != null || this.panelManager.CurrentPanels.Any() || this.commentaryHistoryPopup.IsShown)
                {
                    PlayerWorldInteractionManager.SelectedThing = null;
                    this.HidePanels();
                }
            }
        }

        #endregion

        private static string GetString(StringsForGameScreen value)
        {
            return LanguageManager.Get<StringsForGameScreen>(value);
        }

        private static string GetString(StringsForGameScreen value, object arg1, object arg2, object arg3)
        {
            return LanguageManager.Get<StringsForGameScreen>(value, arg1, arg2, arg3);
        }

        private static string GetString(StringsForGameScreen value, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            return LanguageManager.Get<StringsForGameScreen>(value, arg1, arg2, arg3, arg4, arg5, arg6);
        }
    }
}
