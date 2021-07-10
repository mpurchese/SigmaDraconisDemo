namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;
    using Shared;

    internal class LowFoodRequirement : RequirementBase
    {
        public bool Value { get; }

        public LowFoodRequirement(Operator op, bool value) : base (op)
        {
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            var food = CommentaryContext.ItemTypeCounts[ItemType.Food] + CommentaryContext.ItemTypeCounts[ItemType.Mush];
            var colonists = CommentaryContext.LiveColonists.Count;
            return this.op.Test(food < colonists, this.Value);
        }
    }
}
