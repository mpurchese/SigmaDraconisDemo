namespace SigmaDraconis.WorldInterfaces
{
    using Draconis.Shared;

    public interface IMoveableThing : IThing
    {
        Vector2f PositionOffset { get; }

        bool IsMoving { set; get; }
    }
}
