namespace SigmaDraconis.IO
{
    using System;
    using System.Collections.Generic;
    using Draconis.Shared;
    using Shared;

    public class GameLoadedEventArgs : EventArgs
    {
        public GameVersion Version { get; private set; }
        public Vector2i ScrollPosition { get; private set; }
        public int ZoomLevel { get; private set; }
        public Dictionary<string, string> AdditionalProperties { get; private set; }

        public GameLoadedEventArgs(GameVersion version, Vector2i scrollPosition, int zoomLevel, Dictionary<string, string> additionalProperties)
        {
            this.Version = version;
            this.ScrollPosition = scrollPosition;
            this.ZoomLevel = zoomLevel;
            this.AdditionalProperties = additionalProperties;
        }
    }
}
