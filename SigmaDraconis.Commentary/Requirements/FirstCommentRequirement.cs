namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class FirstCommentRequirement : RequirementBase
    {
        private readonly bool value;

        public FirstCommentRequirement(Operator op, bool value) : base(op)
        {
            this.value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            return isCurrentComment || this.op.Test(!CommentaryContext.ColonistsWithComments.Contains(colonist.Id), value);
        }
    }
}
