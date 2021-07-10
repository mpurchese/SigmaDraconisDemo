namespace SigmaDraconis.Commentary.Requirements
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

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            if (isCurrentComment) return true;

            var hour = (CommentaryContext.FrameNumber / 3600) + 1;
            return this.op.Test(hour, value);
        }
    }
}
