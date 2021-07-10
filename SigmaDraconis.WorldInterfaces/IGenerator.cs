namespace SigmaDraconis.WorldInterfaces
{
    public interface IGenerator : IPowerPlant
    {
        bool AllowBurnCoal { get; set; }
        bool AllowBurnOrganics { get; set; }
    }
}
