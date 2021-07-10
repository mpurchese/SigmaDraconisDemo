namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IBlueprint : IRotatableThing, IAnimatedThing, IColonistInteractive
    {
        WorkPriority BuildPriority { get; set; }
    }
}
