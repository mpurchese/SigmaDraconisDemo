namespace SigmaDraconis.CheckList.Requirements
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

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.ItemTypeCounts[this.ItemType], this.Value);
        }
    }
}
