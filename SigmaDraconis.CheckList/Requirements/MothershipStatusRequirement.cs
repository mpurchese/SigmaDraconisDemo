namespace SigmaDraconis.CheckList.Requirements
{
    using System;
    using Shared;
    using Context;
    using Operators;

    internal class MothershipStatusRequirement : RequirementBase
    {
        public MothershipStatus Value { get; }

        public MothershipStatusRequirement(Operator op, string value) : base (op)
        {
            this.Value = (MothershipStatus)Enum.Parse(typeof(MothershipStatus), value);
        }

        protected override bool TestThis()
        {
            return this.op.Test((int)CheckListContext.MothershipStatus, (int)this.Value);
        }
    }
}
