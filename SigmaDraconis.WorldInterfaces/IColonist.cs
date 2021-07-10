namespace SigmaDraconis.WorldInterfaces
{
    using System;
    using System.Collections.Generic;
    using Cards.Interface;
    using Config;
    using Medical;
    using Shared;

    public interface IColonist : IAnimal, IColonistInteractive
    {
        ColonistBody Body { get; set; }
        ICardCollection Cards { get; }
        ColonistActivityType ActivityType { get; set; }
        string CurrentActivityDescription { get; }
        int? TargetBuilingID { get; set; }
        bool IsRenderLayer1 { get; }
        int RaisedArmsFrame { get; set; }
        int SleepFrame { get; set; }
        int ColourCode { get; set; }
        int HairColourCode { get; set; }
        ItemType CarriedItemTypeArms { get; set; }
        ItemType CarriedItemTypeBack { get; set; }
        ItemType TargetItemType { get; set; }
        int? CarriedCropType { get; set; }
        bool IsWorking { get; set; }
        bool IsRelaxing { get; set; }
        int DrinkingKekFrame { get; set; }
        bool IsIdle { get; set; }
        int IdleTimer { get; }
        int LookingForWaterCounter { get; set; }
        int LookingForFoodCounter { get; set; }
        int Happiness { get; }
        int LastLabProjectId { get; set; }
        bool IsActivityFinished { get; set; }
        uint FramesSinceArrival { get; set; }
        int LastFoodType { get; }
        string Name { get; set; }
        new string ShortName { get; set; }
        bool? SleptInPod { get; }
        bool? SleptOutside { get; }
        int? SleptTemperature { get; }
        bool WaitForDoor { get; set; }
        KekPolicy KekPolicy { get; }
        WorkPolicy WorkPolicy { get; }
        int WorkCooldownTimer { get; }
        int KekHappinessTimer { get; set; }
        Dictionary<ColonistPriority, int> WorkPriorities { get; set; }
        double RestRatePerHour { get; }
        double WorkRate { get; }
        Dictionary<int, int> TimeSinceSocialByColonist { get; }
        SkillType Skill { get; }
        List<string> Story { get; }
        HashSet<int> GiveWayRequestTiles { get; }
        int FramesSinceWaking { get; }
        int StressRateOfChange { get; }
        StressLevel StressLevel { get; }
        uint FramesRoaming { get; }
        uint? FramesSinceRoam { get; }
        bool IsArrived { get; set; }
        int ColonistToWelcome { get; set; }
        int ColonistToMourn { get; set; }
        ItemType LastResourceFound { get; set; }
        MineResourceDensity LastResourceDensityFound { get; set; }
        int OreScannerTileForComment { get; set; }
        ItemType ScannerResourceFound { get; set; }
        MineResourceDensity ScannerResourceDensityFound { get; set; }

        double HungerDisplay { get; }
        double ThirstDisplay { get; }
        double TirednessDisplay { get; }
        double StressDisplay { get; }
        double HungerRateOfChangeDisplay { get; }
        double ThirstRateOfChangeDisplay { get; }
        double TirednessRateOfChangeDisplay { get; }
        double StressRateOfChangeDisplay { get; }

        // Commentary story
        int StorySport { get; set; }
        int StoryInstrument { get; set; }
        int StoryWorkedplace { get; set; }

        void BeginSleeping();
        void Drink(double hydration);
        void Eat(int FoodType, double nourishment, bool firstFrame);
        void UpdateMovingAnimationFrame();
        double GetWorkRate();
        double GetWorkRate(out Dictionary<CardType, int> effects);
        void RaiseArms();
        int GetTileSleepScore(ISmallTile tile);
        void RequestGiveWay(List<ISmallTile> tiles);
        void AddCard(CardType cardType);
        IEnumerable<CropDefinition> GetFoodLikes();
        IEnumerable<CropDefinition> GetFoodDisikes();
        IEnumerable<CropDefinition> GetFoodNeutral();
        IReadOnlyCollection<Tuple<int, long>> GetRecentMeals();
        int? GetFoodOpinion(int cropType, bool pickRandomIfNew = false);
        void SetFoodOpinion(int cropType, int opinion);
        void UpdateDietCard(out int varietyEffect, out int lastMealRepeatCount);
        int GetFoodScoreForEating(int foodType);
        void SetKekPolicy(KekPolicy newPolicy);
        void SetWorkPolicy(WorkPolicy newPolicy);
        bool IsWillingToWork(bool isUrgent);
    }
}
