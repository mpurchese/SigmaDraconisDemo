namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IThingWithShadow : IThing
    {
        ShadowModel ShadowModel { get; }
    }
}
