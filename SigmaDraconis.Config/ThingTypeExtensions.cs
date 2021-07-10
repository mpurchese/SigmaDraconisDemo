namespace SigmaDraconis.Config
{
    using Draconis.Shared;
    using Shared;

    public static class ThingTypeExtensions
    {
        /// <summary>
        /// Returns true if type is a foundation that can be built on
        /// </summary>
        /// <param name="thingType"></param>
        /// <returns></returns>
        public static bool IsFoundation(this ThingType thingType)
        {
            return thingType == ThingType.FoundationMetal || thingType == ThingType.FoundationStone;
        }

        /// <summary>
        /// Returns true if type is a conduit or conduit node
        /// </summary>
        /// <param name="thingType"></param>
        /// <returns></returns>
        public static bool IsConduit(this ThingType thingType)
        {
            return thingType == ThingType.ConduitMinor || thingType == ThingType.ConduitMajor || thingType == ThingType.ConduitNode;
        }

        /// <summary>
        /// Returns true if type is a foundation or something at floor level, e.g. algae pool
        /// </summary>
        /// <param name="thingType"></param>
        /// <returns></returns>
        public static bool IsFoundationLayer(this ThingType thingType)
        {
            return thingType.In(ThingTypeManager.BuildableThingTypesByLayer[BuildingLayer.Floor]);
        }

        public static ThingTypeDefinition GetDefinition(this ThingType thingType)
        {
            return ThingTypeManager.GetDefinition(thingType);
        }
    }
}
