namespace SigmaDraconis.Mood
{
    using Shared;
    using WorldInterfaces;

    public class WorkStressedMoodModifier : IMoodModifer
    {
        public string Description { get; private set; }

        public int Value { get; private set; }

        public void Update(IColonist colonist)
        {
            if (!colonist.Body.IsSleeping && colonist.ActivityType.GetAttribute<IsWorkAttribute>()?.Value == true && colonist.Stress >= Constants.ColonistMaxStressLevel * 0.6f)
            {
                this.Value = 5;
                this.Description = "Working stressed";
            }
            else
            {
                this.Value = 0;
                this.Description = "";
            }
        }
    }
}
