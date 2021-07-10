namespace SigmaDraconis.Commentary.Params
{
    using Context;
    using Language;
    using Shared;

    internal class SkillParam : ITemplateParam
    {
        public string Evaluate(ColonistProxy colonist, ColonistProxy otherColonist)
        {
            switch (colonist.SkillType)
            {
                case SkillType.Engineer: return GetString(StringsForSkillTypeSubject.Engineering);
                case SkillType.Botanist: return GetString(StringsForSkillTypeSubject.Botany);
                case SkillType.Geologist: return GetString(StringsForSkillTypeSubject.Geology);
            }

            return "[skill]";
        }

        private static string GetString(StringsForSkillTypeSubject id)
        {
            return LanguageManager.IsCapitalNouns 
                ? LanguageManager.Get<StringsForSkillTypeSubject>(id)
                : LanguageManager.Get<StringsForSkillTypeSubject>(id).ToLowerInvariant();
        }
    }
}
