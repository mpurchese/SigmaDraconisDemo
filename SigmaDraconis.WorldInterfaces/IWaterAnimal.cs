namespace SigmaDraconis.WorldInterfaces
{
    public interface IWaterAnimal : IAnimal, IMoveableThing
    {
        bool IsFadingIn { get; set; }
        bool IsFadingOut { get; set; }

        int Speed { get; set; }
        float Angle { get; set; }
        long CreatedFrame { get; set; }
    }
}
