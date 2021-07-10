namespace SigmaDraconis.Commentary.Params
{
    using Context;

    internal interface ITemplateParam
    {
        string Evaluate(ColonistProxy colonist, ColonistProxy otherColonist);
    }
}
