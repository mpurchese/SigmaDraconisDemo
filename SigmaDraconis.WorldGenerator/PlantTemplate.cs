namespace SigmaDraconis.WorldGenerator
{
    using Shared;

    public class PlantTemplate : IThingTemplate
    {
        public ThingType ThingType { get; private set; }
        public SmallTileTemplate MainTile { get; private set; }
        public int MainTileIndex => this.MainTile.Index;

        public PlantTemplate(SmallTileTemplate tile, ThingType thingType)
        {
            this.MainTile = tile;
            this.ThingType = thingType;
        }
    }
}
