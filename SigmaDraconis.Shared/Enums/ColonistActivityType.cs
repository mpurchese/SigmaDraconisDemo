namespace SigmaDraconis.Shared
{
    public enum ColonistActivityType
    {
        None = 0,
        [IsRecreation(true)]
        Roam = 1,
        Sleep = 2,
        [IsWork(true)]
        Deconstruct = 3,
        [IsWork(true)]
        HaulPickup = 4,
        [IsWork(true)]
        HaulDropoff = 5,
        Eat = 6,
        Drink = 7,
        [IsWork(true)]
        Farm = 8,
        [IsWork(true)]
        Cook = 10,
        [IsWork(true)]
        Lab = 11,
        ReturnHome = 12,
        GetWarm = 13,
        LeavingLandingPod = 14,
        [IsRecreation(true)]
        Relax = 15,
        Rest = 16,
        [IsWork(true)]
        Mine = 17,
        Move = 18,
        [IsWork(true)]
        Construct = 19,
        [IsWork(true)]
        Repair = 20,
        [IsWork(true)]
        Harvest = 21,
        [IsWork(true)]
        Geology = 22,
        [IsRecreation(true)]
        Social = 23,
        [IsRecreation(true)]
        GetKek = 24,
        [IsRecreation(true)]
        DrinkKek = 25,
    }

    public class IsWorkAttribute : System.Attribute
    {
        public IsWorkAttribute(bool value)
        {
            this.Value = value;
        }

        public bool Value { get; }
    }

    public class IsRecreationAttribute : System.Attribute
    {
        public IsRecreationAttribute(bool value)
        {
            this.Value = value;
        }

        public bool Value { get; }
    }
}
