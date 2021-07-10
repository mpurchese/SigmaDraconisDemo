namespace SigmaDraconis.Commentary.Params
{
    using Context;
    using Language;

    internal class LastFoodParam : ITemplateParam
    {
        public string Evaluate(ColonistProxy colonist, ColonistProxy otherColonist)
        {
            return LanguageManager.IsCapitalNouns ? colonist.LastFoodType : colonist.LastFoodType.ToLowerInvariant();
        }
    }
}
