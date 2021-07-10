namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class ArrivedColonistCountRequirement : RequirementBase
    {
        private readonly int value;

        public ArrivedColonistCountRequirement(Operator op, int value) : base(op)
        {
            this.value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.ArrivedColonistCount, value);
        }
    }
}
