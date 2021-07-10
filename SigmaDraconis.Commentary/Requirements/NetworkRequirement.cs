namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class NetworkRequirement : RequirementBase
    {
        public string ParamName { get; }
        public float Value { get; }

        public NetworkRequirement(string paramName, Operator op, float value) : base(op)
        {
            this.ParamName = paramName;
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            switch (this.ParamName)
            {
                case "ENERGYGENNET": return this.op.Test(CommentaryContext.NetworkEnergyGenNet, this.Value);
                case "ENERGYTOT": return this.op.Test(CommentaryContext.NetworkEnergyTot, this.Value);
                case "FOODFREESPACE": return this.op.Test(CommentaryContext.FoodFreeSpace, (int)this.Value);
                case "STORAGEFREESPACE": return this.op.Test(CommentaryContext.StorageFreeSpace, (int)this.Value);
            }

            return false;
        }
    }
}
