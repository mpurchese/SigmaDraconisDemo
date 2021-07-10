namespace SigmaDraconis.WorldInterfaces
{
    public interface ILandingPod : IColonistInteractive, IRecyclableThing, IAnimatedThing
    {
        bool IsEmpty { get; }
        float Altitude { get; }
        float VerticalSpeed { get; }
    }
}
