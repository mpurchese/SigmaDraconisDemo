namespace SigmaDraconis.AI
{
    using Shared;
    using WorldInterfaces;
    using World.PathFinding;

    public class ResourceStackingJob
    {
        private static int nextId;
        private readonly string description;

        public int Id { get; private set; }
        public IColonistInteractive Source { get; private set; }
        public IColonistInteractive Target { get; private set; }
        public ItemType ItemType { get; private set; }
        public Path Path { get; set; }
        public WorkPriority Priority { get; set; }
        public bool IsInTransit { get; set; }

        public ResourceStackingJob(IColonistInteractive source, IColonistInteractive target, ItemType itemType, WorkPriority priority, Path path)
        {
            this.Id = nextId++;
            this.Source = source;
            this.Target = target;
            this.ItemType = itemType;
            this.Priority = priority;
            this.Path = path;
            this.IsInTransit = source is IColonist;
            this.description = $"job {this.Id} [{itemType} from {source} to {target}]";
        }

        public override string ToString()
        {
            return this.description;
        }
    }
}
