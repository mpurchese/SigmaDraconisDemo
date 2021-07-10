namespace SigmaDraconis.Commentary.Requirements
{
    using System;
    using System.Linq;

    using Shared;

    using Context;
    using Operators;

    internal class CountRequirement : RequirementBase
    {
        public ThingType ThingType { get; }
        public string ParamName { get; }
        public int Value { get; }

        public CountRequirement(string typeDef, string paramName, Operator op, int value) : base (op)
        {
            this.ThingType = (ThingType)Enum.Parse(typeof(ThingType), typeDef);
            this.ParamName = paramName;
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            var val = 0;
            switch (this.ParamName)
            {
                case "READY": val = CommentaryContext.ProxiesByThingType[this.ThingType].Count(t => t.IsReady); break;
                default: val = CommentaryContext.ProxiesByThingType[this.ThingType].Count; break;
            }

            return this.op.Test(val, this.Value);
        }
    }
}
