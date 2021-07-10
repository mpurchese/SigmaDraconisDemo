namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class HourRequirement : RequirementBase
    {
        private readonly int value;

        public HourRequirement(Operator op, int value) : base(op)
        {
            this.value = value;
        }

        protected override bool TestThis()
        {
            var hour = (CheckListContext.FrameNumber / 3600) + 1;
            return this.op.Test(hour, value);
        }
    }
}
