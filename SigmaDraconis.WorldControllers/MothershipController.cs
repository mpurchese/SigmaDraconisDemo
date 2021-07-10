namespace SigmaDraconis.WorldControllers
{
    using Draconis.Shared;
    using System.Collections.Generic;
    using System.Linq;
    using Cards.Interface;
    using Language;
    using WorldInterfaces;
    using Shared;
    using World;
    using World.Buildings;
    using World.Fauna;

    public static class MothershipController
    {
        public static int TimeToArrival = -1;// 86400;
        public static int TimeUntilCanWake = 0;
        public static MothershipStatus MothershipStatus;
        public static string ArrivingColonistName = "";
        public static SkillType ArrivingColonistSkill;
        public static bool CanWakeNonEngineer;

        private static readonly List<IColonistPlaceholder> colonists = new List<IColonistPlaceholder>();

        public static Dictionary<string, string> GetPropertiesForSave()
        {
            return new Dictionary<string, string>
            {
                { "TimeToArrival", TimeToArrival.ToString() },
                { "TimeUntilCanWake", TimeUntilCanWake.ToString() },
                { "MothershipStatus", ((int)MothershipStatus).ToString() },
                { "ArrivingColonistName", ArrivingColonistName },
                { "ArrivingColonistSkill", ((int)ArrivingColonistSkill).ToString()  },
                { "CanWakeNonEngineer", CanWakeNonEngineer ? "1" : "0" }
            };
        }

        public static void SetPropertiesFromLoad(Dictionary<string, string> keyValuePairs)
        {
            if (keyValuePairs.ContainsKey("TimeToArrival")) TimeToArrival = int.Parse(keyValuePairs["TimeToArrival"]);
            if (keyValuePairs.ContainsKey("TimeUntilCanWake")) TimeUntilCanWake = int.Parse(keyValuePairs["TimeUntilCanWake"]);
            if (keyValuePairs.ContainsKey("MothershipStatus")) MothershipStatus = (MothershipStatus)int.Parse(keyValuePairs["MothershipStatus"]);
            if (keyValuePairs.ContainsKey("ArrivingColonistName")) ArrivingColonistName = keyValuePairs["ArrivingColonistName"];
            if (keyValuePairs.ContainsKey("ArrivingColonistSkill")) ArrivingColonistSkill = (SkillType)int.Parse(keyValuePairs["ArrivingColonistSkill"]);
            if (keyValuePairs.ContainsKey("CanWakeNonEngineer")) CanWakeNonEngineer = keyValuePairs["CanWakeNonEngineer"] == "1";

            if (MothershipStatus == MothershipStatus.NoMoreColonists) TimeUntilCanWake = 3600 * Constants.HoursBetweenColonistWakes; // For v0.4 update
        }

        public static List<IColonistPlaceholder> GetColonistPlaceholders()
        {
            if (colonists.Count < Constants.MaxColonists) GeneratePlaceholders();
            return colonists.ToList();
        }

        public static void Reset()
        {
            TimeToArrival = -1;
            TimeUntilCanWake = 0;
            MothershipStatus = MothershipStatus.ColonistIncoming;
            ArrivingColonistName = "";
            CanWakeNonEngineer = false;
            colonists.Clear();
        }

        public static void SetColonistPlaceholders(List<IColonistPlaceholder> colonistList)
        {
            colonists.Clear();
            colonists.AddRange(colonistList);
        }

        public static bool CanWakeColonist()
        {
            return TimeUntilCanWake <= 0 && GetColonistPlaceholders().Any(p => p.PlaceHolderStatus.In(ColonistPlaceholderStatus.InStasis, ColonistPlaceholderStatus.Waking));
        }

        public static bool CanWakeColonist(IColonistPlaceholder colonist, out string reason)
        {
            reason = "";
            // TODO: When colonist == null, return true only in certain situations
            if (colonist == null) return CanWakeColonist();

            if (!colonist.PlaceHolderStatus.In(ColonistPlaceholderStatus.InStasis, ColonistPlaceholderStatus.Waking)) return false;
            if (colonist.PlaceHolderStatus == ColonistPlaceholderStatus.Waking && colonist.IsWakeCommitted) return false;

            if (!CanWakeNonEngineer && colonist.Skill != SkillType.Engineer)
            {
                reason = LanguageManager.Get<StringsForMothershipDialog>(StringsForMothershipDialog.CannotWakeNonEngineer);
                return false;   // Can only wake engineers at game start
            }

            return true;
        }

        public static void WakeColonist(IColonistPlaceholder placeholder)
        {
            // First colonist arrives immediately
            TimeToArrival = 660;
            if (colonists.Any(c => c.IsWakeCommitted)) TimeToArrival = Constants.HoursToWakeColonist * 3600;

            placeholder.IsWakeCommitted = true;
            placeholder.PlaceHolderStatus = ColonistPlaceholderStatus.Waking;
            placeholder.TimeToArrivalInFrames = TimeToArrival;
            ArrivingColonistName = placeholder.Name;
            ArrivingColonistSkill = placeholder.Skill;
            CanWakeNonEngineer = true;
            TimeUntilCanWake = TimeToArrival + (3600 * Constants.HoursBetweenColonistWakes);
            MothershipStatus = MothershipStatus.ColonistIncoming;
        }

        public static void Update()
        {
            if (colonists.Count < Constants.MaxColonists) GeneratePlaceholders();

            if (TimeToArrival > 0)
            {
                TimeToArrival--;
                TimeUntilCanWake--;
                var timeMinutes = TimeToArrival / 3600;
                var timeSeconds = (TimeToArrival % 3600) / 60;
                MothershipStatus = TimeToArrival < 300 ? MothershipStatus.ColonistArriving : MothershipStatus.ColonistIncoming;

                var placeholder = colonists.FirstOrDefault(c => c.TimeToArrivalInFrames > 0 && c.IsWakeCommitted);
                if (placeholder != null) placeholder.TimeToArrivalInFrames = TimeToArrival;

                if (TimeToArrival == 659 && placeholder != null)
                {
                    var tile = ColonistArrivalController.ChooseLandingCoord();
                    if (tile != null)
                    {
                        var index = colonists.Count(c => c.IsWakeCommitted);

                        var colonistStr = LanguageManager.Get<StringsForMothershipDialog>(StringsForMothershipDialog.Colonist);
                        var numberStr = LanguageManager.GetNumberOrDate($"Number{index}");
                        var colonist = new Colonist(tile)
                        {
                            Name = $"{colonistStr} {numberStr} \"{placeholder.Name}\"",
                            ShortName = placeholder.Name,
                            ColourCode = placeholder.ColourCode,
                            HairColourCode = placeholder.HairColourCode,
                            FacingDirection = Direction.S,
                            Rotation = DirectionHelper.GetAngleFromDirection(Direction.S),
                            Skill = placeholder.Skill
                        };

                        colonist.UpdateMovingAnimationFrame();
                        foreach (var story in placeholder.Story) colonist.Story.Add(story);
                        foreach (var card in placeholder.Traits) colonist.AddCard(card);
                        foreach (var kv in placeholder.FoodOpinions) colonist.SetFoodOpinion(kv.Key, kv.Value);
                        if (placeholder.Skill == SkillType.Programmer) colonist.AddCard(CardType.Programmer);
                        World.AddThing(colonist);

                        // Only do jobs for which we have the relevant skill
                        colonist.WorkPriorities[ColonistPriority.Construct] = colonist.Skill == SkillType.Engineer ? 9 : 0;
                        colonist.WorkPriorities[ColonistPriority.Maintain] = colonist.Skill == SkillType.Engineer ? 4 : 0;
                        colonist.WorkPriorities[ColonistPriority.Geology] = colonist.Skill == SkillType.Geologist ? 5 : 0;
                        colonist.WorkPriorities[ColonistPriority.FarmPlant] = colonist.Skill == SkillType.Botanist ? 8 : 0;
                        colonist.WorkPriorities[ColonistPriority.ResearchBotanist] = (colonist.Skill == SkillType.Botanist || colonist.Skill == SkillType.Programmer) ? 3 : 0;
                        colonist.WorkPriorities[ColonistPriority.ResearchEngineer] = (colonist.Skill == SkillType.Engineer || colonist.Skill == SkillType.Programmer) ? 3 : 0;
                        colonist.WorkPriorities[ColonistPriority.ResearchGeologist] = (colonist.Skill == SkillType.Geologist || colonist.Skill == SkillType.Programmer) ? 3 : 0;

                        // Programmers are rubbish
                        if (colonist.Skill == SkillType.Programmer)
                        {
                            colonist.WorkPriorities[ColonistPriority.Deconstruct] = 0;
                            colonist.WorkPriorities[ColonistPriority.Haul] = 0;
                        }

                        var pod1 = new LandingPod(colonist.MainTile) { Altitude = 300f };
                        World.AddThing(pod1);
                        pod1.AfterAddedToWorld();
                        pod1.AfterConstructionComplete();

                        ArrivingColonistName = colonist.ShortName;

                        placeholder.ActualColonistID = colonist.Id;

                    }
                }
                else if (TimeToArrival == 0)
                {
                    placeholder.PlaceHolderStatus = ColonistPlaceholderStatus.Active;
                    WorldStats.Increment(WorldStatKeys.ColonistsWoken);
                }
            }
            else if (colonists.All(c => c.PlaceHolderStatus != ColonistPlaceholderStatus.InStasis))
            {
                MothershipStatus = MothershipStatus.NoMoreColonists;
            }
            else if (TimeUntilCanWake > 0)
            {
                MothershipStatus = MothershipStatus.PareparingToWake;
                TimeUntilCanWake--;
                if (TimeUntilCanWake <= 0)
                {
                    var colonist = World.GetThings<IColonist>(ThingType.Colonist).FirstOrDefault(c => !c.Body.IsSleeping && !c.Body.IsDead);
                    if (colonist != null) EventManager.EnqueueColonistEvent(ColonistEventType.ReadyToWake, colonist.Id);
                }
            }
            else MothershipStatus = MothershipStatus.ReadyToWakeNow;
        }

        private static void GeneratePlaceholders()
        {
            for (int i = colonists.Count; i < Constants.MaxColonists; i++)
            {
                var bodyColour = Rand.Next(10);
                var hairColour = Rand.Next(18);

                var name = ColonistNameGenerator.GetNextName(colonists.Select(c => c.Name).ToList());
                var colonist = new ColonistPlaceholder(i, name, bodyColour, hairColour, ColonistPlaceholderStatus.InStasis);

                colonist.Traits.Add((CardType)(Rand.Next(4) + 1));

                if (i == 9) colonist.Skill = SkillType.Programmer;
                else if (i % 3 == 0) colonist.Skill = SkillType.Engineer;
                else if (i % 3 == 1) colonist.Skill = SkillType.Botanist;
                else if (i % 3 == 2) colonist.Skill = SkillType.Geologist;

                colonist.GenerateStory();
                colonists.Add(colonist);
            }
        }
    }
}
