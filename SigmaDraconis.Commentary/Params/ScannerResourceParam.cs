namespace SigmaDraconis.Commentary.Params
{
    using Context;
    using Language;

    internal class ScannerResourceParam : ITemplateParam
    {
        public string Evaluate(ColonistProxy colonist, ColonistProxy otherColonist)
        {
            return LanguageManager.Get<StringsForItemTypeLower>(colonist.ScannerResourceFound);
        }
    }
}
