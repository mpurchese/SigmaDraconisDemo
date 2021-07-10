namespace SigmaDraconis.Shared
{
    public class HeatOrLightSource
    {
        public float Amount { get; set; }
        public bool IsOn { get; set; }

        public HeatOrLightSource(float amount, bool isOn)
        {
            this.Amount = amount;
            this.IsOn = isOn;
        }
    }
}
