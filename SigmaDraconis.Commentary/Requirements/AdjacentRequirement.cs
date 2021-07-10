namespace SigmaDraconis.Commentary.Requirements
{
    using System;
    using System.Linq;

    using Shared;

    using Context;
    using Operators;

    internal class AdjacentRequirement : RequirementBase
    {
        public ThingType ThingType { get; }
        public string ParamName { get; }
        public bool Value { get; }

        public AdjacentRequirement(string type, string paramName, Operator op, bool value) : base (op)
        {
            this.ThingType = (ThingType)Enum.Parse(typeof(ThingType), type);
            this.ParamName = paramName;
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            if (isCurrentComment) return true;

            var val = false;
            switch (this.ParamName)
            {
                case "READY": val = colonist.AdjacentThings.Any(t => t.ThingType == this.ThingType && t.IsReady); break;
                case "FLOWERING": val = colonist.AdjacentThings.Any(t => t.ThingType == this.ThingType && t.IsFlowering); break;
                case "FRUITING": val = colonist.AdjacentThings.Any(t => t.ThingType == this.ThingType && t.IsFruiting); break;
                case "UNRIPEFRUIT": val = colonist.AdjacentThings.Any(t => t.ThingType == this.ThingType && t.HasUnripeFruit); break;
                case "RESOURCETYPECOAL": val = colonist.AdjacentThings.Any(t => t.ThingType == this.ThingType && t.ResourceType == ItemType.Coal); break;
                default: val = colonist.AdjacentThings.Any(t => t.ThingType == this.ThingType); break;
            }

            return this.op.Test(val, this.Value);
        }
    }
}
