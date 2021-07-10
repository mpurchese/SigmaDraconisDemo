namespace SigmaDraconis.Mood
{
    using System.Linq;
    using Shared;
    using WorldInterfaces;

    public class SleepMoodModifier : IMoodModifer
    {
        public string Description { get; private set; }

        public int Value { get; private set; }

        public void Update(IColonist colonist)
        {
            if (colonist.Body.IsSleeping && colonist.MainTile.ThingsAll.FirstOrDefault(t => t.ThingType == ThingType.SleepPod) is ISleepPod s)
            {
                if (s.OwnerID == colonist.Id)
                {
                    this.Description = "Sleeping in own pod";
                    this.Value = -6;
                }
                else
                {
                    this.Description = "Sleeping in pod";
                    this.Value = -4;
                }
            }
            else
            {
                this.Description = "";
                this.Value = 0;
            }
        }
    }
}
