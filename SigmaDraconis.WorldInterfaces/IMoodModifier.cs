namespace SigmaDraconis.WorldInterfaces
{
    public interface IMoodModifer
    {
        string Description { get; }
        int Value { get; }
        void Update(IColonist colonist);
    }
}
