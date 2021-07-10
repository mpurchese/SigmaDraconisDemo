namespace SigmaDraconis.WorldInterfaces
{
    public interface ISleepPod : IColonistInteractive, IBuildableThing, IAnimatedThing, IEnergyConsumer
    {
        int? OwnerID { get; set; }
        int OwnerChangeTimer { get; set; }
        int TargetTemp { get; set; }
        float Temperature { get; set; }
        bool IsHeaterSwitchedOn { get; set; }
        int? GetAssignedColonistId();
    }
}
