namespace SigmaDraconis.Commentary.Requirements
{
    using System.Linq;
    using Context;
    using Operators;
    using Shared;

    internal class ActivityRequirement : RequirementBase
    {
        public string ParamName { get; }
        public bool Value { get; }

        public ActivityRequirement(string paramName, Operator op, bool value) : base (op)
        {
            this.ParamName = paramName;
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            switch (this.ParamName)
            {
                case "ANYWORK": return this.op.Test(colonist.IsWorking, this.Value);
                case "IDLE": return this.op.Test(colonist.IsIdle, this.Value);
                case "HAULPICKUP": return this.op.Test(colonist.ActivityType == ColonistActivityType.HaulPickup, this.Value);
                case "SOCIAL": return this.op.Test(colonist.ActivityType == ColonistActivityType.Relax && colonist.TimeSinceSocialByColonist.Any(kv => kv.Value == 0), this.Value);
            }

            return false;
        }
    }
}
