namespace SigmaDraconis.Mood
{
    using System.Linq;
    using Shared;
    using WorldInterfaces;

    public class RelaxMoodModifier : IMoodModifer
    {
        public string Description { get; private set; }
        public int Value { get; private set; }

        public void Update(IColonist colonist)
        {
            if (!colonist.Body.IsSleeping && colonist.IsRelaxing == true && colonist.ActivityType == ColonistActivityType.Relax)
            {
                var social = colonist.TimeSinceSocialByColonist.Any(c => c.Value == 0);
                this.Value = -colonist.RelaxDestressRate - (social ? 4 : 0);
                this.Description = social ? "Relaxing socially" : "Relaxing";
            }
            else
            {
                this.Value = 0;
                this.Description = "";
            }
        }
    }
}
