namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IResourceStack : IThing, IColonistInteractive
    {
        ItemType ItemType { get; }
        int ItemCount { get; set; }
        bool IsReady { get; set; }
        WorkPriority HaulPriority { get; set; }
        int TargetItemCount { get; set; }
        int MaxItems { get; }
        void AddItem();
        void TakeItem();
    }
}
