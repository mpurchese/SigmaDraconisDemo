namespace SigmaDraconis.WorldInterfaces
{
    public interface IPlantWithAnimatedFlower : IPlant
    {
        int? FlowerFrame { get; }
        int? FlowerRenderLayer { get; }
    }
}
