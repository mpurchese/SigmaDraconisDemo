namespace SigmaDraconis.Renderers
{
    using System;
    using Microsoft.Xna.Framework.Content;
    using Microsoft.Xna.Framework.Graphics;

    using Draconis.Shared;

    using Shared;
    using UI;
    using System.IO;

    public class GameScreenRenderer : IRenderer, IDisposable
    {
        private GraphicsDevice graphics;

        private readonly WorldRenderer worldRenderer;
        private readonly GameScreen gameScreen;
        private bool isLoading = true;
        private bool isReadyToDraw;
        private int screenshotTextTimout;

        public static GameScreenRenderer Instance { get; private set; }

        public static bool SkipNextFrame;

        public bool IsLoading
        {
            get { return this.isLoading || !this.isReadyToDraw; }
            set
            {
                this.isLoading = value;
                if (value) this.isReadyToDraw = false; 
            }
        }

        public GameScreenRenderer(GameScreen screen)
        {
            if (Instance == null)
            {
                Instance = this;

                this.worldRenderer = new WorldRenderer();

                this.gameScreen = screen;
            }
            else
            {
                throw new ApplicationException("GameScreenRenderer already created");
            }
        }

        public void LoadContent(GraphicsDevice graphicsDevice, ContentManager contentManager)
        {
            this.graphics = graphicsDevice;
            this.worldRenderer.LoadContent(graphicsDevice, contentManager);
        }

        public void InvalidateBuffers()
        {
            this.worldRenderer.InvalidateBuffers();
            this.worldRenderer.InvalidateShadows();
        }

        public void ClearShadows()
        {
            this.worldRenderer.ClearShadows();
        }

        public void Update(Vector2f scrollPos, float zoom, bool isPaused)
        {
            if (!this.isLoading)
            {
                this.worldRenderer.Update(scrollPos, zoom, isPaused);
                this.isReadyToDraw = true;
            }
        }

        public void Draw(Vector2f scrollPos, float zoom, bool screenshot = false, bool screenshotNoUI = false)
        {
            if (this.isReadyToDraw && !this.isLoading)
            {
                if (screenshot)
                {
                    var filePath = GetScreenshotPath();

                    graphics.Clear(Microsoft.Xna.Framework.Color.Black);
                    this.worldRenderer.Draw(scrollPos, zoom, null);
                    this.gameScreen.SetScreenshotText(filePath, screenshotNoUI);
                    this.gameScreen.Draw();
                    this.gameScreen.ClearScreenshotText();

                    var pp = this.graphics.PresentationParameters;
                    using (var renderTarget = new RenderTarget2D(this.graphics, pp.BackBufferWidth, pp.BackBufferHeight, false, graphics.DisplayMode.Format, DepthFormat.None, 0, RenderTargetUsage.DiscardContents))
                    {
                        this.graphics.SetRenderTarget(renderTarget);
                        graphics.Clear(Microsoft.Xna.Framework.Color.Black);
                        this.worldRenderer.Draw(scrollPos, zoom, renderTarget);
                        if (!screenshotNoUI) this.gameScreen.Draw();
                        SaveScreenshot(renderTarget, filePath);
                        this.gameScreen.SetScreenshotText(filePath, screenshotNoUI);
                        this.screenshotTextTimout = 60;
                        this.graphics.SetRenderTarget(null);
                        this.graphics.Present();
                        
                    }
                }
                else
                {
                    graphics.Clear(Microsoft.Xna.Framework.Color.Black);
                    this.worldRenderer.Draw(scrollPos, zoom, null);
                    this.gameScreen.Draw();
                }

                if (this.screenshotTextTimout > 0)
                {
                    this.screenshotTextTimout--;
                    if (this.screenshotTextTimout == 0) this.gameScreen.ClearScreenshotText();
                }
            }
        }

        private static void SaveScreenshot(Texture2D texture, string fiePath)
        {
            using (var stream = File.Create(fiePath))
            {
                texture.SaveAsPng(stream, texture.Width, texture.Height);
            }
        }

        private static string GetScreenshotPath()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SigmaDraconis");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = Path.Combine(dir, "Screenshots");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var files = Directory.GetFiles(dir);
            var fileNo = 1;
            foreach (var file in files)
            {
                if (file.Length > 7 && file.EndsWith(".png"))
                {
                    if (int.TryParse(file.Substring(file.Length - 7, 3), out int number) && number >= fileNo)
                    {
                        fileNo = number + 1;
                    }
                }
            }

            var fileName = $"Screenshot_{fileNo:D3}.png";
            var filePath = Path.Combine(dir, fileName);

            return filePath;
        }

        public OverlayType OverlayType
        {
            get
            {
                return this.worldRenderer.OverlayType;
            }
            set
            {
                this.worldRenderer.OverlayType = value;
            }
        }

        public bool IsRoofVisible
        {
            get
            {
                return this.worldRenderer.IsRoofVisible;
            }
            set
            {
                this.worldRenderer.IsRoofVisible = value;
            }
        }

        public bool ToggleZoneOverlay()
        {
            return this.worldRenderer.ToggleZoneOverlay();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.worldRenderer != null) this.worldRenderer.Dispose();
            }
        }
    }
}
