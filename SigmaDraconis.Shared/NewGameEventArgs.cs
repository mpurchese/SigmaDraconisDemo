namespace SigmaDraconis.Shared
{
    using System;

    public class NewGameEventArgs : EventArgs
    {
        public int MapSize { get; set; }
        public ClimateType ClimateType { get; set; }

        public NewGameEventArgs(int mapSize, ClimateType climateType)
        {
            this.MapSize = mapSize;
            this.ClimateType = climateType;
        }
    }
}
