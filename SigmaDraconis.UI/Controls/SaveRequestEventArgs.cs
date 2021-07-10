namespace SigmaDraconis.UI
{
    using System;

    public class SaveRequestEventArgs : EventArgs
    {
        public string FileName { get; set; }

        public SaveRequestEventArgs(string fileName)
        {
            this.FileName = fileName;
        }
    }
}
