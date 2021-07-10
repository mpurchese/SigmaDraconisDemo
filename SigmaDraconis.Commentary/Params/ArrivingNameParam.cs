namespace SigmaDraconis.Commentary.Params
{
    using Context;

    internal class ArrivingNameParam : ITemplateParam
    {
        public string Evaluate(ColonistProxy colonist, ColonistProxy otherColonist)
        {
            return CommentaryContext.ArrivingName;
        }
    }
}
