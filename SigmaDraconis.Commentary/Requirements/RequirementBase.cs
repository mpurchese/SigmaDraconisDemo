namespace SigmaDraconis.Commentary.Requirements
{
    using System.Collections.Generic;
    using Context;
    using Operators;

    internal abstract class RequirementBase
    {
        protected readonly Operator op;
        protected readonly List<RequirementBase> alternatives = new List<RequirementBase>();

        protected RequirementBase(Operator op)
        {
            this.op = op;
        }

        public void AddAlternative(RequirementBase alt)
        {
            this.alternatives.Add(alt);
        }

        public bool Test(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist = null)
        {
            var result = TestThis(colonist, isCurrentComment, otherColonist);
            foreach (var alt in this.alternatives) result |= alt.Test(colonist, isCurrentComment, otherColonist);
            return result;
        }

        protected abstract bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist);
    }
}
