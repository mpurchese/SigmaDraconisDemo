namespace SigmaDraconis.Mood
{
    using Shared;
    using WorldInterfaces;

    public class WorkTiredMoodModifier : IMoodModifer
    {
        public string Description { get; private set; }

        public int Value { get; private set; }

        public void Update(IColonist colonist)
        {
            if (!colonist.Body.IsSleeping && colonist.ActivityType.GetAttribute<IsWorkAttribute>()?.Value == true && (100 - colonist.Body.Energy) > Constants.ColonistStartSleepTiredness)
            {
                this.Value = 10;
                this.Description = "Working when tired";
            }
            else
            {
                this.Value = 0;
                this.Description = "";
            }
        }
    }
}
