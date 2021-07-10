namespace SigmaDraconis.World.ResourceNetworks
{
    using WorldInterfaces;

    internal class SmoothedWaterConsumer
    {
        public IBuildableThing Building { get; set; }
        public int RemainingAmount { get; set; }
        public int AmountPerFrame { get; set; }
    }
}
