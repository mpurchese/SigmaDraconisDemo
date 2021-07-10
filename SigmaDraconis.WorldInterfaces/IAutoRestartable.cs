namespace SigmaDraconis.WorldInterfaces
{
    public interface IAutoRestartable
    {
        bool IsAutoRestartEnabled { get; }
        void ToggleAutoRestart();
    }
}