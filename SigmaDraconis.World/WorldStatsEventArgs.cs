using System;

namespace SigmaDraconis.World
{
    public class WorldStatsEventArgs : EventArgs
    {
        public string StatId { get; }

        public WorldStatsEventArgs(string statId)
        {
            this.StatId = statId;
        }
    }
}
