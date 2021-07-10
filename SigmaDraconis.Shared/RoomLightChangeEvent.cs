namespace SigmaDraconis.Shared
{
    public class RoomLightChangeEvent
    {
        public RoomLightChangeEvent(int terrainRow, ThingType thingType)
        {
            this.TerrainRow = terrainRow;
            this.ThingType = thingType;
        }

        public int TerrainRow { get; set; }
        public ThingType ThingType { get; set; }
    }
}
