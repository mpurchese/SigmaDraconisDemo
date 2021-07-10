namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IDoor : IAnimatedThing, IWall
    {
        bool IsOpen { get; }
        DoorState State { get; }
        void SetState(DoorState newState);
        void Open();
    }
}