namespace SigmaDraconis.Commentary
{
    using System.Collections.Generic;
    using System.Linq;
    using Context;
    using Params;
    using Requirements;

    internal class CommentDefinition
    {
        private readonly List<RequirementBase> requirements = new List<RequirementBase>();
        private readonly List<ITemplateParam> templateParams = new List<ITemplateParam>();
        private string template;

        public int Id { get; }

        public bool IsImportant { get; set; }
        public bool IsUrgent { get; set; }
        public bool IsSticky { get; set; }
        public int RepeatDelay { get; set; } = int.MaxValue;
        public int FollowedBy { get; set; } = -1;
        public HashSet<int> DontFollow { get; set; } = new HashSet<int>();
        public bool IsSequenceOnly { get; set; }
        public bool IsSleepComment { get; set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(this.template);

        public CommentDefinition(int id)
        {
            this.Id = id;
        }

        public void AddRequirement(RequirementBase requirement)
        {
            this.requirements.Add(requirement);
        }

        public void SetTemplate(string template, List<ITemplateParam> templateParams)
        {
            this.template = template;
            this.templateParams.Clear();
            this.templateParams.AddRange(templateParams);
        }

        public bool CheckStillApplies(ColonistProxy colonist)
        {
            return colonist != null && this.requirements.TrueForAll(r => r.Test(colonist, true));
        }

        public bool Test(ColonistProxy colonist, long frameLastUsed, bool isCurrentComment = false, ColonistProxy otherColonist = null)
        {
            return ((isCurrentComment && this.IsSticky) || frameLastUsed == 0 || frameLastUsed + this.RepeatDelay < CommentaryContext.FrameNumber) 
                && this.requirements.TrueForAll(r => r.Test(colonist, isCurrentComment, otherColonist));
        }

        public string GetText(ColonistProxy colonist, ColonistProxy otherColonist)
        {
            return string.Format(this.template, this.templateParams.Select(p => p.Evaluate(colonist, otherColonist)).ToArray());
        }
    }
}
