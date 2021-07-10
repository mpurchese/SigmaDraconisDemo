namespace SigmaDraconis.Commentary.Params
{
    using Context;

    internal class OtherNameParam : ITemplateParam
    {
        public string Evaluate(ColonistProxy colonist, ColonistProxy otherColonist)
        {
            return otherColonist.Name;
        }
    }
}
