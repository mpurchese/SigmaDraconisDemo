namespace SigmaDraconis.World.Buildings
{
    using ProtoBuf;
    using ResourceStacks;
    using Shared;
    using System.Linq;
    using WorldInterfaces;

    [ProtoContract]
    public class StackingArea : Thing, IStackingArea
    {
        [ProtoMember(1)]
        public ItemType ItemType { get; set; }

        [ProtoMember(2)]
        public StackingAreaMode Mode { get; set; }

        [ProtoMember(3)]
        public int TargetStackSize { get; set; }

        [ProtoMember(4)]
        public WorkPriority WorkPriority { get; set; }

        public override bool CanWalk => true;

        protected StackingArea() : base(ThingType.StackingArea)
        {
        }
       
        public StackingArea(ISmallTile mainTile, ItemType itemType) : base(ThingType.StackingArea, mainTile, 1)
        {
            this.ItemType = itemType;
            this.Mode = Constants.DefaultStackingAreaMode;
            this.TargetStackSize = Constants.DefaultStackingAreaTargetCount;
        }

        public override void AfterAddedToWorld()
        {
            this.UpdateStack();
            base.AfterAddedToWorld();
        }

        public void UpdateStack()
        {
            var stack = this.MainTile.ThingsPrimary.OfType<IResourceStack>().FirstOrDefault();
            if (stack != null)
            {
                if (stack.ItemType != this.ItemType)
                {
                    if (stack.ItemCount == 0)
                    {
                        World.RemoveThing(stack);
                        if (this.Mode != StackingAreaMode.RemoveStack && (this.Mode != StackingAreaMode.TargetStackSize || this.TargetStackSize > 0))
                        {
                            var newStack = new ResourceStack(Constants.ResourceStackTypes[this.ItemType], this.MainTile, 0) { HaulPriority = this.WorkPriority };
                            World.AddThing(newStack);
                            newStack.IsReady = true;
                            stack = newStack;
                        }
                    }
                }
            }
            else if (this.Mode != StackingAreaMode.RemoveStack && (this.Mode != StackingAreaMode.TargetStackSize || this.TargetStackSize > 0))
            {
                stack = new ResourceStack(Constants.ResourceStackTypes[this.ItemType], this.MainTile, 0) { HaulPriority = this.WorkPriority };
                World.AddThing(stack);
                stack.IsReady = true;
            }

            if (stack != null)
            {
                stack.TargetItemCount = this.Mode == StackingAreaMode.TargetStackSize ? this.TargetStackSize : 0;
                stack.HaulPriority = this.WorkPriority;
            }
        }

        public override string ToString()
        {
            return $"Stacking Area {this.Id}";
        }
    }
}
