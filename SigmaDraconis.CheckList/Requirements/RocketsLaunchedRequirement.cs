namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class RocketsLaunchedRequirement : RequirementBase
    {
        private readonly int value;

        public RocketsLaunchedRequirement(Operator op, int value) : base(op)
        {
            this.value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.RocketsLaunched, value);
        }
    }
}
