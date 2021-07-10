namespace SigmaDraconis.WorldInterfaces
{
    public interface IThingWithRenderLayer : IThing
    {
        int RenderLayer { get; set; }
    }
}
