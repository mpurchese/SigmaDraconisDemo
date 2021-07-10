namespace SigmaDraconis.Commentary.Requirements
{
    using System;
    using System.Linq;

    using Shared;

    using Context;
    using Operators;

    internal class OtherColonistRequirement : RequirementBase
    {
        public string ParamName { get; }
        public int Value { get; }

        public OtherColonistRequirement(string paramName, Operator op, int value) : base (op)
        {
            this.ParamName = paramName;
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            if (isCurrentComment) return true;
            if (otherColonist == null) return false;

            var val = 0;
            switch (this.ParamName)
            {
                case "SPORT": val = otherColonist.StorySport; break;
                case "INSTRUMENT": val = otherColonist.StoryInstrument; break;
                case "WORKEDPLACE": val = otherColonist.StoryWorkedplace; break;
            }

            return this.op.Test(val, this.Value);
        }
    }
}
