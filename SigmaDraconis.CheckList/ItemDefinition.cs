namespace SigmaDraconis.CheckList
{
    using System.Collections.Generic;
    using Requirements;

    internal class ItemDefinition
    {
        private readonly List<RequirementBase> requirementsStart = new List<RequirementBase>();
        private readonly List<RequirementBase> requirementsComplete = new List<RequirementBase>();

        public int Id { get; }

        public ItemDefinition(int id)
        {
            this.Id = id;
        }

        public void AddRequirementStart(RequirementBase requirement)
        {
            this.requirementsStart.Add(requirement);
        }

        public void AddRequirementComplete(RequirementBase requirement)
        {
            this.requirementsComplete.Add(requirement);
        }

        public bool TestStart()
        {
            return this.requirementsStart.TrueForAll(r => r.Test());
        }

        public bool TestComplete()
        {
            return this.requirementsComplete.TrueForAll(r => r.Test());
        }
    }
}
