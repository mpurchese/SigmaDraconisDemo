namespace SigmaDraconis.WorldInterfaces
{
    public interface ITree : IThingWithShadow, IThingHidesTiles
    {
        float Height { get; set; }
        float TreeTopWarpPhase { get; }
    }
}
