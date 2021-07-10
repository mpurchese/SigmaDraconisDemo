namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;
    using Cards.Interface;

    public interface IColonistJobProvider: IThing
    {
        Dictionary<CardType, int> WorkRateEffects { get; }
        bool DoJob(double workSpeed, Dictionary<CardType, int> effects = null);
    }
}
