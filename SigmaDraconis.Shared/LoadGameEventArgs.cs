namespace SigmaDraconis.Shared
{
    using System;

    public class LoadGameEventArgs : EventArgs
    {
        public string FileName { get; set; }
        public bool IsAutosave { get; set; }

        public LoadGameEventArgs(string fileName, bool isAutosave)
        {
            this.FileName = fileName;
            this.IsAutosave = isAutosave;
        }
    }
}
