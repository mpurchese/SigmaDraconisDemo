namespace SigmaDraconis.WorldInterfaces
{
    using System.Collections.Generic;

    public interface IFoodDispenser : IDispenser
    {
        bool AllowMush { get; set; }

        bool AllowFood { get; set; }
        int CurrentFoodType { get; set; }

        IEnumerable<int> GetFoodTypesAvailable();
        bool TakeFood(int foodType, out double amount);
    }
}
