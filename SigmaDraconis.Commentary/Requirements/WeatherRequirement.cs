namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class WeatherRequirement : RequirementBase
    {
        public string ParamName { get; }
        public int Value { get; }

        public WeatherRequirement(string paramName, Operator op, int value) : base(op)
        {
            this.ParamName = paramName;
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            switch (this.ParamName)
            {
                case "TEMPERATURE": return this.op.Test(CommentaryContext.WorldTemperature, this.Value);
            }

            return false;
        }
    }
}
