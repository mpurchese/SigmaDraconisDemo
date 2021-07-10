namespace SigmaDraconis.Mood
{
    using WorldInterfaces;

    public class RestMoodModifier : IMoodModifer
    {
        public string Description { get; private set; }

        public int Value { get; private set; }

        public void Update(IColonist colonist)
        {
            if (!colonist.Body.IsSleeping && colonist.Body.Energy > 90)
            {
                this.Description = "Well rested";
                this.Value = -4;
            }
            else if (!colonist.Body.IsSleeping && colonist.Body.Energy < 15)
            {
                this.Description = "Very tired";
                this.Value = 4;
            }
            else
            {
                this.Description = "";
                this.Value = 0;
            }
        }
    }
}
