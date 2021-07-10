namespace SigmaDraconis.Commentary.Params
{
    using Context;

    internal class NameParam : ITemplateParam
    {
        public string Evaluate(ColonistProxy colonist, ColonistProxy otherColonist)
        {
            return colonist.Name;
        }
    }
}
