namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class ProjectCompleteRequirement : RequirementBase
    {
        public int ItemId { get; }
        public bool Value { get; }

        public ProjectCompleteRequirement(int itemId, Operator op, bool value) : base (op)
        {
            this.ItemId = itemId;
            this.Value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.CompleteProjectIds.Contains(this.ItemId), this.Value);
        }
    }
}
