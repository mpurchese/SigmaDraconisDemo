namespace SigmaDraconis.UI
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    using Draconis.Input;
    using Draconis.Shared;
    using Draconis.UI;
    using IO;
    using Language;
    using Settings;
    using Shared;

    public class MenuScreen : ScreenBase
    {
        private Texture2D mainTexture = null;
        private Texture2D titleTexture = null;
        private Texture2D versionTexture = null;
        private Texture2D fullscreenTexture = null;
        private Texture2D loadingTexture = null;
        private int mainTextureAlpha = 255;
        private bool isFadingOut = false;
        private bool isReadyToStartGame = false;
        private string loadGameFileName = "";
        private bool isLoadGameAutosave;
        private ClimateType selectedClimateType = ClimateType.Normal;

        private readonly MainMenuOptionButton newGameButton;
        private readonly MainMenuOptionButton loadGameButton;
        private readonly MainMenuOptionButton settingsButton;
        private readonly MainMenuOptionButton creditsButton;
        private readonly MainMenuOptionButton exitButton;

        private readonly LoadGameDialog loadGameDialog;
        private readonly DeleteGameDialog deleteGameDialog;
        private readonly CreditsDialog creditsDialog;
        private readonly NewGameDialog newGameDialog;
        private readonly ErrorDialog errorDialog;

        private readonly IconButton englishButton;
        private readonly IconButton germanButton;
        private readonly IconButton twitterButton;
        private readonly IconButton facebookButton;

        private readonly SimpleTooltip englishTooltip;
        private readonly SimpleTooltip germanTooltip;

        public static MenuScreen Instance { get; private set; }

        public EmptyElement TooltipParent { get; }

        public event EventHandler<LoadGameEventArgs> LoadGameRequest;
        public event EventHandler<NewGameEventArgs> NewGameRequest;
        public event EventHandler<EventArgs> ExitRequest;

        public MenuScreen(GameWindow gameWindow, GraphicsDeviceManager deviceManager)
            : base(gameWindow, deviceManager)
        {
            if (Instance != null) throw new ApplicationException("MenuScreen instance already created");
            Instance = this;

            this.newGameButton = new MainMenuOptionButton(this, 0.607, 0.215, "NewGame");
            this.loadGameButton = new MainMenuOptionButton(this, 0.607, 0.275, "LoadGame");
            this.settingsButton = new MainMenuOptionButton(this, 0.607, 0.335, "Settings");
            this.creditsButton = new MainMenuOptionButton(this, 0.607, 0.395, "Credits");
            this.exitButton = new MainMenuOptionButton(this, 0.607, 0.455, "Exit");

            this.englishButton = new IconButton(this, (this.W / 2) - 52, this.H - 28, "Textures\\Menu\\English\\Flag") { IsSelected = LanguageManager.CurrentLanguage == "English", Tag = "English" };
            this.germanButton = new IconButton(this, (this.W / 2) + 4, this.H - 28, "Textures\\Menu\\Deutsch\\Flag") { IsSelected = LanguageManager.CurrentLanguage == "Deutsch", Tag = "Deutsch" };

            this.twitterButton = new IconButton(this, this.W - 72, this.H - 36, "Textures\\Icons\\Twitter", 50 / (float)UIStatics.Scale)
            {
                BorderColour1 = new Color(0, 0, 0, 0),
                BorderColour2 = new Color(0, 0, 0, 0),
                AnchorLeft = false,
                AnchorRight = true,
                AnchorTop = false,
                AnchorBottom = true
            };

            this.facebookButton = new IconButton(this, this.W - 36, this.H - 36, "Textures\\Icons\\Facebook", 50 / (float)UIStatics.Scale)
            {
                BorderColour1 = new Color(0, 0, 0, 0),
                BorderColour2 = new Color(0, 0, 0, 0),
                AnchorLeft = false,
                AnchorRight = true,
                AnchorTop = false,
                AnchorBottom = true
            };

            this.AddChild(this.englishButton);
            this.AddChild(this.germanButton);
            this.AddChild(this.twitterButton);
            this.AddChild(this.facebookButton);

            this.newGameButton.MouseLeftClick += this.OnNewGameButtonClick;
            this.loadGameButton.MouseLeftClick += this.OnLoadGameButtonClick;
            this.settingsButton.MouseLeftClick += this.OnOptionsButtonClick;
            this.creditsButton.MouseLeftClick += this.OnCreditsButtonClick;
            this.exitButton.MouseLeftClick += this.OnExitButtonClick;
            this.facebookButton.MouseLeftClick += this.OnFacebookButtonClick;
            this.twitterButton.MouseLeftClick += this.OnTwitterButtonClick;

            this.AddChild(this.newGameButton);
            this.AddChild(this.loadGameButton);
            this.AddChild(this.settingsButton);
            this.AddChild(this.creditsButton);
            this.AddChild(this.exitButton);

            this.TooltipParent = new EmptyElement(this, 0, 0, this.W, this.H) { IsInteractive = false };

            this.newGameDialog = new NewGameDialog(this);
            this.loadGameDialog = new LoadGameDialog(this);
            this.deleteGameDialog = new DeleteGameDialog(this);
            this.creditsDialog = new CreditsDialog(this);
            this.errorDialog = new ErrorDialog(this);

            this.AddChild(this.newGameDialog);
            this.AddChild(this.loadGameDialog);
            this.AddChild(this.deleteGameDialog);
            this.AddChild(this.creditsDialog);
            this.AddChild(this.errorDialog);

            this.AddChild(this.TooltipParent);

            this.englishTooltip = new SimpleTooltip(this.TooltipParent, this.englishButton, LanguageManager.Get<StringsForFlagTooltips>(StringsForFlagTooltips.English));
            this.germanTooltip = new SimpleTooltip(this.TooltipParent, this.germanButton, LanguageManager.Get<StringsForFlagTooltips>(StringsForFlagTooltips.German));
            this.TooltipParent.AddChild(this.englishTooltip);
            this.TooltipParent.AddChild(this.germanTooltip);

            this.newGameDialog.NewGame += this.OnNewGameDialogNewGame;
            this.newGameDialog.Cancel += this.OnNewGameDialogCancel;

            this.loadGameDialog.LoadClick += this.LoadGameDialogStart;
            this.loadGameDialog.CancelClick += this.LoadGameDialogCancel;
            this.loadGameDialog.DeleteClick += this.LoadGameDialogDelete;

            this.deleteGameDialog.CloseClick += this.DeleteGameDialogClose;

            this.creditsDialog.CloseClick += this.CreditsDialogClose;

            this.errorDialog.ContinueClick += this.ErrorDialogClose;

            this.englishButton.MouseLeftClick += this.OnLanguageButtonClick;
            this.germanButton.MouseLeftClick += this.OnLanguageButtonClick;

            this.InitBaseDialogs(this);

            previousIsFullScreen = graphicsManager.IsFullScreen;
            previousDisplayWidth = UIStatics.Graphics.Viewport.Width;
            previousDisplayHeight = UIStatics.Graphics.Viewport.Height;
        }

        public override void LoadContent()
        {
            this.spriteBatch = new SpriteBatch(UIStatics.Graphics);
            this.mainTexture = UIStatics.Content.Load<Texture2D>("Textures\\Menu\\MainMenu");
            this.titleTexture = UIStatics.Content.Load<Texture2D>("Textures\\Menu\\MainMenuTitle");
            this.versionTexture = UIStatics.Content.Load<Texture2D>("Textures\\Menu\\Version");
            this.fullscreenTexture = UIStatics.Content.Load<Texture2D>($"Textures\\Menu\\{LanguageManager.CurrentLanguage}\\FullscreenHint");
            this.loadingTexture = UIStatics.Content.Load<Texture2D>($"Textures\\Menu\\{LanguageManager.CurrentLanguage}\\Loading");

            base.LoadContent();
        }

        public void ShowErrorDialog(string title, string error)
        {
            this.errorDialog.SetMessage(title, error);
            this.OpenDialog(this.errorDialog);
        }

        public override void Update()
        {
            this.DoDisplaySettings();

            var showMenuButtons = !this.isFadingOut && this.Children.OfType<Dialog>().All(d => !d.IsVisible);
            foreach (var button in this.Children.OfType<MainMenuOptionButton>())
            {
                button.IsVisible = showMenuButtons;
            }

            this.twitterButton.IsVisible = this.mainTextureAlpha == 255;
            this.facebookButton.IsVisible = this.mainTextureAlpha == 255;
            this.englishButton.IsVisible = this.mainTextureAlpha == 255;
            this.germanButton.IsVisible = this.mainTextureAlpha == 255;

            if (this.W != UIStatics.Graphics.Viewport.Width)
            {
                this.W = UIStatics.Graphics.Viewport.Width;
                GameScreen.Instance.W = this.W;
            }

            if (this.H != UIStatics.Graphics.Viewport.Height)
            {
                this.H = UIStatics.Graphics.Viewport.Height;
                GameScreen.Instance.H = this.H;
            }

            if (this.isFadingOut)
            {
                this.mainTextureAlpha -= 10;
                if (this.mainTextureAlpha < 0)
                {
                    this.mainTextureAlpha = 0;
                }
            }

            base.Update();

            if (this.isReadyToStartGame)
            {
                if (!string.IsNullOrEmpty(this.loadGameFileName))
                {
                    this.LoadGameRequest?.Invoke(this, new LoadGameEventArgs(this.loadGameFileName, this.isLoadGameAutosave));
                }
                else
                {
                    this.isReadyToStartGame = false;
                    this.NewGameRequest?.Invoke(this, new NewGameEventArgs(64, this.selectedClimateType));
                }
            }
        }

        public void Reset()
        {
            this.loadGameFileName = "";
            this.isReadyToStartGame = false;
            this.mainTextureAlpha = 255;
            this.isFadingOut = false;
            this.IsVisible = false;
        }

        protected override void DrawContent()
        {
            UIStatics.Graphics.Clear(Color.Black);

            Rectangle r = new Rectangle(this.ScreenX, this.ScreenY, this.W, this.H);
            var colour = new Color(this.mainTextureAlpha, this.mainTextureAlpha, this.mainTextureAlpha);

            var scaleX = this.W / (double)this.mainTexture.Width;
            var scaleY = this.H / (double)this.mainTexture.Height;
            var titleRect = new Rectangle((int)(181 * scaleX), (int)(30 * scaleY), (int)(1560 * scaleX), (int)(118 * scaleY));
            var versionRect = new Rectangle(this.W - 84 - this.versionTexture.Width, this.H - 35, this.versionTexture.Width, this.versionTexture.Height);
            var fullscreenHintRect = new Rectangle(0, this.H - this.fullscreenTexture.Height, this.fullscreenTexture.Width, this.fullscreenTexture.Height);

            var isFullScreenHintVisible = !graphicsManager.IsFullScreen && this.mainTextureAlpha == 255 && SettingsManager.GetFirstKeyForAction("ToggleFullScreen") == "F11";

            // Crop background graphic to maintain aspect ratio
            var backgroundSourceRect = new Rectangle(0, 0, this.mainTexture.Width, this.mainTexture.Height);
            if (UIStatics.Graphics.Viewport.AspectRatio < backgroundSourceRect.Width / (float)backgroundSourceRect.Height)
            {
                var w = UIStatics.Graphics.Viewport.AspectRatio * backgroundSourceRect.Height;
                backgroundSourceRect.X = (int)(backgroundSourceRect.Width - w);
                backgroundSourceRect.Width -= backgroundSourceRect.X;
            }

            foreach (var btn in this.Children.OfType<MainMenuOptionButton>())
            {
                btn.SetScale(scaleX, scaleY, -backgroundSourceRect.X * scaleX * 0.5);
            }

            this.spriteBatch.Begin();
            this.spriteBatch.Draw(this.mainTexture, r, backgroundSourceRect, colour);
            this.spriteBatch.Draw(this.titleTexture, titleRect, Color.White);
            this.spriteBatch.Draw(this.versionTexture, versionRect, Color.White);
            if (isFullScreenHintVisible) this.spriteBatch.Draw(this.fullscreenTexture, fullscreenHintRect, Color.White);
            this.spriteBatch.End();

            this.spriteBatch.Begin(blendState: BlendState.NonPremultiplied);
            if (this.mainTextureAlpha > 0)
            {
                var loadingRect = new Rectangle((int)((1920 - this.loadingTexture.Width) * scaleX * 0.5), (int)((1080 - this.loadingTexture.Height) * scaleY * 0.5), this.loadingTexture.Width, this.loadingTexture.Height);
                this.spriteBatch.Draw(this.loadingTexture, loadingRect, new Color(Color.White, 255 - this.mainTextureAlpha));
            }
            else
            {
                var loadingRect = new Rectangle((int)((1920 - this.loadingTexture.Width) * scaleX * 0.5), (int)((1080 - this.loadingTexture.Height) * scaleY * 0.5), this.loadingTexture.Width, this.loadingTexture.Height);
                this.spriteBatch.Draw(this.loadingTexture, loadingRect, Color.White);
                this.isReadyToStartGame = true;
            }

            this.spriteBatch.End();

            this.IsContentChangedSinceDraw = false;
        }

        public override void HandleKeyRelease(Keys key)
        {
            if (SettingsManager.GetKeysForAction("ToggleFullScreen").Contains(key.ToString()))
            {
                this.ToggleFullScreen();
            }

            if (!newGameButton.IsVisible) return;   // Buttons not visible

            if (key == Keys.Space || key == Keys.Enter)
            {
                if (this.newGameButton.IsMouseOver)
                {
                    this.OnNewGameButtonClick(null, null);
                }
                else if (this.loadGameButton.IsMouseOver)
                {
                    this.OnLoadGameButtonClick(null, null);
                }
                else if (this.settingsButton.IsMouseOver)
                {
                    this.OnOptionsButtonClick(null, null);
                }
                else if (this.creditsButton.IsMouseOver)
                {
                    this.OnCreditsButtonClick(null, null);
                }
                else if (this.exitButton.IsMouseOver)
                {
                    this.OnExitButtonClick(null, null);
                }
            }
        }

        public override void ApplyLayout()
        {
            foreach (var dialog in this.Children.OfType<Dialog>())
            {
                dialog.ApplyScale();
                dialog.UpdateHorizontalPosition(this.W);
                dialog.UpdateVerticalPosition(this.H);
                dialog.ApplyLayout();
            }

            this.englishButton.X = (this.W / 2) - 52;
            this.englishButton.Y = this.H - 28;
            this.germanButton.X = (this.W / 2) + 4;
            this.germanButton.Y = this.H - 28;

            this.englishTooltip.ApplyScale();
            this.englishTooltip.ApplyLayout();
            this.germanTooltip.ApplyScale();
            this.germanTooltip.ApplyLayout();

            this.appliedScale = UIStatics.Scale;
            this.IsContentChangedSinceDraw = true;
        }

        private void OnLanguageButtonClick(object sender, MouseEventArgs e)
        {
            if (!(sender is IconButton button) || LanguageManager.CurrentLanguage == button.Tag) return;

            LanguageManager.Load(button.Tag);
            SettingsManager.SetSetting(SettingGroup.Misc, SettingNames.Language, button.Tag);
            SettingsManager.Save();

            this.englishButton.IsSelected = this.englishButton == button;
            this.germanButton.IsSelected = this.germanButton == button;
        }

        protected override void HandleLanguageChange()
        {
            this.englishTooltip.SetTitle(LanguageManager.Get<StringsForFlagTooltips>(StringsForFlagTooltips.English));
            this.germanTooltip.SetTitle(LanguageManager.Get<StringsForFlagTooltips>(StringsForFlagTooltips.German));

            this.fullscreenTexture = UIStatics.Content.Load<Texture2D>($"Textures\\Menu\\{LanguageManager.CurrentLanguage}\\FullscreenHint");
            this.loadingTexture = UIStatics.Content.Load<Texture2D>($"Textures\\Menu\\{LanguageManager.CurrentLanguage}\\Loading");
            this.IsContentChangedSinceDraw = true;

            base.HandleLanguageChange();
        }

        #region Button click handlers

        private void OnNewGameButtonClick(object sender, MouseEventArgs e)
        {
            this.OpenDialog(this.newGameDialog);
        }

        private void OnLoadGameButtonClick(object sender, MouseEventArgs e)
        {
            this.OpenDialog(this.loadGameDialog);
        }

        private void OnOptionsButtonClick(object sender, MouseEventArgs e)
        {
            this.optionsDialog.ResetSettings(this.graphicsManager.IsFullScreen);
            this.OpenDialog(this.optionsDialog);
        }

        private void OnCreditsButtonClick(object sender, MouseEventArgs e)
        {
            this.OpenDialog(this.creditsDialog);
        }

        private void OnExitButtonClick(object sender, MouseEventArgs e)
        {
            this.ExitRequest?.Invoke(this, null);
        }

        private void OnFacebookButtonClick(object sender, MouseEventArgs e)
        {
            Process.Start("https://www.facebook.com/SigmaDraconisGame/");
        }

        private void OnTwitterButtonClick(object sender, MouseEventArgs e)
        {
            Process.Start("https://twitter.com/sigdraconisgame");
        }

        #endregion

        #region Dialog event handlers

        private void OnNewGameDialogNewGame(object sender, NewGameEventArgs e)
        {
            this.isFadingOut = true;
            this.selectedClimateType = e.ClimateType;
            this.newGameDialog.IsVisible = false;
        }

        private void OnNewGameDialogCancel(object sender, EventArgs e)
        {
            KeyboardManager.FocusedElement = this;
            this.newGameDialog.IsVisible = false;
        }

        private void LoadGameDialogStart(object sender, EventArgs e)
        {
            this.loadGameDialog.IsVisible = false;
            if (SaveGameManager.CheckSaveGameVersion(this.loadGameDialog.FileName))
            {
                this.isFadingOut = true;
                this.loadGameFileName = this.loadGameDialog.FileName;
                this.isLoadGameAutosave = this.loadGameDialog.IsAutosave;
            }
            else
            {
                this.errorDialog.SetMessage(LanguageManager.Get<StringsForErrorDialog>(StringsForErrorDialog.FailedToLoadGame), SaveGameManager.LastException);
                this.errorDialog.IsVisible = true;
            }
        }

        private void LoadGameDialogDelete(object sender, EventArgs e)
        {
            this.loadGameDialog.IsVisible = false;
            this.deleteGameDialog.FileName = this.loadGameDialog.FileName;
            this.deleteGameDialog.IsAutosave = this.loadGameDialog.IsAutosave;
            this.OpenDialog(this.deleteGameDialog);
        }

        private void LoadGameDialogCancel(object sender, EventArgs e)
        {
            this.loadGameDialog.IsVisible = false;
            KeyboardManager.FocusedElement = this;
        }

        private void DeleteGameDialogClose(object sender, EventArgs e)
        {
            this.deleteGameDialog.IsVisible = false;
            this.OpenDialog(this.loadGameDialog);
            this.loadGameDialog.UpdateButtons();

        }

        private void CreditsDialogClose(object sender, EventArgs e)
        {
            this.creditsDialog.IsVisible = false;
            KeyboardManager.FocusedElement = this;
        }

        private void ErrorDialogClose(object sender, EventArgs e)
        {
            this.errorDialog.IsVisible = false;
            KeyboardManager.FocusedElement = this;
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.spriteBatch != null) this.spriteBatch.Dispose();
                if (this.mainTexture != null) this.mainTexture.Dispose();
                if (this.titleTexture != null) this.titleTexture.Dispose();
                if (this.loadingTexture != null) this.loadingTexture.Dispose();

                base.Dispose(true);
            }
        }
    }
}
