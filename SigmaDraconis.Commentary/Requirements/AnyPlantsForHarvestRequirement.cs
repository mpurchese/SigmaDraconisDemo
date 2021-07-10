namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class AnyPlantsForHarvestRequirement : RequirementBase
    {
        public bool Value { get; }

        public AnyPlantsForHarvestRequirement(Operator op, bool value) : base (op)
        {
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            return this.op.Test(CommentaryContext.AnyPlantsForHarvest, this.Value);
        }
    }
}
