namespace SigmaDraconis.Commentary.Params
{
    using Context;
    using Language;
    using Shared;

    internal class ScannerResourceDensityParam : ITemplateParam
    {
        public string Evaluate(ColonistProxy colonist, ColonistProxy otherColonist)
        {
            return LanguageManager.Get<MineResourceDensity>(colonist.ScannerResourceDensityFound);
        }
    }
}
