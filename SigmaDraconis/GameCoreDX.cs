namespace SigmaDraconis
{
    using System;
    using System.Timers;

    using Microsoft.Xna.Framework;

    using Engine;
    using Settings;
    using UI;
    using Shared;

    public class GameCoreDX : GameCore
    {
        private int prevFullScreenX = 0;
        private int prevFullScreenY = 0;
        private int prevWindowX = 0;
        private int prevWindowY = 0;

        protected override void OnExitRequest(object sender, EventArgs e)
        {
            // Must call Exit() from platform specific code or Monogame gives an error
            this.Exit();
        }

        protected override void CheckAppRegainedFocus()
        {
            try
            {
                if (!this.IsActive)
                {
                    this.wasFocused = false;
                    if (GameScreen.Instance.IsVisible && !this.gameEngine.IsPaused)
                    {
                        this.gameEngine.IsPaused = true;
                        this.pausedDueToLostFocus = true;
                        Log("Game paused due to lost focus.");
                    }

                    return;
                }
                else if (!this.wasFocused && this.graphicsManager.IsFullScreen)
                {
                    this.wasFocused = true;
                    Log("Game regained focus in full screen mode.");
                    if (this.graphicsManager.PreferredBackBufferWidth != SettingsManager.FullScreenSizeX || this.graphicsManager.PreferredBackBufferHeight != SettingsManager.FullScreenSizeY)
                    {
                        Log($"Setting screen resolution to ({SettingsManager.FullScreenSizeX}, {SettingsManager.FullScreenSizeY})");
                        this.graphicsManager.PreferredBackBufferWidth = SettingsManager.FullScreenSizeX;
                        this.graphicsManager.PreferredBackBufferHeight = SettingsManager.FullScreenSizeY;
                        this.graphicsManager.ApplyChanges();
                    }

                    Log($"IsFullScreen = {graphicsManager.IsFullScreen}.  Performing the double-toggle...");
                    this.graphicsManager.ToggleFullScreen();
                    Log($"IsFullScreen = {graphicsManager.IsFullScreen}.  Setting window position from ({this.Window.Position.X}, {this.Window.Position.Y}) to ({this.prevFullScreenX}, {this.prevFullScreenY})...");
                    this.Window.Position = new Point(this.prevFullScreenX, this.prevFullScreenY);  // Prevents restore to wrong screen on alt-tab
                    Log("Second toggle...");
                    this.graphicsManager.ToggleFullScreen();
                    Log($"Full-screen restore complete.  IsFullScreen = {graphicsManager.IsFullScreen}, display coords ({this.Window.Position.X}, {this.Window.Position.Y}, {this.GraphicsDevice.DisplayMode.Width}, {this.GraphicsDevice.DisplayMode.Height})");
                }

                if (this.IsActive && this.pausedDueToLostFocus)
                {
                    this.pausedDueToLostFocus = false;
                    if (GameScreen.Instance.IsVisible)
                    {
                        Log("Un-pausing game.");
                        this.gameEngine.IsPaused = false;
                    }
                }

                if (this.Window.Position.X <= -16000 || this.Window.Position.Y <= -16000)
                {
                    var newPos = this.graphicsManager.IsFullScreen ? new Point(this.prevFullScreenX, this.prevFullScreenY) : new Point(this.prevWindowX, this.prevWindowY);
                    Log($"Game was minimised to coords ({this.Window.Position.X}, {this.Window.Position.Y}).  Moving to ({newPos.X}, {newPos.Y}).  IsFullScreen = {graphicsManager.IsFullScreen}.");
                    this.Window.Position = newPos;
                    this.graphicsManager.ApplyChanges();
                    Log("Change applied.");
                }
                else if (this.graphicsManager.IsFullScreen)
                {
                    this.prevFullScreenX = this.Window.Position.X;
                    this.prevFullScreenY = this.Window.Position.Y;
                }
                else if ((this.Window.Position.X != this.prevWindowX || this.Window.Position.Y != this.prevWindowY)
                    && (this.Window.Position.X > 0 || this.Window.Position.X < -100) && (this.Window.Position.Y > 0 || this.Window.Position.Y < -100))
                {
                    // Hacky If clause is to stop screen getting put in a position with offset just off the top-left, which it likes to do when switching from full screen.
                    this.prevWindowX = this.Window.Position.X;
                    this.prevWindowY = this.Window.Position.Y;
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR: {ex}");
            }
        }

        protected override void OnDisplaySettingsChangeRequest(object sender, DisplaySettingsChangeRequestEventArgs e)
        {
            base.OnDisplaySettingsChangeRequest(sender, e);

            if (e.ToggleFullScreen && !this.graphicsManager.IsFullScreen)
            {
                var windowX = this.prevWindowX;
                var windowY = this.prevWindowY;

                if (windowX > -100 && windowX <= 0)
                {
                    if (e.Width < this.GraphicsDevice.DisplayMode.Width - 10)
                    {
                        windowX = (this.GraphicsDevice.DisplayMode.Width - e.Width) / 3;
                    }
                }

                if (windowY > -100 && windowY <= 0)
                {
                    if (e.Height < this.GraphicsDevice.DisplayMode.Height - 80)
                    {
                        windowY = (this.GraphicsDevice.DisplayMode.Height - e.Height) / 3;
                    }
                }

                Log($"Moving window from ({this.Window.Position.X}, {this.Window.Position.Y}) to ({windowX}, {windowY}).");
                this.Window.Position = new Point(windowX, windowY);
                this.graphicsManager.ApplyChanges();
                Log("Changed applied.");
            }
            else if (e.ToggleFullScreen)
            {
                this.prevWindowX = this.Window.Position.X;
                this.prevWindowY = this.Window.Position.Y;
            }
        }

        private static void Log(string message)
        {
            Logger.Instance.Log("GameCoreDX", message);
        }
    }
}
