namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class BotanistWokenRequirement : RequirementBase
    {
        public bool Value { get; }

        public BotanistWokenRequirement(Operator op, bool value) : base (op)
        {
            this.Value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.IsBotanistWoken, this.Value);
        }
    }
}
