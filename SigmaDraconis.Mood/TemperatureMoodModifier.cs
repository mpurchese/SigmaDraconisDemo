namespace SigmaDraconis.Mood
{
    using System;
    using WorldInterfaces;

    public class TemperatureMoodModifier : IMoodModifer
    {
        public string Description { get; private set; }

        public int Value { get; private set; }

        public void Update(IColonist colonist)
        {
            var temp = (int)(Math.Round(colonist.Body.Temperature));
            if (temp >= 30)
            {
                this.Value = 8;
                this.Description = "Extremely hot";
            }
            else if (temp >= 25)
            {
                this.Value = 5;
                this.Description = "Very hot";
            }
            else if (temp >= 22)
            {
                this.Value = 2;
                this.Description = "Feeling hot";
            }
            else if (temp <= 10)
            {
                this.Value = 8;
                this.Description = "Freezing cold";
            }
            else if (temp <= 15)
            {
                this.Value = 5;
                this.Description = "Very cold";
            }
            else if (temp <= 18)
            {
                this.Value = 2;
                this.Description = "Feeling cold";
            }
            else
            {
                this.Value = 0;
                this.Description = "";
            }
        }
    }
}
