namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class ColonistEnergyRequirement : RequirementBase
    {
        public double Value { get; }

        public ColonistEnergyRequirement(Operator op, double value) : base (op)
        {
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            return this.op.Test(colonist.Energy, this.Value);
        }
    }
}
