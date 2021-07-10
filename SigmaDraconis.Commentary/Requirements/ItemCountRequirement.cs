namespace SigmaDraconis.Commentary.Requirements
{
    using System;

    using Shared;

    using Context;
    using Operators;

    internal class ItemCountRequirement : RequirementBase
    {
        public ItemType ItemType { get; }
        public int Value { get; }

        public ItemCountRequirement(string type, Operator op, int value) : base (op)
        {
            this.ItemType = (ItemType)Enum.Parse(typeof(ItemType), type);
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            return this.op.Test(CommentaryContext.ItemTypeCounts[this.ItemType], this.Value);
        }
    }
}
