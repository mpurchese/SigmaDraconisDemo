namespace SigmaDraconis.CheckList.Requirements
{
    using System;
    using Shared;
    using Context;
    using Operators;

    internal class AllColonistsHaveOwnSleepPodRequirement : RequirementBase
    {
        public bool Value { get; }

        public AllColonistsHaveOwnSleepPodRequirement(Operator op, bool value) : base(op)
        {
            this.Value = value;
        }

        protected override bool TestThis()
        {
            return this.op.Test(CheckListContext.AllColonistsHaveOwnSleepPod, this.Value);
        }
    }
}
