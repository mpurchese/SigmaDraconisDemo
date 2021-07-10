namespace SigmaDraconis.WorldInterfaces
{
    using Draconis.Shared;

    public interface IRenderOffsettable : IThing
    {
        Vector2i RenderPositionOffset { get; set; }
    }
}
