namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using System.Linq;
    using ProtoBuf;
    using Cards.Interface;
    using Shared;

    [ProtoContract]
    public class CardCollection : ICardCollection
    {
        [ProtoMember(1)]
        private Dictionary<CardType, Dictionary<string, string>> serializationObject;

        [ProtoMember(2)]
        private Dictionary<int, CardType> socialCardTypesByColonist;

        public bool IsDisplayInvalidated { get; set; }

        public Dictionary<CardType, ICard> Cards { get; protected set; } = new Dictionary<CardType, ICard>();
        public Dictionary<CardType, ICard> TraitCards { get; protected set; } = new Dictionary<CardType, ICard>();

        private readonly Dictionary<CardEffectType, Dictionary<CardType, int>> cardEffects = new Dictionary<CardEffectType, Dictionary<CardType, int>>();

        public CardCollection()
        {
            if (this.socialCardTypesByColonist == null) this.socialCardTypesByColonist = new Dictionary<int, CardType>();
        }

        public ICard Add(CardType cardType)
        {
            if (this.Cards.ContainsKey(cardType))
            {
                var existingCard = this.Cards[cardType];
                this.ApplyCardEffects(cardType, existingCard);  // Effects may have changed, e.g. for diet
                return existingCard;
            }

            this.IsDisplayInvalidated = true;
            var card = CardFactory.Get(cardType);
            if (card != null) this.Add(cardType, card);
            return card;
        }

        public ICard AddMoodCard(int happinessEffect)
        {
            var cardType = happinessEffect > 0 ? CardType.GoodMood : CardType.BadMood;

            this.IsDisplayInvalidated = true;
            var card = CardFactory.Get(cardType);
            card.Effects[CardEffectType.Happiness] = happinessEffect;
            if (card != null) this.Add(cardType, card);
            return card;
        }

        public bool Contains(CardType cardType)
        {
            return this.Cards.ContainsKey(cardType);
        }

        public void Clear()
        {
            this.IsDisplayInvalidated = true;
            this.Cards.Clear();
            this.TraitCards.Clear();
            this.cardEffects.Clear();
            this.socialCardTypesByColonist.Clear();
        }

        public void Remove(CardType cardType)
        {
            this.IsDisplayInvalidated = true;

            if (this.Cards.ContainsKey(cardType)) this.Cards.Remove(cardType);
            if (this.TraitCards.ContainsKey(cardType)) this.TraitCards.Remove(cardType);

            var effectsToRemove = new List<CardEffectType>();
            foreach (var effect in this.cardEffects)
            {
                if (effect.Value.ContainsKey(cardType)) effect.Value.Remove(cardType);
                if (!effect.Value.Any()) effectsToRemove.Add(effect.Key);
            }

            foreach (var effect in effectsToRemove) this.cardEffects.Remove(effect);
        }

        public int GetEffectsSum(CardEffectType effect)
        {
            return this.cardEffects.ContainsKey(effect) ? this.cardEffects[effect].Values.Sum() : 0;
        }

        public bool GetEffectsAny(CardEffectType effect)
        {
            return this.cardEffects.ContainsKey(effect) ? this.cardEffects[effect].Any(e => e.Value != 0) : false;
        }

        public Dictionary<CardType, int> GetCardsByEffect(CardEffectType effect)
        {
            return this.cardEffects.ContainsKey(effect) ? this.cardEffects[effect] : new Dictionary<CardType, int>();
        }

        public void UpdateDarknessCard(float lightLevel, bool isAwake)
        {
            if (isAwake && lightLevel <= 0.25f) this.Add(CardType.Dark);
            else this.RemoveIfExists(CardType.Dark);
        }

        public void UpdateWorkOutdoorsCard(bool isWorkingAtLabOutside)
        {
            if (isWorkingAtLabOutside) this.Add(CardType.WorkOutside);
            else this.RemoveIfExists(CardType.WorkOutside);
        }

        public void UpdateHappinessCard(int happiness)
        {
            var cardType = CardType.NeutralHappiness;
            if (happiness >= 6) cardType = CardType.VeryHappy;
            else if (happiness >= 2) cardType = CardType.Happy;
            else if (happiness <= -6) cardType = CardType.VeryUnhappy;
            else if (happiness <= -2) cardType = CardType.Unhappy;

            if (!(this.Cards.Values.FirstOrDefault(c => c is HappinessCard) is HappinessCard happinessCard))
            {
                happinessCard = this.Add(cardType) as HappinessCard;
                this.IsDisplayInvalidated = true;
            }
            else if (happinessCard.Type != cardType)
            {
                this.Remove(happinessCard.Type);
                happinessCard = this.Add(cardType) as HappinessCard;
                this.IsDisplayInvalidated = true;
            }

            if (happinessCard != null && happinessCard.Happiness != happiness)
            {
                happinessCard.Happiness = happiness;
                this.IsDisplayInvalidated = true;
            }
        }

        public void UpdateKekCard(int kekHappinessTimer)
        {
            if (kekHappinessTimer > 0)
            {
                if (!this.Contains(CardType.Kek)) this.Add(CardType.Kek);
                if (this.Contains(CardType.Kek) && this.Cards[CardType.Kek] is KekCard card)
                {
                    var prevRemainingHours = card.RemainingHours;
                    var newRemainingHours = (1800 + kekHappinessTimer) / 3600;
                    if (newRemainingHours != prevRemainingHours)
                    {
                        card.RemainingHours = newRemainingHours;
                        this.IsDisplayInvalidated = true;
                    }
                }
            }
            else this.RemoveIfExists(CardType.Kek);
        }

        public void UpdateRoamCard(uint? framesSinceRoam, uint framesRoaming)
        {
            if (this.Contains(CardType.Roam) && this.Cards[CardType.Roam] is RoamCard card)
            {
                if (framesSinceRoam.GetValueOrDefault() > Constants.ColonistRoamCardTimeout) this.Remove(CardType.Roam);
                else
                {
                    var prevRemainingHours = card.RemainingHours;
                    var newRemainingHours = (int)(1800 + (Constants.ColonistRoamCardTimeout - framesSinceRoam.GetValueOrDefault())) / 3600;
                    if (newRemainingHours != prevRemainingHours)
                    {
                        card.RemainingHours = newRemainingHours;
                        this.IsDisplayInvalidated = true;
                    }
                }
            }
            else if (framesSinceRoam.HasValue && framesSinceRoam.Value < Constants.ColonistRoamCardTimeout && framesRoaming > Constants.ColonistRoamFramesForCard)
            {
                if (!this.Contains(CardType.Roam))
                {
                    this.Add(CardType.Roam);
                }
            }
        }

        public void UpdateSocialCards(Dictionary<int, int> framesSocialByColonist, Dictionary<int, int> framesSinceSocialByColonist, Dictionary<int, string> colonistNames)
        {
            foreach (var kv in framesSinceSocialByColonist)
            {
                if (kv.Value == 0 && framesSocialByColonist[kv.Key] >= Constants.ColonistSocialFramesForCard)
                {
                    // Card type (we can have up to three social cards, each has its own card type
                    var cardType = CardType.None;
                    if (this.socialCardTypesByColonist.ContainsKey(kv.Key)) cardType = this.socialCardTypesByColonist[kv.Key];
                    else if (!this.socialCardTypesByColonist.ContainsValue(CardType.Social1)) cardType = CardType.Social1;
                    else if (!this.socialCardTypesByColonist.ContainsValue(CardType.Social2)) cardType = CardType.Social2;
                    else if (!this.socialCardTypesByColonist.ContainsValue(CardType.Social3)) cardType = CardType.Social3;

                    // Add card if new
                    if (cardType != CardType.None && !this.Cards.ContainsKey(cardType))
                    {
                        if (!this.socialCardTypesByColonist.ContainsKey(kv.Key)) this.socialCardTypesByColonist.Add(kv.Key, cardType);
                        this.Add(cardType, new SocialCard(cardType, colonistNames[kv.Key]));
                        this.IsDisplayInvalidated = true;
                    }
                }
                else if (kv.Value > Constants.ColonistSocialCardTimeout && this.socialCardTypesByColonist.ContainsKey(kv.Key) && this.Cards.ContainsKey(this.socialCardTypesByColonist[kv.Key]))
                {
                    // Remove card
                    this.Remove(this.socialCardTypesByColonist[kv.Key]);
                    this.socialCardTypesByColonist.Remove(kv.Key);
                }
            }
        }

        public void UpdateWorkloadCard(StressLevel workload)
        {
            var cardType = CardType.WorkloadLow;
            if (workload == StressLevel.Extreme) cardType = CardType.WorkloadExtreme;
            else if (workload == StressLevel.High) cardType = CardType.WorkloadHigh;
            else if (workload == StressLevel.Moderate) cardType = CardType.WorkloadModerate;

            if (!(this.Cards.Values.FirstOrDefault(c => c is WorkloadCard) is WorkloadCard workloadCard))
            {
                workloadCard = this.Add(cardType) as WorkloadCard;
            }
            else if (workloadCard.Type != cardType)
            {
                this.Remove(workloadCard.Type);
                workloadCard = this.Add(cardType) as WorkloadCard;
            }
        }

        public void UpdateHotColdCard(double bodyTemperature, int hypothermiaSeverity, int hyperthermiaSeverity)
        {
            var cardType = CardType.None;

            if (bodyTemperature <= 15)
            {
                this.RemoveIfExists(CardType.Cold1);
                this.RemoveIfExists(CardType.Hot1);
                this.RemoveIfExists(CardType.Hot2);
                cardType = CardType.Cold2;
            }
            else if (bodyTemperature <= 18)
            {
                this.RemoveIfExists(CardType.Cold2);
                this.RemoveIfExists(CardType.Hot1);
                this.RemoveIfExists(CardType.Hot2);
                cardType = CardType.Cold1;
            }
            else if (bodyTemperature >= 25)
            {
                this.RemoveIfExists(CardType.Cold1);
                this.RemoveIfExists(CardType.Cold2);
                this.RemoveIfExists(CardType.Hot1);
                cardType = CardType.Hot2;
            }
            else if (bodyTemperature >= 22)
            {
                this.RemoveIfExists(CardType.Cold1);
                this.RemoveIfExists(CardType.Cold2);
                this.RemoveIfExists(CardType.Hot2);
                cardType = CardType.Hot1;
            }
            else
            {
                this.RemoveIfExists(CardType.Cold1);
                this.RemoveIfExists(CardType.Cold2);
                this.RemoveIfExists(CardType.Hot1);
                this.RemoveIfExists(CardType.Hot2);
            }

            if (cardType != CardType.None && !this.Contains(cardType))
            {
                this.Add(cardType);
                this.IsDisplayInvalidated = true;
            }

            if (this.Cards.ContainsKey(CardType.Cold3))
            {
                var card = this.Cards[CardType.Cold3];
                if (card is Cold3Card c && c.Severity != hypothermiaSeverity)
                {
                    this.IsDisplayInvalidated = true;
                    if (hypothermiaSeverity > 0) c.Severity = hypothermiaSeverity;
                    else this.Remove(CardType.Cold3);
                }
            }
            else if (hypothermiaSeverity > 0 && this.Add(CardType.Cold3) is Cold3Card c)
            {
                c.Severity = hypothermiaSeverity;
                this.IsDisplayInvalidated = true;
            }


            if (this.Cards.ContainsKey(CardType.Hot3))
            {
                var card = this.Cards[CardType.Hot3];
                if (card is Hot3Card c && c.Severity != hyperthermiaSeverity)
                {
                    this.IsDisplayInvalidated = true;
                    if (hyperthermiaSeverity > 0) c.Severity = hyperthermiaSeverity;
                    else this.Remove(CardType.Hot3);
                }
            }
            else if (hyperthermiaSeverity > 0 && this.Add(CardType.Hot3) is Hot3Card c)
            {
                c.Severity = hyperthermiaSeverity;
                this.IsDisplayInvalidated = true;
            }
        }

        public void UpdateHungerCard(double nourishment, int starvationSeverity)
        {
            var cardType = CardType.None;

            if (nourishment <= 25)
            {
                this.RemoveIfExists(CardType.Hunger1);
                cardType = CardType.Hunger2;
            }
            else if (nourishment <= 60)
            {
                this.RemoveIfExists(CardType.Hunger2);
                cardType = CardType.Hunger1;
            }
            else
            {
                this.RemoveIfExists(CardType.Hunger1);
                this.RemoveIfExists(CardType.Hunger2);
            }

            if (cardType != CardType.None && !this.Contains(cardType))
            {
                this.Add(cardType);
                this.IsDisplayInvalidated = true;
            }

            if (this.Cards.ContainsKey(CardType.Hunger3))
            {
                var card = this.Cards[CardType.Hunger3];
                if (card is Hunger3Card c && c.Severity != starvationSeverity)
                {
                    this.IsDisplayInvalidated = true;
                    if (starvationSeverity > 0) c.Severity = starvationSeverity;
                    else this.Remove(CardType.Hunger3);
                }
            }
            else if (starvationSeverity > 0 && this.Add(CardType.Hunger3) is Hunger3Card c)
            {
                c.Severity = starvationSeverity;
                this.IsDisplayInvalidated = true;
            }
        }

        public void UpdateThirstCard(double hydration, int dehydrationSeverity)
        {
            var cardType = CardType.None;

            if (hydration <= 25)
            {
                this.RemoveIfExists(CardType.Thirst1);
                cardType = CardType.Thirst2;
            }
            else if (hydration <= 60)
            {
                this.RemoveIfExists(CardType.Thirst2);
                cardType = CardType.Thirst1;
            }
            else
            {
                this.RemoveIfExists(CardType.Thirst1);
                this.RemoveIfExists(CardType.Thirst2);
            }

            if (cardType != CardType.None && !this.Contains(cardType))
            {
                this.Add(cardType);
                this.IsDisplayInvalidated = true;
            }

            if (this.Cards.ContainsKey(CardType.Thirst3))
            {
                var card = this.Cards[CardType.Thirst3];
                if (card is Thirst3Card c && c.Severity != dehydrationSeverity)
                {
                    this.IsDisplayInvalidated = true;
                    if (dehydrationSeverity > 0) c.Severity = dehydrationSeverity;
                    else this.Remove(CardType.Thirst3);
                }
            }
            else if (dehydrationSeverity > 0 && this.Add(CardType.Thirst3) is Thirst3Card c)
            {
                c.Severity = dehydrationSeverity;
                this.IsDisplayInvalidated = true;
            }
        }

        public void UpdateTirednessCard(double energy)
        {
            if (energy <= 15)
            {
                this.RemoveIfExists(CardType.Tired1);
                if (!this.Contains(CardType.Tired2)) this.Add(CardType.Tired2);
            }
            else if (energy <= 25)
            {
                this.RemoveIfExists(CardType.Tired2);
                if (!this.Contains(CardType.Tired1)) this.Add(CardType.Tired1);
            }
            else
            {
                this.RemoveIfExists(CardType.Tired1);
                this.RemoveIfExists(CardType.Tired2);
            }
        }

        public void UpdateNewArrivalCard(int newArrivalHappiness)
        {
            if (this.Cards.ContainsKey(CardType.NewArrival))
            {
                var card = this.Cards[CardType.NewArrival];
                if (card is NewArrivalCard c && c.Happiness != newArrivalHappiness)
                {
                    this.IsDisplayInvalidated = true;
                    if (newArrivalHappiness > 0) c.Happiness = newArrivalHappiness;
                    else this.Remove(CardType.NewArrival);
                }
            }
            else if (newArrivalHappiness > 0 && this.Add(CardType.NewArrival) is NewArrivalCard c)
            {
                if (c.Happiness != newArrivalHappiness)
                {
                    c.Happiness = newArrivalHappiness;
                    this.ApplyCardEffects(CardType.NewArrival, c);
                }

                this.IsDisplayInvalidated = true;
            }
        }

        public void UpdateNewColonyCard(int remainingHours)
        {
            if (this.Cards.ContainsKey(CardType.NewColony))
            {
                var card = this.Cards[CardType.NewColony];
                if (card is NewColonyCard c && c.RemainingHours != remainingHours)
                {
                    if (remainingHours > 0) c.RemainingHours = remainingHours;
                    else this.Remove(CardType.NewColony);
                }
            }
            else if (remainingHours > 0 && this.Add(CardType.NewColony) is NewColonyCard c) c.RemainingHours = remainingHours;
        }

        public void UpdateLonelinessCard(int happinessPenalty)
        {
            if (happinessPenalty > 0)
            {
                if (!this.Contains(CardType.Lonely)) this.Add(CardType.Lonely);
                var card = this.Cards[CardType.Lonely];
                card.Effects[CardEffectType.Happiness] = 0 - happinessPenalty;
                this.cardEffects[CardEffectType.Happiness][CardType.Lonely] = 0 - happinessPenalty;
                this.IsDisplayInvalidated = true;
            }
            else this.RemoveIfExists(CardType.Lonely);
        }

        private void Add(CardType cardType, ICard card)
        {
            this.Cards.Add(cardType, card);
            if (card is TraitCard) this.TraitCards.Add(cardType, card);
            this.ApplyCardEffects(cardType, card);
        }

        private void ApplyCardEffects(CardType cardType, ICard card)
        {
            foreach (var effect in card.Effects)
            {
                if (!this.cardEffects.ContainsKey(effect.Key)) this.cardEffects.Add(effect.Key, new Dictionary<CardType, int> { { cardType, effect.Value } });
                else if (this.cardEffects[effect.Key].ContainsKey(cardType)) this.cardEffects[effect.Key][cardType] = effect.Value;
                else this.cardEffects[effect.Key].Add(cardType, effect.Value);
            }
        }

        private void RemoveIfExists(CardType cardType)
        {
            if (this.Cards.ContainsKey(cardType))
            {
                this.Remove(cardType);
                this.IsDisplayInvalidated = true;
            }
        }

#pragma warning disable IDE0051 // Private members not unused - they are needed for serialization

        [ProtoBeforeSerialization]

        private void BeforeSerialization()
        {
            this.serializationObject = this.Cards.ToDictionary(c => c.Key, c => c.Value.GetSerializationObject());
        }


        [ProtoAfterDeserialization]
        private void AfterDeserialization()
        {
            if (this.serializationObject == null) this.serializationObject = new Dictionary<CardType, Dictionary<string, string>>();

            foreach (var o in this.serializationObject)
            {
                var card = CardFactory.Get(o.Key);
                if (card != null)
                {
                    card.InitFromSerializationObject(o.Value);
                    this.Add(o.Key, card);
                }
            }

            if (this.socialCardTypesByColonist == null) this.socialCardTypesByColonist = new Dictionary<int, CardType>();
        }

#pragma warning restore IDE0051 // Private members not unused - they are needed for serialization
    }
}
