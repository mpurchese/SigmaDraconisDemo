namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class TimeUntilCanWakeRequirement : RequirementBase
    {
        private readonly int value;

        public TimeUntilCanWakeRequirement(Operator op, int value) : base(op)
        {
            this.value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            return this.op.Test(CommentaryContext.TimeUntilCanWake, value);
        }
    }
}
