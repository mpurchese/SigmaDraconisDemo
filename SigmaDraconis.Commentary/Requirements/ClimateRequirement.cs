namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class ClimateRequirement : RequirementBase
    {
        public string ParamName { get; }
        public int Value { get; }

        public ClimateRequirement(string paramName, Operator op, int value) : base(op)
        {
            this.ParamName = paramName;
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            switch (this.ParamName)
            {
                case "SEVERITY": return this.op.Test((int)CommentaryContext.ClimateType, this.Value);
            }

            return false;
        }
    }
}
