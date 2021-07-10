namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class LastProjectRequirement : RequirementBase
    {
        public int Value { get; }

        public LastProjectRequirement(Operator op, int value) : base (op)
        {
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            return this.op.Test(colonist.LastLabProjectId, this.Value);
        }
    }
}
