namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;
    using Cards.Interface;
    using Shared;

    public interface IColonistPlaceholder
    {
        int Index { get; set; }
        string Name { get; set; }
        List<CardType> Traits { get; }
        SkillType Skill { get; }
        List<string> Story { get; }
        Dictionary<int, int> FoodOpinions { get; }
        int ColourCode { get; set; }
        int HairColourCode { get; set; }
        ColonistPlaceholderStatus PlaceHolderStatus { get; set; }
        bool IsWakeCommitted { get; set; }
        int TimeToArrivalInFrames { get; set; }
        int? ActualColonistID { get; set; }

        void GenerateStory();
    }
}
