namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class DiceRequirement : RequirementBase
    {
        private readonly int diceId;
        private readonly int value;

        public DiceRequirement(int diceId, Operator op, int value) : base(op)
        {
            this.diceId = diceId;
            this.value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            return isCurrentComment || this.op.Test(CommentaryContext.GetDiceRoll(diceId), value);
        }
    }
}
