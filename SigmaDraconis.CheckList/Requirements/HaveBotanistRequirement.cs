namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class HaveBotanistRequirement : RequirementBase
    {
        public bool Value { get; }

        public HaveBotanistRequirement(Operator op, bool value) : base (op)
        {
            this.Value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.HaveBotanist, this.Value);
        }
    }
}
