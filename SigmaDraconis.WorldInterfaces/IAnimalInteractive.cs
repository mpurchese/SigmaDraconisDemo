namespace SigmaDraconis.WorldInterfaces
{
    public interface IAnimalInteractive : IThing
    {
        int ReservedByAnimalID { get; }
        void Reserve(int animalID);
    }
}
