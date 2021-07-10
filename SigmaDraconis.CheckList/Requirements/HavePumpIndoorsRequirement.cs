namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class HavePumpIndoorsRequirement : RequirementBase
    {
        public bool Value { get; }

        public HavePumpIndoorsRequirement(Operator op, bool value) : base (op)
        {
            this.Value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.HavePumpIndoors, this.Value);
        }
    }
}
