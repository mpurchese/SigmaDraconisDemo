namespace SigmaDraconis.WorldInterfaces
{
    using Shared;

    public interface IWaterDispenser : IDispenser
    {
        /// <summary>
        /// Prepare and deliver water
        /// </summary>
        /// <param name="amount">The amount taken (may be zero during preparation)</param>
        /// <returns>False if something wrong</returns>
        bool TakeWater(out double amount);
    }
}
