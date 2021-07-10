namespace SigmaDraconis.Shared
{
    public class PropertyChangeEvent
    {
        public PropertyChangeEvent(int thingId, string propertyName, object previousValue, object newValue, int? terrainRow = null, ThingType? thingType = null)
        {
            this.PropertyName = propertyName;
            this.PreviousValue = previousValue;
            this.NewValue = newValue;
            this.TerrainRow = terrainRow;
            this.ThingType = thingType;
            this.ThingId = thingId;
        }

        public int ThingId { get; set; }
        public string PropertyName { get; set; }
        public object PreviousValue { get; set; }
        public object NewValue { get; set; }
        public int? TerrainRow { get; set; }
        public ThingType? ThingType { get; set; }
    }
}
