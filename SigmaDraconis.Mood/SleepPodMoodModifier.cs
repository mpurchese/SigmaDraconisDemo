namespace SigmaDraconis.Mood
{
    using WorldInterfaces;

    public class SleepPodMoodModifier : IMoodModifer
    {
        public string Description { get; private set; }

        public int Value { get; private set; }

        public void Update(IColonist colonist)
        {
            if (!colonist.Body.IsSleeping && colonist.SleptOutside == true)
            {
                this.Description = "Slept outside";
                this.Value = 5;
            }
            else if (!colonist.Body.IsSleeping && colonist.SleptInPod == false)
            {
                this.Description = "Slept without pod";
                this.Value = 3;
            }
            else
            {
                this.Description = "";
                this.Value = 0;
            }
        }
    }
}
