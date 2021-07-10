namespace SigmaDraconis.Mood
{
    using World.Rooms;
    using WorldInterfaces;

    public class LightDarkMoodModifier : IMoodModifer
    {
        public string Description { get; private set; }

        public int Value { get; private set; }

        public void Update(IColonist colonist)
        {
            if (colonist.Body.IsSleeping || colonist.IsResting)
            {
                this.Value = 0;
                this.Description = "";
            }
            else
            {
                var light = RoomManager.GetTileLightLevel(colonist.MainTileIndex);
                if (light < 0.2f)
                {
                    this.Value = 4;
                    this.Description = "In darkness";
                }
                else if (light > 0.9f)
                {
                    var temperature = RoomManager.GetTileTemperature(colonist.MainTileIndex);
                    if (temperature >= 15 && temperature < 30)
                    {
                        this.Value = -2;
                        this.Description = "Nice weather";
                    }
                    else
                    {
                        this.Value = 0;
                        this.Description = "";
                    }
                }
                else
                {
                    this.Value = 0;
                    this.Description = "";
                }
            }
        }
    }
}
