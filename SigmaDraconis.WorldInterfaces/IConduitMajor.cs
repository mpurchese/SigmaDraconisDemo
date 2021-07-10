namespace SigmaDraconis.WorldInterfaces
{
    public interface IConduitMajor : IBuildableThing
    {
        int Node1 { get; set; }
        int? Node2 { get; set; }
    }
}