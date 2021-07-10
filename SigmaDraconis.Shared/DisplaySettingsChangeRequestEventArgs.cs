namespace SigmaDraconis.Shared
{
    using System;

    public class DisplaySettingsChangeRequestEventArgs : EventArgs
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool ToggleFullScreen { get; set; }

        public DisplaySettingsChangeRequestEventArgs(int width, int height, bool toggleFullScreen = false)
        {
            this.Width = width;
            this.Height = height;
            this.ToggleFullScreen = toggleFullScreen;
        }
    }
}
