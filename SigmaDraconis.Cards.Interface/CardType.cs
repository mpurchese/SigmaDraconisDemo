namespace SigmaDraconis.Cards.Interface
{
    public enum CardType
    {
        None = 0,

        // Traits
        ColdTolerant = 1,
        HeatTolerant = 2,
        FastWalk = 3,
        Workaholic = 4,

        // Happiness
        VeryHappy = 100,
        Happy = 101,
        NeutralHappiness = 102,
        Unhappy = 103,
        VeryUnhappy = 104,

        // Needs
        Thirst1 = 200,
        Thirst2 = 201,
        Thirst3 = 202,
        Hunger1 = 210,
        Hunger2 = 211,
        Hunger3 = 212,
        Tired1 = 220,
        Tired2 = 221,
        Cold1 = 230,
        Cold2 = 231,
        Cold3 = 232,
        Hot1 = 240,
        Hot2 = 241,
        Hot3 = 242,

        // Workload
        WorkloadLow = 300,
        WorkloadModerate = 301,
        WorkloadHigh = 302,
        WorkloadExtreme = 303,

        // Food effects
        GoodDiet = 400,
        NeutralDiet = 401,
        BadDiet = 402,

        // Sleep
        SleepGood = 500,
        SleepBad = 501,

        // Recreation
        Roam = 600,

        // Social
        Social1 = 700,
        Social2 = 701,
        Social3 = 702,

        // Misc
        NewArrival = 900,
        NewColony = 901,
        Lonely = 902,
        GoodMood = 903,
        BadMood = 904,
        Dark = 905,
        WorkOutside = 906,
        Kek = 907,
        Programmer = 908,
    }
}
