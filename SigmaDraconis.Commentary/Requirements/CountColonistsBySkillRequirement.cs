namespace SigmaDraconis.Commentary.Requirements
{
    using System;
    using System.Linq;

    using Shared;

    using Context;
    using Operators;

    internal class CountColonistsBySkillRequirement : RequirementBase
    {
        public SkillType SkillType { get; }
        public int Value { get; }

        public CountColonistsBySkillRequirement(string type, Operator op, int value) : base (op)
        {
            this.SkillType = (SkillType)Enum.Parse(typeof(SkillType), type);
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            return this.op.Test(CommentaryContext.LiveColonists.Count(c => c.SkillType == this.SkillType), this.Value);
        }
    }
}
