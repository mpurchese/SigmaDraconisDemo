namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class HaveFoodFromCropsRequirement : RequirementBase
    {
        public bool Value { get; }

        public HaveFoodFromCropsRequirement(Operator op, bool value) : base (op)
        {
            this.Value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.HaveFoodFromCrops, this.Value);
        }
    }
}
