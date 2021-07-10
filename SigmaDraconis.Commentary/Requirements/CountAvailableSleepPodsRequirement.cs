namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class CountAvailableSleepPodsRequirement : RequirementBase
    {
        public int Value { get; }

        public CountAvailableSleepPodsRequirement(Operator op, int value) : base (op)
        {
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            if (!CommentaryContext.AvailableSleepPodsByColonistId.ContainsKey(colonist.Id)) return false;
            return this.op.Test(CommentaryContext.AvailableSleepPodsByColonistId[colonist.Id], this.Value);
        }
    }
}
