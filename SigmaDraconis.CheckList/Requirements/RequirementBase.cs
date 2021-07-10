namespace SigmaDraconis.CheckList.Requirements
{
    using System.Collections.Generic;
    using System.Linq;
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

        public bool Test()
        {
            return TestThis() || this.alternatives.Any(a => a.Test());
        }

        protected abstract bool TestThis();
    }
}
