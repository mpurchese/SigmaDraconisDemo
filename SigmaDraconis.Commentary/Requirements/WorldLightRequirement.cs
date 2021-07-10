namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class WorldLightRequirement : RequirementBase
    {
        public string ParamName { get; }
        public float Value { get; }

        public WorldLightRequirement(string paramName, Operator op, float value) : base(op)
        {
            this.ParamName = paramName;
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            switch (this.ParamName)
            {
                case "BRIGHTNESS": return this.op.Test(CommentaryContext.WorldLightBrightness, this.Value);
            }

            return false;
        }
    }
}
