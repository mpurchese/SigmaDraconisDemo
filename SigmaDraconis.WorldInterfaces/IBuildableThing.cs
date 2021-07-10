namespace SigmaDraconis.WorldInterfaces
{
    public interface IBuildableThing : IRecyclableThing, IAnimatedThing
    {
        int ConstructionProgress { get; set; }
        bool IsReady { get; set; }
        bool IsConstructionPaused { get; }
        void AfterConstructionComplete();
        int GetAnimationFrameForDeconstructOverlay();
        bool IncrementConstructionProgress(double amountPercent);
        void Recycle();  // All buildable things can also be recycled
        void CancelRecycle();
    }
}
