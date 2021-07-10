namespace SigmaDraconis.Cards.Interface
{
    using System.Collections.Generic;
    using Shared;

    public interface ICardCollection
    {
        Dictionary<CardType, ICard> Cards { get; }
        Dictionary<CardType, ICard> TraitCards { get; }
        bool IsDisplayInvalidated { get; set; }

        void Clear();
        ICard Add(CardType cardType);
        ICard AddMoodCard(int happinessEffect);
        bool Contains(CardType cardType);
        void Remove(CardType cardType);
        void UpdateHappinessCard(int happiness);
        void UpdateHotColdCard(double bodyTemperature, int hypothermiaSeverity, int hyperthermiaSeverity);
        void UpdateHungerCard(double nourishment, int starvationSeverity);
        void UpdateThirstCard(double hydration, int dehydrationSeverity);
        void UpdateRoamCard(uint? framesSinceRoam, uint framesRoaming);
        void UpdateKekCard(int kekHappinessTimer);
        void UpdateTirednessCard(double energy);
        void UpdateNewArrivalCard(int newArrivalHappiness);
        void UpdateNewColonyCard(int remainingHours);
        void UpdateLonelinessCard(int happinessPenalty);
        void UpdateWorkloadCard(StressLevel workload);
        void UpdateDarknessCard(float lightLevel, bool isAwake);
        void UpdateWorkOutdoorsCard(bool isWorkingAtLabOutside);

        bool GetEffectsAny(CardEffectType effect);
        int GetEffectsSum(CardEffectType effect);
        Dictionary<CardType, int> GetCardsByEffect(CardEffectType effect);
        void UpdateSocialCards(Dictionary<int, int> framesSocialByColonist, Dictionary<int, int> framesSinceSocialByColonist, Dictionary<int, string> colonistNames);
    }
}
