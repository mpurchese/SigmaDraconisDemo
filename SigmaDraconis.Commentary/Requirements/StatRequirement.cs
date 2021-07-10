namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class StatRequirement : RequirementBase
    {
        public string ParamName { get; }
        public long Value { get; }

        public StatRequirement(string paramName, Operator op, long value) : base (op)
        {
            this.ParamName = paramName;
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            return this.op.Test(CommentaryContext.Stats[this.ParamName], this.Value);
        }
    }
}
