namespace SigmaDraconis.WorldInterfaces
{
    public interface IConduitMinor : IBuildableThing
    {
        int ConnectedNodeId { get; set; }
    }
}