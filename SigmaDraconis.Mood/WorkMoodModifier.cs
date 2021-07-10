namespace SigmaDraconis.Mood
{
    using Shared;
    using Cards.Interface;
    using WorldInterfaces;

    public class WorkMoodModifier : IMoodModifer
    {
        public string Description { get; private set; }

        public int Value { get; private set; }

        public void Update(IColonist colonist)
        {
            if (!colonist.Body.IsSleeping && !colonist.Cards.Contains(CardType.Workaholic) && colonist.ActivityType.GetAttribute<IsWorkAttribute>()?.Value == true)
            {
                this.Value = 5;
                this.Description = "Working";
            }
            else
            {
                this.Value = 0;
                this.Description = "";
            }
        }
    }
}
