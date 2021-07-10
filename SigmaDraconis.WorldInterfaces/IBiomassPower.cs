namespace SigmaDraconis.WorldInterfaces
{
    public interface IBiomassPower : IPowerPlant
    {
        bool AllowBurnMush { get; set; }
        bool AllowBurnOrganics { get; set; }
    }
}
