namespace SigmaDraconis.Mood
{
    using System.Text;
    using World;
    using WorldInterfaces;

    public class SocialMoodModifier : IMoodModifer
    {
        //public string Description { get; private set; }
        //public int Value { get; private set; }

        public string Description => "";

        public int Value => 0;

        public void Update(IColonist colonist)
        {
            // TODO
        }

    //    public void Update(IColonist colonist)
    //    {
    //        if (colonist.IsSleeping)
    //        {
    //            this.Value = 0;
    //            this.Description = "";
    //            return;
    //        }

    //        this.Value = 0;
    //        var text = new StringBuilder();

    //        foreach(var kv in colonist.TimeSinceSocialByColonist)
    //        {
    //            if (kv.Value < 2000)   // 20 hours
    //            {
    //                if (World.GetThing(kv.Key) is IColonist c)
    //                {
    //                    if (this.Value > 0) text.Append("|");
    //                    text.Append($"Talked with {c.ShortName}");
    //                    this.Value -= 2;
    //                }
    //            }
    //        }

    //        this.Description = text.ToString();
    //    }
    }
}
