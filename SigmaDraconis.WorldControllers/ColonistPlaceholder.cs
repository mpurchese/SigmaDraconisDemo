namespace SigmaDraconis.WorldControllers
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;
    using ProtoBuf;
    using Cards.Interface;
    using Config;
    using Language;
    using Shared;
    using WorldInterfaces;

    [ProtoContract]
    public class ColonistPlaceholder : IColonistPlaceholder
    {
        [ProtoMember(1)]
        public int Index { get; set; }

        [ProtoMember(2)]
        public string Name { get; set; }

        [ProtoMember(3)]
        public List<CardType> Traits { get; private set; }

        [ProtoMember(4)]
        public SkillType Skill { get; set; }

        [ProtoMember(5)]
        public List<string> Story { get; }  // Not used from v0.11

        [ProtoMember(6)]
        public int ColourCode { get; set; }

        [ProtoMember(7)]
        public int HairColourCode { get; set; }

        [ProtoMember(8)]
        public ColonistPlaceholderStatus PlaceHolderStatus { get; set; }

        [ProtoMember(9)]
        public int TimeToArrivalInFrames { get; set; }

        [ProtoMember(10)]
        public int? ActualColonistID { get; set; }

        [ProtoMember(11)]
        public bool IsWakeCommitted { get; set; }

        [ProtoMember(12)]
        public Dictionary<int, int> FoodOpinions { get; private set; }

        // For deserialzation
        private ColonistPlaceholder() { }

        public ColonistPlaceholder(int index, string name, int colourCode, int hairColourCode, ColonistPlaceholderStatus status)
        {
            this.Index = index;
            this.Name = name;
            this.Traits = new List<CardType>();
            this.Story = new List<string>();
            this.ColourCode = colourCode;
            this.HairColourCode = hairColourCode;
            this.PlaceHolderStatus = status;
            this.TimeToArrivalInFrames = 660;
            this.GenerateRandomFoodOpinions();
        }

        public void GenerateStory()
        {
            if (this.FoodOpinions == null) this.GenerateRandomFoodOpinions();

            this.Story.Clear();
            var n = 100 + ((((int)this.Skill) - 1) * 10);
            var skillReplace1 = GetString((StringsForColonistStory)n);
            var skillReplace2 = this.Skill != SkillType.Programmer ? GetString((StringsForColonistStory)n + 1) : "";

            var story1 = GetString(StringsForColonistStory.Skill1).Replace("[discipline1]", this.ProcessStoryString(skillReplace1));
            var story2 = this.Skill != SkillType.Programmer ? GetString(StringsForColonistStory.Skill2).Replace("[discipline2]", this.ProcessStoryString(skillReplace2)) : GetString(StringsForColonistStory.Skill2a);
            var story3 = GetString(StringsForColonistStory.Skill3);
            var story4 = this.Traits.Count > 0 ? GetString((StringsForColonistStory)(1000 + (int)this.Traits[0])) : "";
            var story5 = GetFoodOpinionsString();

            this.Story.Add(this.ProcessStoryString(story1));
            this.Story.Add(this.ProcessStoryString(story2));
            this.Story.Add(this.ProcessStoryString(story3));
            if (story4 != "") this.Story.Add(this.ProcessStoryString(story4));
            this.Story.Add(story5);
        }

        private string ProcessStoryString(string str)
        {
            var result = str.Replace("[name]", this.Name);

            // Curley brackets contain options that can be picked at random
            var match = Regex.Match(result, "{.+?}");
            while (match.Success)
            {
                result = result.Replace(match.Value, this.ReplaceCurleys(match.Value));
                match = Regex.Match(result, "{.+?}");
            }

            return result;
        }

        private string ReplaceCurleys(string str)
        {
            var options = str.Split('|');
            var index = Rand.Next(options.Length);
            return options[index].Replace("{", "").Replace("}", "");
        }

        private void GenerateRandomFoodOpinions()
        {
            var crops = CropDefinitionManager.GetAll().Where(c => c.IsCrop && c.CanEat).ToList();

            this.FoodOpinions = new Dictionary<int, int>
            {
                { 0, 0 },
                { Constants.MushFoodType, -1 }
            };

            var likes = 0;
            var neutral = 1;
            var dislikes = 1;
            foreach (var crop in crops)
            {
                var opinion = 0;
                var ok = false;
                while (!ok)
                {
                    opinion = Rand.Next(3) - 1;
                    if (opinion == 1 && likes < 3)
                    {
                        likes++;
                        ok = true;
                    }
                    else if (opinion == 0 && neutral < 3)
                    {
                        neutral++;
                        ok = true;
                    }
                    else if (opinion == -1 && dislikes < 3)
                    {
                        dislikes++;
                        ok = true;
                    }
                }

                this.FoodOpinions.Add(crop.Id, opinion);
            }

            // Must have at least one like and one dislike (not including mush)
            while (likes == 0 || dislikes == 1)
            {
                var r = Rand.Next(crops.Count);
                var id = crops[r].Id;
                if (this.FoodOpinions[id] != 0) continue;
                if (likes == 0)
                {
                    this.FoodOpinions[id] = 1;
                    likes++;
                }
                else if (dislikes == 1)
                {
                    this.FoodOpinions[id] = -1;
                    dislikes++;
                }
            }
        }

        private string GetFoodOpinionsString()
        {
            var likes = this.FoodOpinions.Where(f => f.Key != Constants.MushFoodType && f.Value > 0).Select(f => CropDefinitionManager.GetDefinition(f.Key).DisplayNameLower).ToList();
            var dislikes = this.FoodOpinions.Where(f => f.Key != Constants.MushFoodType && f.Value < 0).Select(f => CropDefinitionManager.GetDefinition(f.Key).DisplayNameLower).ToList();
            return LanguageManager.Get<StringsForColonistStory>(StringsForColonistStory.Food, GetList(likes), GetList(dislikes)).Replace("[name]", this.Name);
        }

        private static string GetString(StringsForColonistStory str)
        {
            return LanguageManager.Get<StringsForColonistStory>(str);
        }

        private static string GetList(List<string> items)
        {
            if (items.Count == 1) return LanguageManager.Get<StringsForColonistStory>(StringsForColonistStory.List1, items[0]);
            if (items.Count == 2) return LanguageManager.Get<StringsForColonistStory>(StringsForColonistStory.List2, items[0], items[1]);
            if (items.Count == 3) return LanguageManager.Get<StringsForColonistStory>(StringsForColonistStory.List3, items[0], items[1], items[2]);
            return "";
        }
    }
}
