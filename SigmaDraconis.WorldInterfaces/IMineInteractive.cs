namespace SigmaDraconis.WorldInterfaces
{
    public interface IMineInteractive : IMine, IColonistJobProvider, IColonistInteractive
    {
        void StillWorking();
        int AssignedColonistDistance { get; set; }
    }
}
