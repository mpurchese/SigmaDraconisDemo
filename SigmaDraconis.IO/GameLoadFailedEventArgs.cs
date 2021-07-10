namespace SigmaDraconis.IO
{
    using System;

    public class GameLoadFailedEventArgs : EventArgs
    {
        public string Reason { get; set; }

        public GameLoadFailedEventArgs(string reason)
        {
            this.Reason = reason;
        }
    }
}
