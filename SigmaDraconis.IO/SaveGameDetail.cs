namespace SigmaDraconis.IO
{
    using Shared;
    using System;

    public class SaveGameDetail
    {
        public string FileName { get; set; }
        public DateTime FileDate { get; set; }
        public GameVersion GameVersion { get; set; }
        public WorldTime WorldTime { get; set; }
    }
}
