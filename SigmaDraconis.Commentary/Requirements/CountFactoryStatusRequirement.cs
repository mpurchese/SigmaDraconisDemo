namespace SigmaDraconis.Commentary.Requirements
{
    using System;
    using System.Linq;

    using Shared;

    using Context;
    using Operators;

    internal class CountFactoryStatusRequirement : RequirementBase
    {
        public ThingType ThingType { get; }
        public FactoryStatus Status { get; }
        public int Value { get; }

        public CountFactoryStatusRequirement(string type, string status, Operator op, int value) : base (op)
        {
            this.ThingType = (ThingType)Enum.Parse(typeof(ThingType), type);
            this.Status = (FactoryStatus)Enum.Parse(typeof(FactoryStatus), status);
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            return this.op.Test(CommentaryContext.ProxiesByThingType[this.ThingType].Count(t => t.FactoryStatus == this.Status), this.Value);
        }
    }
}
