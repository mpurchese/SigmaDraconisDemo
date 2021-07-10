namespace SigmaDraconis.Commentary.Requirements
{
    using Context;
    using Operators;

    internal class FoodPreferenceRequirement : RequirementBase
    {
        public int FoodTypeId { get; }
        public string ParamName { get; }
        public bool Value { get; }

        public FoodPreferenceRequirement(string type, string paramName, Operator op, bool value) : base (op)
        {
            this.FoodTypeId = int.Parse(type);
            this.ParamName = paramName;
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            switch (this.ParamName)
            {
                case "LIKE": return this.op.Test(colonist.LikedFoodIds.Contains(this.FoodTypeId), this.Value);
                case "DISLIKE": return this.op.Test(colonist.DislikedFoodIds.Contains(this.FoodTypeId), this.Value);
            }

            return false;
        }
    }
}
