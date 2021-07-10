namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IFlyingInsect : IAnimal, IMoveableThing
    {
        bool IsFadingIn { get; set; }
        bool IsFadingOut { get; set; }

        int Height { get; set; }
        int Speed { get; set; }
        float Angle { get; set; }
    }
}
