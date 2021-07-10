namespace SigmaDraconis.CheckList.Requirements
{
    using System;
    using Shared;
    using Context;
    using Operators;

    internal class StorageCountRequirement : RequirementBase
    {
        public ThingType StorageType { get; }
        public int Value { get; }

        public StorageCountRequirement(string type, Operator op, int value) : base (op)
        {
            this.StorageType = (ThingType)Enum.Parse(typeof(ThingType), type);
            this.Value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.StorageTypeCounts[this.StorageType], this.Value);
        }
    }
}
