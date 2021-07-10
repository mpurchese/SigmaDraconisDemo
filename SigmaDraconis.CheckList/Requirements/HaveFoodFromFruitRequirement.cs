namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class HaveFoodFromFruitRequirement : RequirementBase
    {
        public bool Value { get; }

        public HaveFoodFromFruitRequirement(Operator op, bool value) : base (op)
        {
            this.Value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.HaveFoodFromFruit, this.Value);
        }
    }
}
