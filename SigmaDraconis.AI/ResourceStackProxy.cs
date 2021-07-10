namespace SigmaDraconis.AI
{
    using System.Linq;
    using Shared;
    using WorldInterfaces;

    internal class ResourceStackProxy
    {
        public ItemType Type { get; set; }
        public int Count { get; set; }
        public WorkPriority Priority { get; set; }
        public StackingAreaMode Mode { get; set; }
        public int TargetCount { get; set; }
        public int PredictedItemCount { get; set; }
        public IResourceStack Stack { get; }

        public ResourceStackProxy(IResourceStack stack)
        {
            this.Type = stack.ItemType;
            this.Count = stack.ItemCount;
            this.Priority = stack.HaulPriority;
            this.TargetCount = stack.TargetItemCount;
            this.PredictedItemCount = stack.ItemCount;

            var area = stack.MainTile.ThingsPrimary.OfType<IStackingArea>().FirstOrDefault(a => a.ItemType == stack.ItemType || stack.ItemCount == 0);
            this.Mode = area?.Mode ?? StackingAreaMode.RemoveStack;

            this.Stack = stack;
        }

        public bool Update(IResourceStack stack)
        {
            var area = stack.MainTile.ThingsPrimary.OfType<IStackingArea>().FirstOrDefault(a => a.ItemType == stack.ItemType || stack.ItemCount == 0);
            var mode = area?.Mode ?? StackingAreaMode.RemoveStack;

            this.PredictedItemCount = stack.ItemCount;

            if (stack.ItemType == this.Type && stack.ItemCount == this.Count && stack.HaulPriority == this.Priority && stack.TargetItemCount == this.TargetCount && mode == this.Mode)
            {
                return false;
            }

            this.Type = stack.ItemType;
            this.Count = stack.ItemCount;
            this.Priority = stack.HaulPriority;
            this.TargetCount = stack.TargetItemCount;
            this.Mode = mode;

            return true;
        }
    }
}
