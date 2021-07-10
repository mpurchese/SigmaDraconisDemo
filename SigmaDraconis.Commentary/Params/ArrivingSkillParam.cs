namespace SigmaDraconis.Commentary.Params
{
    using Context;
    using Language;
    using Shared;

    internal class ArrivingSkillParam : ITemplateParam
    {
        public string Evaluate(ColonistProxy colonist, ColonistProxy otherColonist)
        {
            return LanguageManager.IsCapitalNouns 
                ? LanguageManager.Get<SkillType>(CommentaryContext.ArrivingSkill)
                : LanguageManager.Get<SkillType>(CommentaryContext.ArrivingSkill).ToLowerInvariant();
        }
    }
}
