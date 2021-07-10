namespace SigmaDraconis.Commentary.Requirements
{
    using System;

    using Cards.Interface;

    using Context;
    using Operators;

    internal class ColonistCardRequirement : RequirementBase
    {
        public CardType CardType { get; }
        public bool Value { get; }

        public ColonistCardRequirement(string type, Operator op, bool value) : base (op)
        {
            this.CardType = (CardType)Enum.Parse(typeof(CardType), type);
            this.Value = value;
        }

        protected override bool TestThis(ColonistProxy colonist, bool isCurrentComment, ColonistProxy otherColonist)
        {
            return this.op.Test(colonist.Cards.Contains(this.CardType), this.Value);
        }
    }
}
