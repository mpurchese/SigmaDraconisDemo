namespace SigmaDraconis.WorldGenerator
{
    using Shared;

    public class RockTemplate : IThingTemplate
    {
        public ThingType ThingType { get; private set; }
        public ItemType ResourceType { get; private set; }
        public SmallTileTemplate MainTile { get; private set; }
        public int MainTileIndex => this.MainTile.Index;

        public RockTemplate(SmallTileTemplate tile, ThingType thingType, ItemType resourceType)
        {
            this.MainTile = tile;
            this.ThingType = thingType;
            this.ResourceType = resourceType;
        }
    }
}
