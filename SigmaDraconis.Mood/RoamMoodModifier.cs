namespace SigmaDraconis.Mood
{
    using Shared;
    using WorldInterfaces;

    public class RoamMoodModifier : IMoodModifer
    {
        public string Description { get; private set; }

        public int Value { get; private set; }

        public void Update(IColonist colonist)
        {
            if (!colonist.Body.IsSleeping && colonist.ActivityType == ColonistActivityType.Roam)
            {
                this.Value = -colonist.RoamDestressRate;
                this.Description = "Relaxing walk";
            }
            else
            {
                this.Value = 0;
                this.Description = "";
            }
        }
    }
}
