namespace SigmaDraconis.World.Projects
{
    using System.Collections.Generic;
    using Language;
    using Shared;

    public class Project
    {
        public int Id { get; set; }
        public string DisplayName => LanguageManager.GetProjectName(this.Id);
        public string Description => LanguageManager.GetProjectDescription(this.Id);
        public int DisplayOrder { get; set; }
        public int TotalWork { get; set; }
        public int RemainingWork { get; set; }
        public bool IsDone => this.RemainingWork <= 0;
        public ThingType LabType { get; set; }
        public List<ThingType> LockedBuildings { get; set; } = new List<ThingType>();
        public List<int> RequiredProjects { get; set; } = new List<int>();
        public SkillType SkillType { get; set; }
    }
}