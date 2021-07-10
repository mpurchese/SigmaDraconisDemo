namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class ColonistLocationRequirement : RequirementBase
    {
        public string PropertyName { get; }
        public bool Value { get; }

        public ColonistLocationRequirement(string propertyName, Operator op, bool value) : base (op)
        {
            this.PropertyName = propertyName;
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            switch (this.PropertyName)
            {
                case "ByCoast": return this.op.Test(colonist.IsByCoast, this.Value);
                case "InSleepPod": return this.op.Test(colonist.IsInSleepPod, this.Value);
                case "Indoors": return this.op.Test(colonist.IsIndoors, this.Value);
            }

            return false;
        }
    }
}
