namespace SigmaDraconis.Cards
{
    using System.Collections.Generic;
    using Language;
    using Interface;

    public class DietCard : Card
    {
        private int likesEffect;
        private int varietyEffect;
        private string likesStr;
        private string dislikesStr;

        public DietCard(CardType type, CardDisplayType displayType) : base(type, displayType)
        {
            this.Effects = new Dictionary<CardEffectType, int>();
        }

        public void Update(int likesEffect, int varietyEffect, List<string> likedFoods, List<string> dislikedFoods)
        {
            this.likesEffect = likesEffect;
            this.varietyEffect = varietyEffect;
            if (!this.Effects.ContainsKey(CardEffectType.Happiness)) this.Effects.Add(CardEffectType.Happiness, likesEffect + varietyEffect);
            else this.Effects[CardEffectType.Happiness] = likesEffect + varietyEffect;

            this.likesStr = "";
            if (likedFoods.Count >= 3) this.likesStr = LanguageManager.Get<StringsForColonistStory>(StringsForColonistStory.List3, likedFoods[0], likedFoods[1], likedFoods[2]);
            else if (likedFoods.Count == 2) this.likesStr = LanguageManager.Get<StringsForColonistStory>(StringsForColonistStory.List2, likedFoods[0], likedFoods[1]);
            else if (likedFoods.Count == 1) this.likesStr = LanguageManager.Get<StringsForColonistStory>(StringsForColonistStory.List1, likedFoods[0]);

            this.dislikesStr = "";
            if (dislikedFoods.Count >= 3) this.dislikesStr = LanguageManager.Get<StringsForColonistStory>(StringsForColonistStory.List3, dislikedFoods[0], dislikedFoods[1], dislikedFoods[2]);
            else if (dislikedFoods.Count == 2) this.dislikesStr = LanguageManager.Get<StringsForColonistStory>(StringsForColonistStory.List2, dislikedFoods[0], dislikedFoods[1]);
            else if (dislikedFoods.Count == 1) this.dislikesStr = LanguageManager.Get<StringsForColonistStory>(StringsForColonistStory.List1, dislikedFoods[0]);
        }

        public override string GetDescription(string colonistName)
        {
            var likesEffectStr = $"+{this.likesEffect}";
            var varietyEffectStr = this.varietyEffect >= 0 ? $"+{this.varietyEffect}" : this.varietyEffect.ToString();
            return LanguageManager.GetCardDescription(this.Type, this.Effects[CardEffectType.Happiness], likesEffectStr, varietyEffectStr, colonistName, this.likesStr, this.dislikesStr);
        }

        public override Dictionary<string, string> GetSerializationObject()
        {
            return new Dictionary<string, string> { 
                { "Likes", this.likesEffect.ToString() },
                { "Variety", this.varietyEffect.ToString() },
                { "LikesStr", this.likesStr },
                { "DislikesStr", this.dislikesStr } };
        }

        public override void InitFromSerializationObject(Dictionary<string, string> obj)
        {
            if (obj.ContainsKey("Likes")) this.likesEffect = int.Parse(obj["Likes"]);
            if (obj.ContainsKey("Variety")) this.varietyEffect = int.Parse(obj["Variety"]);
            if (obj.ContainsKey("LikesStr")) this.likesStr = obj["LikesStr"];
            if (obj.ContainsKey("DislikesStr")) this.dislikesStr = obj["DislikesStr"];

            if (!this.Effects.ContainsKey(CardEffectType.Happiness)) this.Effects.Add(CardEffectType.Happiness, likesEffect + varietyEffect);
            else this.Effects[CardEffectType.Happiness] = likesEffect + varietyEffect;
        }
    }
}
