namespace SigmaDraconis.CheckList.Requirements
{
    using Context;
    using Operators;

    internal class ClimateRequirement : RequirementBase
    {
        public string ParamName { get; }
        public int Value { get; }

        public ClimateRequirement(string paramName, Operator op, int value) : base (op)
        {
            this.ParamName = paramName;
            this.Value = value;
        }

        protected override bool TestThis()
        {
            switch (this.ParamName)
            {
                case "SEVERITY": return this.op.Test((int)CheckListContext.ClimateType, this.Value);
            }

            return false;
        }
    }
}
