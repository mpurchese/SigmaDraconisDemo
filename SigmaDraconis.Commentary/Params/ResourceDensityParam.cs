namespace SigmaDraconis.Commentary.Params
{
    using Context;
    using Language;
    using Shared;

    internal class ResourceDensityParam : ITemplateParam
    {
        public string Evaluate(ColonistProxy colonist, ColonistProxy otherColonist)
        {
            return LanguageManager.Get<MineResourceDensity>(colonist.LastResourceDensityFound);
        }
    }
}
