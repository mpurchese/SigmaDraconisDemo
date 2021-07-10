namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class ItemCompleteRequirement : RequirementBase
    {
        public int ItemId { get; }
        public bool Value { get; }

        public ItemCompleteRequirement(int itemId, Operator op, bool value) : base (op)
        {
            this.ItemId = itemId;
            this.Value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.CompleteItemIds.Contains(this.ItemId), this.Value);
        }
    }
}
