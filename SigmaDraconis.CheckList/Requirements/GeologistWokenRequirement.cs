namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class GeologistWokenRequirement : RequirementBase
    {
        public bool Value { get; }

        public GeologistWokenRequirement(Operator op, bool value) : base (op)
        {
            this.Value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.IsGeologistWoken, this.Value);
        }
    }
}
