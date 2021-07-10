namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class HaveGeologistRequirement : RequirementBase
    {
        public bool Value { get; }

        public HaveGeologistRequirement(Operator op, bool value) : base (op)
        {
            this.Value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.HaveGeologist, this.Value);
        }
    }
}
