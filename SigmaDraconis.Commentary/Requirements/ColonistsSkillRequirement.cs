namespace SigmaDraconis.Commentary.Requirements
{
    using System;

    using Shared;

    using Context;
    using Operators;

    internal class ColonistsSkillRequirement : RequirementBase
    {
        public SkillType SkillType { get; }
        public bool Value { get; }

        public ColonistsSkillRequirement(string type, Operator op, bool value) : base (op)
        {
            this.SkillType = (SkillType)Enum.Parse(typeof(SkillType), type);
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            return this.op.Test(colonist.SkillType == this.SkillType, this.Value);
        }
    }
}
