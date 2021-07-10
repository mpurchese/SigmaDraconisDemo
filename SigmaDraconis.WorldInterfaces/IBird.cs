namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IBird : IAnimal, IMoveableThing
    {
        bool IsFadingIn { get; set; }
        bool IsFadingOut { get; set; }

        float Height { get; set; }
        int Speed { get; set; }
        float Angle { get; set; }
        int Turning { get; set; }
    }
}
