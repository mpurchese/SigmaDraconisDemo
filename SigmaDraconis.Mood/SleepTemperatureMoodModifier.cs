namespace SigmaDraconis.Mood
{
    using System;
    using WorldInterfaces;

    public class SleepTemperatureMoodModifier : IMoodModifer
    {
        public string Description { get; private set; }

        public int Value { get; private set; }

        public void Update(IColonist colonist)
        {
            if (!colonist.Body.IsSleeping)
            {
                var val = colonist.SleptTemperature.GetValueOrDefault(20) - 20;
                if (val < -7)
                {
                    this.Description = "Slept in the cold";
                    this.Value = 0 - (val + 7);
                }
                else if (val > 7)
                {
                    this.Description = "Slept in the heat";
                    this.Value = val - 7;
                }
                else
                {
                    this.Description = "";
                    this.Value = 0;
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
